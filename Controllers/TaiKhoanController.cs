using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Nhom06_QuanLyBanSah.Models;

namespace Nhom06_QuanLyBanSah.Controllers
{
    public class TaiKhoanController : Controller
    {
        QUANLYBANSACH_NHOM06Entities db = new QUANLYBANSACH_NHOM06Entities();

        // GET: Đăng nhập
        public ActionResult DangNhap()
        {
            return View();
        }

        // POST: Đăng nhập - KHÔNG MÃ HÓA
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

                // Debug - Ghi log
                System.Diagnostics.Debug.WriteLine("=== LOGIN ATTEMPT ===");
                System.Diagnostics.Debug.WriteLine($"Email: {Email}");
                System.Diagnostics.Debug.WriteLine($"Password: {MatKhau}");

                // Tìm user theo email
                var user = db.TAIKHOAN.FirstOrDefault(x => x.Email == Email);

                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("User NOT FOUND in database");
                    ViewBag.Error = "Email không tồn tại trong hệ thống!";
                    return View();
                }

                System.Diagnostics.Debug.WriteLine($"User found: {user.HoTen}, Role: {user.Role}");
                System.Diagnostics.Debug.WriteLine($"DB Password: {user.MatKhau}");

                // So sánh mật khẩu TRỰC TIẾP (không mã hóa)
                if (user.MatKhau != MatKhau)
                {
                    System.Diagnostics.Debug.WriteLine("Password MISMATCH");
                    ViewBag.Error = "Mật khẩu không đúng!";
                    return View();
                }

                System.Diagnostics.Debug.WriteLine("Login SUCCESS");

                // Đăng nhập thành công - Lưu session
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
                ViewBag.Error = $"Đã xảy ra lỗi: {ex.Message}";
                return View();
            }
        }

        // GET: Test Password - Để debug
        public ActionResult TestPassword()
        {
            try
            {
                var allUsers = db.TAIKHOAN.ToList();

                StringBuilder result = new StringBuilder();
                result.Append("<html><head><style>");
                result.Append("body { font-family: Arial; padding: 20px; }");
                result.Append("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
                result.Append("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
                result.Append("th { background-color: #4CAF50; color: white; }");
                result.Append("tr:nth-child(even) { background-color: #f2f2f2; }");
                result.Append(".info { background-color: #d9edf7; padding: 15px; border-radius: 5px; margin: 10px 0; }");
                result.Append("</style></head><body>");

                result.Append("<h2>🔑 Danh Sách Tài Khoản (Password không mã hóa)</h2>");

                result.Append("<div class='info'>");
                result.Append("<p><strong>Lưu ý:</strong> Password được lưu dạng PLAIN TEXT (không mã hóa)</p>");
                result.Append("</div>");

                result.Append("<h3>📋 Tất cả tài khoản:</h3>");
                result.Append("<table>");
                result.Append("<tr><th>ID</th><th>Họ Tên</th><th>Email</th><th>Password</th><th>Role</th><th>Action</th></tr>");

                foreach (var u in allUsers)
                {
                    result.Append("<tr>");
                    result.Append($"<td>{u.userID}</td>");
                    result.Append($"<td>{u.HoTen}</td>");
                    result.Append($"<td>{u.Email}</td>");
                    result.Append($"<td><strong>{u.MatKhau}</strong></td>");
                    result.Append($"<td><span style='color: {(u.Role == "admin" ? "red" : "blue")}'>{u.Role}</span></td>");
                    result.Append($"<td><button onclick='copyLogin(\"{u.Email}\", \"{u.MatKhau}\")'>Copy Login</button></td>");
                    result.Append("</tr>");
                }

                result.Append("</table>");

                result.Append("<div class='info'>");
                result.Append("<h4>📝 Tạo tài khoản mới:</h4>");
                result.Append("<pre style='background: #f4f4f4; padding: 15px; border-radius: 5px;'>");
                result.Append(@"
INSERT INTO TAIKHOAN 
(HoTen, NgaySinh, GioiTinh, DienThoai, MatKhau, Email, DiaChi, Role, NgayTao, NgayCapNhat) 
VALUES
(N'Test User', '1990-01-01', N'Nam', '0999999999', '123456', 'test@test.com', N'TP.HCM', 'user', GETDATE(), GETDATE());
");
                result.Append("</pre>");
                result.Append("</div>");

                result.Append("<script>");
                result.Append(@"
function copyLogin(email, pass) {
    const text = 'Email: ' + email + '\nPassword: ' + pass;
    navigator.clipboard.writeText(text).then(() => {
        alert('Đã copy thông tin đăng nhập!');
    });
}
");
                result.Append("</script>");

                result.Append("</body></html>");

                return Content(result.ToString(), "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<h2>Error:</h2><pre>{ex.Message}\n\n{ex.StackTrace}</pre>", "text/html");
            }
        }

        // GET: Đăng ký
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(TAIKHOAN model)
        {
            try
            {
                // Debug log
                System.Diagnostics.Debug.WriteLine("=== REGISTRATION ATTEMPT ===");
                System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");
                System.Diagnostics.Debug.WriteLine($"HoTen: {model.HoTen}");

                // Validate cơ bản
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

                // LƯU MẬT KHẨU TRỰC TIẾP (không mã hóa)
                System.Diagnostics.Debug.WriteLine($"Password (plain text): {model.MatKhau}");

                // Lưu vào database
                db.TAIKHOAN.Add(model);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Registration SUCCESS");

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập với email và mật khẩu vừa tạo.";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"REGISTRATION ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                ViewBag.Error = $"Đã xảy ra lỗi khi đăng ký: {ex.Message}";
                return View(model);
            }
        }

        // Đăng xuất
        public ActionResult DangXuat()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Trangchu", "Home");
        }

        // Trang thông tin cá nhân (User)
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

        // POST: Cập nhật thông tin cá nhân
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

        // Đổi mật khẩu
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

            // KIỂM TRA MẬT KHẨU CŨ TRỰC TIẾP (không mã hóa)
            if (user.MatKhau != MatKhauCu)
            {
                ViewBag.Error = "Mật khẩu cũ không đúng!";
                return View();
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu mới không khớp!";
                return View();
            }

            // LƯU MẬT KHẨU MỚI (không mã hóa)
            user.MatKhau = MatKhauMoi;
            user.NgayCapNhat = DateTime.Now;
            db.SaveChanges();

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }
    }
}