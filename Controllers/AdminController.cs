using Nhom06_QuanLyBanSah.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity; 
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Nhom06_QuanLyBanSah.Controllers
{
    public class AdminController : Controller
    {
        QUANLYBANSACH_NHOM06Entities db = new QUANLYBANSACH_NHOM06Entities();

        //Kiem tra quyen admin 
        private bool IsAdmin()
        {
            if (Session["Role"] == null) return false;//Nếu chưa đăng nhập → KHÔNG cho vào

            string role = Session["Role"].ToString();
            return role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        }

        // Trang chu admin
        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan"); //Không phải admin → đá về trang đăng nhập
            }

            ViewBag.TongSach = db.SACH.Count(s => s.IsDelete == false);
            ViewBag.TongDonHang = db.DONHANG.Count();
            ViewBag.TongKhachHang = db.TAIKHOAN
                .AsEnumerable()
                .Count(x => x.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
            ViewBag.TongDoanhThu = db.DONHANG.Sum(x => (decimal?)x.TongTien) ?? 0;//Đếm số tài khoản USER
                                                                                  //Tính tổng tiền

            //Nếu chưa có đơn hàng → không bị lỗi

        //ViewBag dùng để làm gì?

//Gửi dữ liệu từ Controller sang View
            return View();
        }


        // Quan ly tai khoan
        public ActionResult QuanLyTaiKhoan()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var danhSachTK = db.TAIKHOAN.OrderByDescending(x => x.NgayTao).ToList();//Lấy danh sách tài khoản mới nhất trước
            return View(danhSachTK);
        }

        // Chi tiet tai khoan
        public ActionResult ChiTietTaiKhoan(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = db.TAIKHOAN.Find(id);//Tìm đúng 1 tài khoản theo khóa chính
            if (tk == null)
            {
                return HttpNotFound();
            }

            ViewBag.DonHangs = db.DONHANG
                .Where(x => x.UserID == id)
                .OrderByDescending(x => x.NgayDatHang)
                .Take(10)
                .ToList();//Xem 10 đơn hàng gần nhất của tài khoản đó

            return View(tk);
        }

        // Sua tai khoan
        [HttpGet]
        public ActionResult SuaTaiKhoan(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = db.TAIKHOAN.Find(id);
            if (tk == null)
            {
                return HttpNotFound();
            }

            ViewBag.RoleList = new SelectList(new[] {
                new { Value = "admin", Text = "Admin" },
                new { Value = "user", Text = "User" }
            }, "Value", "Text", tk.Role);

            return View(tk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTaiKhoan(TAIKHOAN model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            ViewBag.RoleList = new SelectList(new[] {
                new { Value = "admin", Text = "Admin" },
                new { Value = "user", Text = "User" }
            }, "Value", "Text", model.Role);

            var tk = db.TAIKHOAN.Find(model.userID);
            if (tk != null)
            {
                var existEmail = db.TAIKHOAN
                    .FirstOrDefault(x => x.Email == model.Email && x.userID != model.userID);//Không cho 2 tài khoản dùng chung Email
                if (existEmail != null)
                {
                    ViewBag.Error = "Email đã được sử dụng bởi tài khoản khác!";
                    return View(model);
                }

                var existPhone = db.TAIKHOAN
                    .FirstOrDefault(x => x.DienThoai == model.DienThoai && x.userID != model.userID);
                if (existPhone != null)
                {
                    ViewBag.Error = "Số điện thoại đã được sử dụng bởi tài khoản khác!";
                    return View(model);
                }

                tk.HoTen = model.HoTen;//Ghi đè dữ liệu mới
                tk.NgaySinh = model.NgaySinh;
                tk.GioiTinh = model.GioiTinh;
                tk.DienThoai = model.DienThoai;
                tk.Email = model.Email;
                tk.DiaChi = model.DiaChi;

                tk.Role = model.Role.ToLower();
                tk.NgayCapNhat = DateTime.Now;

                db.SaveChanges();
                TempData["Success"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction("QuanLyTaiKhoan");
            }

            return View(model);
        }

        // Reset mat khau
        [HttpPost]
        public ActionResult ResetMatKhau(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = db.TAIKHOAN.Find(id);
            if (tk != null)
            {
                string matKhauMoi = tk.Role.Equals("admin", StringComparison.OrdinalIgnoreCase)
                    ? "admin123"
                    : "user123";//Admin hỗ trợ khi người dùng quên mật khẩu

                tk.MatKhau = matKhauMoi;
                tk.NgayCapNhat = DateTime.Now;
                db.SaveChanges();

                TempData["Success"] = $"Đã reset mật khẩu thành công! Mật khẩu mới: {matKhauMoi}";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
            }

            return RedirectToAction("ChiTietTaiKhoan", new { id = id });
        }

        [HttpPost]
        public ActionResult XoaTaiKhoan(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tk = db.TAIKHOAN.Find(id);
            if (tk != null)
            {
                if (Session["UserID"] != null && (int)Session["UserID"] == id)
                {
                    TempData["Error"] = "Không thể xóa tài khoản của chính mình!";
                    return RedirectToAction("QuanLyTaiKhoan");
                }

                var coDonHang = db.DONHANG.Any(x => x.UserID == id);
                if (coDonHang)
                {
                    TempData["Error"] = "Không thể xóa tài khoản đã có đơn hàng! Bạn có thể vô hiệu hóa tài khoản thay vì xóa.";
                    return RedirectToAction("QuanLyTaiKhoan");
                }

                var coPhieuNhap = db.PHIEUNHAP.Any(x => x.UserID == id);
                if (coPhieuNhap)
                {
                    TempData["Error"] = "Không thể xóa tài khoản đã tạo phiếu nhập!";
                    return RedirectToAction("QuanLyTaiKhoan");
                }

                db.TAIKHOAN.Remove(tk);
                db.SaveChanges();
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
            }

            return RedirectToAction("QuanLyTaiKhoan");
        }

        // Quan ly sach
        public ActionResult QuanLySach(string search, int? theLoai, string trangThai)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Lấy danh sách sách
            // Lấy danh sách sách
            var danhSachSach = db.SACH
     .Include(s => s.THELOAI)
     .Include(s => s.TACGIA)
     .Include(s => s.NHAXUATBAN)
     .Where(s => s.IsDelete == false)
     .AsQueryable();

            // Lọc theo tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                danhSachSach = danhSachSach.Where(x => x.TenSach.Contains(search));
                ViewBag.Search = search;
            }

            // Lọc theo thể loại
            if (theLoai.HasValue)
            {
                danhSachSach = danhSachSach.Where(x => x.MaTheLoai == theLoai.Value);
                ViewBag.TheLoaiSelected = theLoai;
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                switch (trangThai)
                {
                    case "conhang":
                        danhSachSach = danhSachSach.Where(x => x.SoLuongTon > 10); // Có thể điều chỉnh ngưỡng
                        break;
                    case "saphethang":
                        danhSachSach = danhSachSach.Where(x => x.SoLuongTon > 0 && x.SoLuongTon <= 10);
                        break;
                    case "hethang":
                        danhSachSach = danhSachSach.Where(x => x.SoLuongTon <= 0);
                        break;
                }
                ViewBag.TrangThaiSelected = trangThai;
            }

            ViewBag.TheLoaiList = db.THELOAI.ToList();

            return View(danhSachSach.OrderByDescending(x => x.MaSach).ToList());
        }

        //Them sach

        [HttpGet]
        public ActionResult ThemSach()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            ViewBag.TheLoaiList = new SelectList(db.THELOAI.OrderBy(x => x.TenTheLoai), "MaTheLoai", "TenTheLoai");
            ViewBag.TacGiaList = new SelectList(db.TACGIA.OrderBy(x => x.TenTacGia), "MaTacGia", "TenTacGia");
            ViewBag.NhaXuatBanList = new SelectList(db.NHAXUATBAN.OrderBy(x => x.TenNXB), "MaNXB", "TenNXB");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSach(SACH model, HttpPostedFileBase AnhBiaFile)
        {
            if (!IsAdmin())
                return RedirectToAction("DangNhap", "TaiKhoan");

            // Chuẩn hoá tên sách để check trùng
            model.TenSach = (model.TenSach ?? "").Trim();

            // Check trùng tên (không phân biệt hoa thường)
            bool trungTen = db.SACH.Any(s => s.TenSach.Trim().ToLower() == model.TenSach.ToLower());
            if (trungTen)
            {
                TempData["Error"] = "Đã có sách này rồi!";
                return RedirectToAction("QuanLySach");
            }

            if (!ModelState.IsValid)
            {
                // Nếu muốn khi sai validate vẫn ở trang ThemSach thì giữ đoạn này
                ViewBag.TheLoaiList = new SelectList(db.THELOAI.OrderBy(x => x.TenTheLoai), "MaTheLoai", "TenTheLoai", model.MaTheLoai);
                ViewBag.TacGiaList = new SelectList(db.TACGIA.OrderBy(x => x.TenTacGia), "MaTacGia", "TenTacGia", model.MaTacGia);
                ViewBag.NhaXuatBanList = new SelectList(db.NHAXUATBAN.OrderBy(x => x.TenNXB), "MaNXB", "TenNXB", model.MaNXB);
                return View(model);
            }

            // Upload ảnh
            if (AnhBiaFile != null && AnhBiaFile.ContentLength > 0)
            {
                string fileName = Path.GetFileName(AnhBiaFile.FileName);
                string path = Path.Combine(Server.MapPath("~/HinhSP"), fileName);
                AnhBiaFile.SaveAs(path);
                model.AnhBia = fileName;
            }
            else model.AnhBia = "default.jpg";

            // Giá trị mặc định
            model.SoLuongBan = model.SoLuongBan ?? 0;
            model.SoLuongTon = model.SoLuongTon ?? 0;
            model.NgayCapNhat = DateTime.Now;

            try
            {
                db.SACH.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Thêm sách thành công!";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                // Nếu có trường hợp 2 người thêm cùng lúc => vẫn bị UNIQUE ở DB
                TempData["Error"] = "Đã có sách này rồi!";
            }

            return RedirectToAction("QuanLySach");
        }
        public ActionResult ChiTietSach(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var sach = db.SACH
                .Include(s => s.THELOAI)
                .Include(s => s.TACGIA)
                .Include(s => s.NHAXUATBAN)
                .FirstOrDefault(x => x.MaSach == id);

            if (sach == null)
            {
                return HttpNotFound();
            }

            // Lấy thống kê bán hàng
            ViewBag.SoLuongDaBan = db.CHITIETDONHANG
                .Where(x => x.MaSach == id)
                .Sum(x => (int?)x.SoLuong) ?? 0;

            ViewBag.DoanhThu = db.CHITIETDONHANG
                .Where(x => x.MaSach == id)
                .Sum(x => (decimal?)(x.SoLuong * x.GiaBanTaiThoiDiem)) ?? 0;

            return View(sach);
        }

        // Sua sach
        [HttpGet]
        public ActionResult SuaSach(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var sach = db.SACH.Find(id);
            if (sach == null)
            {
                return HttpNotFound();
            }

            ViewBag.TheLoaiList = new SelectList(db.THELOAI.OrderBy(x => x.TenTheLoai), "MaTheLoai", "TenTheLoai", sach.MaTheLoai);
            ViewBag.TacGiaList = new SelectList(db.TACGIA.OrderBy(x => x.TenTacGia), "MaTacGia", "TenTacGia", sach.MaTacGia);
            ViewBag.NhaXuatBanList = new SelectList(db.NHAXUATBAN.OrderBy(x => x.TenNXB), "MaNXB", "TenNXB", sach.MaNXB);

            return View(sach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSach(SACH model, HttpPostedFileBase AnhBiaFile)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var sach = db.SACH.Find(model.MaSach);
            if (sach != null)
            {
                // Xử lý upload ảnh mới
                if (AnhBiaFile != null && AnhBiaFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(AnhBiaFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/HinhSP"), fileName);
                    AnhBiaFile.SaveAs(path);
                    sach.AnhBia = fileName;
                }

                // Cập nhật thông tin
                sach.TenSach = model.TenSach;
                sach.MaTheLoai = model.MaTheLoai;
                sach.MaTacGia = model.MaTacGia;
                sach.MaNXB = model.MaNXB;
                sach.GiaBan = model.GiaBan;
                sach.SoLuongTon = model.SoLuongTon;
                sach.MoTa = model.MoTa;
                sach.NamXuatBan = model.NamXuatBan;

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sách thành công!";
                return RedirectToAction("QuanLySach");
            }

            ViewBag.TheLoaiList = new SelectList(db.THELOAI.OrderBy(x => x.TenTheLoai), "MaTheLoai", "TenTheLoai", model.MaTheLoai);
            ViewBag.TacGiaList = new SelectList(db.TACGIA.OrderBy(x => x.TenTacGia), "MaTacGia", "TenTacGia", model.MaTacGia);
            ViewBag.NhaXuatBanList = new SelectList(db.NHAXUATBAN.OrderBy(x => x.TenNXB), "MaNXB", "TenNXB", model.MaNXB);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaSach(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Chỉ lấy sách CHƯA bị xóa mềm
            var sach = db.SACH.FirstOrDefault(s => s.MaSach == id && s.IsDelete == false);
            if (sach == null)
            {
                TempData["Error"] = "Không tìm thấy sách!";
                return RedirectToAction("QuanLySach");
            }

            // ✅ XÓA MỀM: KHÔNG remove, chỉ cập nhật IsDeleted
            sach.IsDelete = true;
            sach.NgayCapNhat = DateTime.Now;

            db.SaveChanges();

            TempData["Success"] = "Xóa sách thành công!";
            return RedirectToAction("QuanLySach");
        }





        //Quan ly don hang
        public ActionResult QuanLyDonHang(string search, string trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Load lại giá trị lọc cho View
            ViewBag.Search = search;
            ViewBag.TrangThaiSelected = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            var danhSachDH = db.DONHANG
                .Include(dh => dh.TAIKHOAN)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hoặc tên khách hàng
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                int maDH;
                if (int.TryParse(search, out maDH))
                {
                    danhSachDH = danhSachDH.Where(x => x.MaDonHang == maDH);
                }
                else
                {
                    // Chuyển sang tìm kiếm tên/email (sử dụng .AsEnumerable() nếu cần so sánh không phân biệt hoa/thường)
                    danhSachDH = danhSachDH.Where(x => x.TAIKHOAN.HoTen.Contains(search) ||
                                                       x.TAIKHOAN.Email.Contains(search));
                }
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                danhSachDH = danhSachDH.Where(x => x.TinhTrangGiaoHang == trangThai);
            }

            // Lọc theo ngày
            if (tuNgay.HasValue)
            {
                danhSachDH = danhSachDH.Where(x => x.NgayDatHang >= tuNgay.Value);
            }

            if (denNgay.HasValue)
            {
                var denNgayEnd = denNgay.Value.AddDays(1);
                danhSachDH = danhSachDH.Where(x => x.NgayDatHang < denNgayEnd);
            }

            return View(danhSachDH.OrderByDescending(x => x.NgayDatHang).ToList());
        }

       

        [HttpPost]
        public ActionResult CapNhatTrangThai(int id, string trangThai)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền!" });
            }

            var donHang = db.DONHANG.Find(id);
            if (donHang != null)
            {
                donHang.TinhTrangGiaoHang = trangThai;

                // Nếu đơn hàng đã giao thành công, cập nhật ngày giao
                if (trangThai == "Đã giao hàng thành công")
                {
                    donHang.NgayGiaoHang = DateTime.Now;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
        }


        public ActionResult ThongKeTongQuat()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Thống kê tổng quan
            ViewBag.TongSach = db.SACH.Count(s => s.IsDelete == false);
            ViewBag.TongDonHang = db.DONHANG.Count();

            // Đếm User và Admin
            var allUsers = db.TAIKHOAN.AsEnumerable();
            ViewBag.TongUser = allUsers.Count(x => x.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
            ViewBag.TongAdmin = allUsers.Count(x => x.Role.Equals("admin", StringComparison.OrdinalIgnoreCase));

            ViewBag.TongDoanhThu = db.DONHANG.Sum(x => (decimal?)x.TongTien) ?? 0;

            // Đơn hàng theo trạng thái
            ViewBag.ChoXuLy = db.DONHANG.Count(x => x.TinhTrangGiaoHang == "Chờ xử lý");
            ViewBag.DaXacNhan = db.DONHANG.Count(x => x.TinhTrangGiaoHang == "Đã xác nhận");
            ViewBag.DangVanChuyen = db.DONHANG.Count(x => x.TinhTrangGiaoHang == "Đang vận chuyển");
            ViewBag.DaGiaoHang = db.DONHANG.Count(x => x.TinhTrangGiaoHang == "Đã giao hàng thành công");
            ViewBag.DaHuy = db.DONHANG.Count(x => x.TinhTrangGiaoHang == "Đã hủy");

            // Top 5 sách bán chạy
            var topSach = db.SACH
   .Include(s => s.THELOAI)
   .Where(s => s.IsDelete == false)
   .OrderByDescending(x => x.SoLuongBan)
   .Take(5)
   .ToList();
            ViewBag.TopSach = topSach;

            // Top 5 khách hàng mua nhiều
            var topKhachHang = db.DONHANG
                .Where(x => x.TinhTrangGiaoHang == "Đã giao hàng thành công")
                .GroupBy(x => x.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    TongDonHang = g.Count(),
                    TongTien = g.Sum(x => x.TongTien)
                })
                .OrderByDescending(x => x.TongTien)
                .Take(5)
                .ToList();

            ViewBag.TopKhachHang = topKhachHang;

            return View();
        }
        // Thêm vào AdminController.cs

        // ===================== QUẢN LÝ HOÀN TRẢ =====================

        public ActionResult QuanLyHoanTra(string search = "", string trangThai = "")
        {
            var danhSach = db.HOANTRA.Include("DONHANG").Include("DONHANG.TAIKHOAN").AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                int maSearch = 0;
                int.TryParse(search, out maSearch);

                danhSach = danhSach.Where(h =>
                    h.MaDonHang == maSearch ||
                    h.DONHANG.TAIKHOAN.HoTen.Contains(search) ||
                    h.LyDo.Contains(search)
                );
                ViewBag.Search = search;
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                danhSach = danhSach.Where(h => h.TrangThai == trangThai);
                ViewBag.TrangThai = trangThai;
            }

            danhSach = danhSach.OrderByDescending(h => h.NgayYeuCau);

            return View(danhSach.ToList());
        }

        // Xử lý yêu cầu hủy
        [HttpPost]
        public ActionResult XuLyYeuCauHuy(int maHoanTra, string hanhDong, string ghiChu = "")
        {
            try
            {
                var hoanTra = db.HOANTRA.Find(maHoanTra);
                if (hoanTra == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy yêu cầu!" });
                }

                var donHang = db.DONHANG.Find(hoanTra.MaDonHang);
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                if (hanhDong == "chap_nhan")
                {
                    // Chấp nhận hủy
                    hoanTra.TrangThai = "Đã xử lý";
                    hoanTra.NgayYeuCau = DateTime.Now;
                    hoanTra.LyDo = ghiChu;

                    donHang.TinhTrangGiaoHang = "Đã hủy";

                    // Hoàn lại số lượng sách
                    var chiTietDonHang = db.CHITIETDONHANG.Where(c => c.MaDonHang == donHang.MaDonHang).ToList();
                    foreach (var item in chiTietDonHang)
                    {
                        var sach = db.SACH.Find(item.MaSach);
                        if (sach != null)
                        {
                            sach.SoLuongTon += item.SoLuong;
                            sach.SoLuongBan -= item.SoLuong;
                        }
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã chấp nhận hủy đơn hàng!" });
                }
                else if (hanhDong == "tu_choi")
                {
                    // Từ chối hủy
                    hoanTra.TrangThai = "Từ chối";
                    hoanTra.NgayYeuCau = DateTime.Now;
                    hoanTra.LyDo = ghiChu;

                    donHang.TinhTrangGiaoHang = "Đã xác nhận"; // Quay về trạng thái xác nhận

                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã từ chối yêu cầu hủy đơn hàng!" });
                }

                return Json(new { success = false, message = "Hành động không hợp lệ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Cập nhật action ChiTietDonHang để hiển thị thông tin hoàn trả
        public ActionResult ChiTietDonHang(int id)
        {
            var donHang = db.DONHANG.Find(id);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang");
            }

            // Lấy thông tin yêu cầu hủy (nếu có)
            var yeuCauHuy = db.HOANTRA.FirstOrDefault(h => h.MaDonHang == id);
            ViewBag.YeuCauHuy = yeuCauHuy;

            return View(donHang);
        }
        // Thêm vào AdminController.cs

        // ==================== PHẦN CODE SAU ACTION ThongKe ====================
        // Thêm vào cuối action ThongKe (sau phần doanhThuTheoTheLoai)

        public ActionResult ThongKe(DateTime? tuNgay, DateTime? denNgay)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            // Nếu không có ngày, mặc định lấy 30 ngày gần nhất
            if (!tuNgay.HasValue)
            {
                tuNgay = DateTime.Now.AddDays(-30);
            }

            if (!denNgay.HasValue)
            {
                denNgay = DateTime.Now;
            }

            // Đảm bảo denNgay bao gồm cả ngày đó
            var denNgayEnd = denNgay.Value.AddDays(1);

            ViewBag.TuNgay = tuNgay.Value.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay.Value.ToString("yyyy-MM-dd");

            // ========== THỐNG KÊ TỔNG QUAN ==========
            ViewBag.TongSach = db.SACH.Count(s => s.IsDelete == false);
            ViewBag.TongDonHang = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd);

            var allUsers = db.TAIKHOAN.AsEnumerable();
            ViewBag.TongUser = allUsers.Count(x => x.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
            ViewBag.TongAdmin = allUsers.Count(x => x.Role.Equals("admin", StringComparison.OrdinalIgnoreCase));

            ViewBag.TongDoanhThu = db.DONHANG
                .Where(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đã giao hàng thành công")
                .Sum(x => (decimal?)x.TongTien) ?? 0;

            // ========== ĐƠN HÀNG THEO TRẠNG THÁI ==========
            ViewBag.ChoXuLy = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Chờ xử lý");

            ViewBag.DaXacNhan = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đã xác nhận");

            ViewBag.DangVanChuyen = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đang vận chuyển");

            ViewBag.DaGiaoHang = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đã giao hàng thành công");

            ViewBag.DaHuy = db.DONHANG
                .Count(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đã hủy");

            // ========== TOP 5 SÁCH BÁN CHẠY ==========
            var topSach = (from ctdh in db.CHITIETDONHANG
                           join dh in db.DONHANG on ctdh.MaDonHang equals dh.MaDonHang
                           join s in db.SACH on ctdh.MaSach equals s.MaSach
                           where dh.NgayDatHang >= tuNgay && dh.NgayDatHang < denNgayEnd
&& dh.TinhTrangGiaoHang == "Đã giao hàng thành công"
&& s.IsDelete == false
                           group ctdh by new { s.MaSach, s.TenSach, s.GiaBan, s.AnhBia } into g
                           orderby g.Sum(x => x.SoLuong) descending
                           select new
                           {
                               MaSach = g.Key.MaSach,
                               TenSach = g.Key.TenSach,
                               SoLuongBan = g.Sum(x => x.SoLuong),
                               DoanhThu = g.Sum(x => x.SoLuong * x.GiaBanTaiThoiDiem),
                               AnhBia = g.Key.AnhBia
                           }).Take(5).ToList();

            ViewBag.TopSach = topSach;

            // ========== TOP 5 KHÁCH HÀNG MUA NHIỀU ==========
            var topKhachHang = (from dh in db.DONHANG
                                join tk in db.TAIKHOAN on dh.UserID equals tk.userID
                                where dh.NgayDatHang >= tuNgay && dh.NgayDatHang < denNgayEnd
                                   && dh.TinhTrangGiaoHang == "Đã giao hàng thành công"
                                group dh by new { tk.userID, tk.HoTen, tk.Email } into g
                                orderby g.Sum(x => x.TongTien) descending
                                select new
                                {
                                    UserID = g.Key.userID,
                                    HoTen = g.Key.HoTen,
                                    Email = g.Key.Email,
                                    TongDonHang = g.Count(),
                                    TongTien = g.Sum(x => x.TongTien)
                                }).Take(5).ToList();

            ViewBag.TopKhachHang = topKhachHang;

            // ========== DOANH THU THEO NGÀY ==========
            var doanhThuTheoNgay = db.DONHANG
                .Where(x => x.NgayDatHang >= tuNgay && x.NgayDatHang < denNgayEnd
                         && x.TinhTrangGiaoHang == "Đã giao hàng thành công")
                .GroupBy(x => DbFunctions.TruncateTime(x.NgayDatHang))
                .Select(g => new
                {
                    Ngay = g.Key,
                    DoanhThu = g.Sum(x => x.TongTien),
                    SoDonHang = g.Count()
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            ViewBag.DoanhThuTheoNgay = doanhThuTheoNgay;

            // ========== DOANH THU THEO THỂ LOẠI ==========
            var doanhThuTheoTheLoai = (from ctdh in db.CHITIETDONHANG
                                       join dh in db.DONHANG on ctdh.MaDonHang equals dh.MaDonHang
                                       join s in db.SACH on ctdh.MaSach equals s.MaSach
                                       join tl in db.THELOAI on s.MaTheLoai equals tl.MaTheLoai
                                       where dh.NgayDatHang >= tuNgay && dh.NgayDatHang < denNgayEnd
                                          && dh.TinhTrangGiaoHang == "Đã giao hàng thành công"
                                       group ctdh by tl.TenTheLoai into g
                                       orderby g.Sum(x => x.SoLuong * x.GiaBanTaiThoiDiem) descending
                                       select new
                                       {
                                           TheLoai = g.Key,
                                           DoanhThu = g.Sum(x => x.SoLuong * x.GiaBanTaiThoiDiem),
                                           SoLuongBan = g.Sum(x => x.SoLuong)
                                       }).Take(10).ToList();

            ViewBag.DoanhThuTheoTheLoai = doanhThuTheoTheLoai;

            return View();
        }

        // ========== KẾT THÚC CLASS AdminController ==========
        // Đóng ngoặc nhọn của class
    }
}

    