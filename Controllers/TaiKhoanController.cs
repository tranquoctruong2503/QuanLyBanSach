using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Nhom06_QuanLyBanSah.Models;

namespace Nhom06_QuanLyBanSah.Controllers
{
    public class TaiKhoanController : Controller
    {
        QUANLYBANSACH_NHOM06Entities db = new QUANLYBANSACH_NHOM06Entities();

        //Đăng ký
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(TAIKHOAN model, string XacNhanMatKhau)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.HoTen))
                {
                    ViewBag.Error = "Vui lòng nhập họ tên!";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ViewBag.Error = "Vui lòng nhập email!";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.MatKhau))
                {
                    ViewBag.Error = "Vui lòng nhập mật khẩu!";
                    return View(model);
                }

                if (model.MatKhau.Length < 6)
                {
                    ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự!";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(XacNhanMatKhau))
                {
                    ViewBag.Error = "Vui lòng xác nhận lại mật khẩu!";
                    return View(model);
                }

                if (model.MatKhau != XacNhanMatKhau)
                {
                    ViewBag.Error = "Mật khẩu và xác nhận mật khẩu không khớp nhau!";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.DienThoai))
                {
                    ViewBag.Error = "Vui lòng nhập số điện thoại!";
                    return View(model);
                }

                // Kiểm tra email đã tồn tại
                var existEmail = db.TAIKHOAN.FirstOrDefault(x => x.Email == model.Email);
                if (existEmail != null)
                {
                    ViewBag.Error = "Email đã được sử dụng! Vui lòng chọn email khác.";
                    return View(model);
                }

                // Kiểm tra số điện thoại đã tồn tại
                var existPhone = db.TAIKHOAN.FirstOrDefault(x => x.DienThoai == model.DienThoai);
                if (existPhone != null)
                {
                    ViewBag.Error = "Số điện thoại đã được đăng ký! Vui lòng sử dụng số khác.";
                    return View(model);
                }

                // Thiết lập thông tin mặc định
                model.Role = "user";
                model.NgayTao = DateTime.Now;
                model.NgayCapNhat = DateTime.Now;

                //Mã hóa mật khẩu bằng BCrypt
                model.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

                // Lưu vào database
                db.TAIKHOAN.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Đăng ký tài khoản thành công!";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.";
                return View(model);
            }
        }

        //Đăng nhập
        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string Email, string MatKhau, string returnUrl)
        {
            try
            {
                // Kiểm tra input
                if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(MatKhau))
                {
                    ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                    return View();
                }

                // Loại bỏ khoảng trắng thừa
                Email = Email.Trim();
                MatKhau = MatKhau.Trim();

                // Debug
                System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ===");
                System.Diagnostics.Debug.WriteLine($"Email: {Email}");
                System.Diagnostics.Debug.WriteLine($"Password (plain text): {MatKhau}");

                // Tìm user theo email
                var user = db.TAIKHOAN.FirstOrDefault(x => x.Email == Email);

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("User NOT FOUND in database");
                    ViewBag.Error = "Email không tồn tại trong hệ thống!";
                    return View();
                }

                System.Diagnostics.Debug.WriteLine($"User found: {user.HoTen}, Role: {user.Role}");

               
                // So sánh chuỗi đã mã hóa
                bool isValidPassword = BCrypt.Net.BCrypt.Verify(MatKhau, user.MatKhau);
                if (!isValidPassword)
                {
                    System.Diagnostics.Debug.WriteLine("Password MISMATCH");
                    ViewBag.Error = "Mật khẩu không đúng!";
                    return View();
                }

                System.Diagnostics.Debug.WriteLine("Login SUCCESS");

                Session["TaiKhoan"] = user;
                Session["HoTen"] = user.HoTen;
                Session["Role"] = user.Role;
                Session["UserID"] = user.userID;

                System.Diagnostics.Debug.WriteLine($"Session created - Role: {user.Role}");

                // Chuyển hướng theo role
                if (user.Role == "admin")
                {
                    System.Diagnostics.Debug.WriteLine("Redirecting to Admin panel");
                    return RedirectToAction("Index", "admin");
                }

                System.Diagnostics.Debug.WriteLine("Redirecting to Home page");
                return RedirectToAction("Trangchu", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LOGIN ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ViewBag.Error = $"Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.";
                return View();
            }
        }

      

      
        public ActionResult DangXuat()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Trangchu", "Home");
        }

        //Thông tin cá nhân
        [HttpGet]
        public ActionResult ThongTinCaNhan()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            int userID = (int)Session["UserID"];
            var user = db.TAIKHOAN.Find(userID);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThongTinCaNhan(TAIKHOAN model)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            if (ModelState.IsValid)
            {
                var user = db.TAIKHOAN.Find(model.userID);
                if (user != null)
                {
                    user.HoTen = model.HoTen;
                    user.NgaySinh = model.NgaySinh;
                    user.GioiTinh = model.GioiTinh;
                    user.DienThoai = model.DienThoai;
                    user.DiaChi = model.DiaChi;
                    user.NgayCapNhat = DateTime.Now;

                    db.SaveChanges();
                    Session["HoTen"] = user.HoTen;
                    ViewBag.Success = "Cập nhật thông tin thành công!";
                }
            }

            return View(model);
        }

        //Đổi mật khẩu
        [HttpGet]
        public ActionResult DoiMatKhau()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("DangNhap");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(string MatKhauCu, string MatKhauMoi, string XacNhanMatKhau)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("DangNhap");

            int userID = (int)Session["UserID"];
            var user = db.TAIKHOAN.Find(userID);

            bool isValidOldPassword = BCrypt.Net.BCrypt.Verify(MatKhauCu, user.MatKhau);
            if (!isValidOldPassword)
            {
                ViewBag.Error = "Mật khẩu cũ không đúng!";
                return View();
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu mới không khớp!";
                return View();
            }

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhauMoi);
            user.NgayCapNhat = DateTime.Now;
            db.SaveChanges();

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        //Sinh mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length = 8)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        //Gửi Email (Cấu hình SMTP Gmail)
        private bool SendEmail(string toEmail, string newPassword)
        {
            try
            {
                // 1. Cấu hình thông tin người gửi
                var senderEmail = new MailAddress("trqtruong123@gmail.com", "Nhà Sách Quốc Trường");
                var receiverEmail = new MailAddress(toEmail);

                const string senderAppPassword = "uvqe ojjg bwgp fbnu";

                const string subject = "Khôi phục mật khẩu - Nhà Sách Quốc Trường";
                string body = $@"
            <h3>Thông báo cấp lại mật khẩu</h3>
            <p>Chào bạn,</p>
            <p>Hệ thống đã đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Mật khẩu mới của bạn là: <strong>{newPassword}</strong></p>
            <p>Vui lòng đăng nhập và tiến hành đổi lại mật khẩu ngay để đảm bảo an toàn.</p>
            <br/>
            <p>Trân trọng,</p>
            <p>Đội ngũ hỗ trợ khách hàng.</p>";

                // 2. Cấu hình server gửi mail của Google (SMTP)
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, senderAppPassword)
                };

                // 3. Tạo nội dung email
                using (var message = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true 
                })
                {
                    smtp.Send(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi gửi mail: " + ex.Message);
                return false;
            }
        }

        //Quên mật khẩu

        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuenMatKhau(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ email!";
                return View();
            }

            // 1. Kiểm tra xem Email có tồn tại trong hệ thống không
            var user = db.TAIKHOAN.FirstOrDefault(x => x.Email == Email.Trim());
            if (user == null)
            {
                TempData["Success"] = "Nếu email hợp lệ, một mật khẩu mới đã được gửi đến hộp thư của bạn.";
                return RedirectToAction("DangNhap");
            }

            string newRandomPassword = GenerateRandomPassword(8);

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newRandomPassword);
            user.NgayCapNhat = DateTime.Now;
            db.SaveChanges();

            bool isEmailSent = SendEmail(user.Email, newRandomPassword);

            if (isEmailSent)
            {
                TempData["Success"] = "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("DangNhap");
            }
            else
            {
                ViewBag.Error = "Lỗi hệ thống gửi mail. Không thể khôi phục mật khẩu lúc này!";
                return View();
            }
        }
    }
}