using Nhom06_QuanLyBanSah.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Nhom06_QuanLyBanSah.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        QUANLYBANSACH_NHOM06Entities5 db = new QUANLYBANSACH_NHOM06Entities5();

        public ActionResult Trangchu()
        {
            var listSach = db.SACH.Where(s => s.IsDelete == false).ToList();
            return View(listSach);
        }

        public ActionResult GioiThieu()
        {
            return View();
        }

        public ActionResult TinTuc()
        {
            return View();
        }

        public ActionResult LienHe()
        {

            return View();
        }

        public ActionResult SanPham(int page = 1, string sortOrder = "name_asc")
        {
            int pageSize = 12;
            var sachs = db.SACH.Include("THELOAI").Include("TACGIA")
                               .Where(s => s.IsDelete == false)
                               .AsQueryable();

            switch (sortOrder)
            {
                case "name_desc":
                    sachs = sachs.OrderByDescending(s => s.TenSach);
                    break;
                case "price_asc":
                    sachs = sachs.OrderBy(s => s.GiaBan);
                    break;
                case "price_desc":
                    sachs = sachs.OrderByDescending(s => s.GiaBan);
                    break;
                default:
                    sachs = sachs.OrderBy(s => s.TenSach);
                    break;
            }

            int totalItems = sachs.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var items = sachs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SLSach = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentSort = sortOrder;

            return View(items);
        }

        public ActionResult SearchSanPham(int? maTheLoai, string keyword, string sortOrder, int page = 1, int pageSize = 12)
        {
            var sachs = db.SACH
                .Include("THELOAI")
                .Include("TACGIA")
                .Where(s => s.IsDelete == false)
                .AsQueryable();

            // lọc theo thể loại (nếu chọn)
            if (maTheLoai.HasValue)
            {
                sachs = sachs.Where(s => s.MaTheLoai == maTheLoai.Value);
                ViewBag.MaTheLoai = maTheLoai;
            }

            // ✅ tìm theo keyword: tên sách OR tên tác giả OR tên thể loại
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                sachs = sachs.Where(s =>
                    s.TenSach.Contains(keyword) ||
                    s.TACGIA.TenTacGia.Contains(keyword) ||
                    s.THELOAI.TenTheLoai.Contains(keyword)
                );

                ViewBag.Keyword = keyword;
            }

            // sort
            switch (sortOrder)
            {
                case "az": sachs = sachs.OrderBy(s => s.TenSach); break;
                case "za": sachs = sachs.OrderByDescending(s => s.TenSach); break;
                case "priceAsc": sachs = sachs.OrderBy(s => s.GiaBan); break;
                case "priceDesc": sachs = sachs.OrderByDescending(s => s.GiaBan); break;
                default: sortOrder = "az"; sachs = sachs.OrderBy(s => s.TenSach); break;
            }

            int totalItems = sachs.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(totalPages, 1)));

            var items = sachs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SLSach = totalItems;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(items);
        }


        // Thêm vào HomeController.cs

        // Action hiển thị chi tiết sản phẩm (CẬP NHẬT)
        public ActionResult ChiTietSanPham(int ms)
        {
            SACH s = db.SACH.FirstOrDefault(x => x.MaSach == ms && x.IsDelete == false);
            if (s == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách đánh giá
            var danhGias = db.DANHGIA
                .Where(d => d.MaSach == ms)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToList();

            ViewBag.DanhGias = danhGias;

            // Tính điểm trung bình
            if (danhGias.Any())
            {
                ViewBag.DiemTrungBinh = danhGias.Average(d => d.SoSao);
                ViewBag.TongDanhGia = danhGias.Count;
            }
            else
            {
                ViewBag.DiemTrungBinh = 0;
                ViewBag.TongDanhGia = 0;
            }

            // Kiểm tra xem user đã mua và nhận hàng thành công chưa
            bool daMuaThanhCong = false;
            if (Session["UserID"] != null)
            {
                int userID = (int)Session["UserID"];

                daMuaThanhCong = db.CHITIETDONHANG
                    .Any(ct => ct.MaSach == ms &&
                         ct.DONHANG.UserID == userID &&
                         ct.DONHANG.TinhTrangGiaoHang == "Đã giao hàng thành công");

                ViewBag.DaMuaThanhCong = daMuaThanhCong;

                // Kiểm tra đã đánh giá chưa
                ViewBag.DaDanhGia = db.DANHGIA.Any(d => d.MaSach == ms && d.UserID == userID);
            }
            else
            {
                ViewBag.DaMuaThanhCong = false;
                ViewBag.DaDanhGia = false;
            }

            // Thống kê số sao
            var thongKeSao = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
            {
                thongKeSao[i] = danhGias.Count(d => d.SoSao == i);
            }
            ViewBag.ThongKeSao = thongKeSao;

            return View(s);
        }

        // Action thêm đánh giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemDanhGia(int maSach, int soSao, string noiDung)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    TempData["Error"] = "Bạn phải đăng nhập để đánh giá!";
                    return RedirectToAction("DangNhap", "TaiKhoan");
                }

                int userID = (int)Session["UserID"];

                // Kiểm tra đã mua và nhận hàng thành công chưa
                bool daMuaThanhCong = db.CHITIETDONHANG
                    .Any(ct => ct.MaSach == maSach &&
                         ct.DONHANG.UserID == userID &&
                         ct.DONHANG.TinhTrangGiaoHang == "Đã giao hàng thành công");

                if (!daMuaThanhCong)
                {
                    TempData["Error"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua và nhận hàng thành công!";
                    return RedirectToAction("ChiTietSanPham", new { ms = maSach });
                }

                // Kiểm tra đã đánh giá chưa
                var danhGiaCu = db.DANHGIA.FirstOrDefault(d => d.MaSach == maSach && d.UserID == userID);

                if (danhGiaCu != null)
                {
                    // Cập nhật đánh giá cũ
                    danhGiaCu.SoSao = soSao;
                    danhGiaCu.NoiDung = noiDung;
                    danhGiaCu.NgayDanhGia = DateTime.Now;
                }
                else
                {
                    // Thêm đánh giá mới
                    var danhGia = new DANHGIA
                    {
                        MaSach = maSach,
                        UserID = userID,
                        SoSao = soSao,
                        NoiDung = noiDung,
                        NgayDanhGia = DateTime.Now
                    };
                    db.DANHGIA.Add(danhGia);
                }

                db.SaveChanges();
                TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToAction("ChiTietSanPham", new { ms = maSach });
        }

        // Action xóa đánh giá
        [HttpPost]
        public ActionResult XoaDanhGia(int maDanhGia)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
                }

                int userID = (int)Session["UserID"];
                var danhGia = db.DANHGIA.Find(maDanhGia);

                if (danhGia == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá!" });
                }

                if (danhGia.UserID != userID)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá này!" });
                }

                int maSach = danhGia.MaSach;
                db.DANHGIA.Remove(danhGia);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã xóa đánh giá!", redirectUrl = Url.Action("ChiTietSanPham", new { ms = maSach }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public ActionResult DanhMucTheLoai()
        {
            var listTL = db.THELOAI.OrderBy(cd => cd.TenTheLoai).ToList();
            return View(listTL);
        }

        public ActionResult SachttheoTL(int? maTheLoai, string sortOrder, int page = 1, int pageSize = 12)
        {
            ViewBag.MaTheLoai = new SelectList(db.THELOAI.ToList(), "MaTheLoai", "TenTheLoai", maTheLoai);

            if (string.IsNullOrEmpty(sortOrder))
            {
                sortOrder = "name_asc";
            }
            ViewBag.CurrentSort = sortOrder;

            var sachs = db.SACH.Include("THELOAI").Include("TACGIA")
                                          .Where(s => s.IsDelete == false)
                                          .AsQueryable();
            if (maTheLoai != null)
            {
                sachs = sachs.Where(s => s.MaTheLoai == maTheLoai);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    sachs = sachs.OrderByDescending(s => s.TenSach);
                    break;
                case "price_asc":
                    sachs = sachs.OrderBy(s => s.GiaBan);
                    break;
                case "price_desc":
                    sachs = sachs.OrderByDescending(s => s.GiaBan);
                    break;
                default:
                    sachs = sachs.OrderBy(s => s.TenSach);
                    break;
            }

            int totalItems = sachs.Count();

            if (page < 1) page = 1;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sachs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SLSach = totalItems;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SelectedTheLoai = maTheLoai;

            return View(items);
        }

        public List<GioHang> LayGioHang()
        {
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang == null)
            {
                lstGioHang = new List<GioHang>();
                Session["GioHang"] = lstGioHang;
            }
            return lstGioHang;
        }

        //Thêm giỏ hàng
        public ActionResult ThemGioHang(int ms, string strURL)
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng!";

                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var sach = db.SACH.FirstOrDefault(x => x.MaSach == ms && x.IsDelete == false);
            if (sach == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return Redirect(strURL ?? "/");
            }

            if (sach.SoLuongTon <= 0)
            {
                TempData["Error"] = "Rất tiếc, sản phẩm này đã tạm hết hàng!";
                return Redirect(strURL ?? "/");
            }

            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanPham = lstGioHang.Find(sp => sp.iMaSach == ms);

            if (sanPham == null)
            {
                double giaBan = sach.GiaBan.HasValue ? Convert.ToDouble(sach.GiaBan.Value) : 0;
                sanPham = new GioHang(ms, sach.TenSach, sach.AnhBia, giaBan, 1);
                lstGioHang.Add(sanPham);
            }
            else
            {
                if (sanPham.iSoLuong + 1 > sach.SoLuongTon)
                {
                    TempData["Error"] = $"Cửa hàng chỉ còn tối đa {sach.SoLuongTon} cuốn cho tựa sách này!";
                    return Redirect(strURL ?? "/"); 
                }
                sanPham.iSoLuong++;
            }

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";

            return RedirectToAction("SanPham", "Home");
        }

        private int TongSoLuong()
        {
            int tsl = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                tsl += lstGioHang.Sum(sp => sp.iSoLuong);
            }
            return tsl;
        }

        private double TongThanhTien()
        {
            double ttt = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                ttt += lstGioHang.Sum(sp => sp.ThanhTien);
            }
            return ttt;
        }

        public ActionResult GioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();

            // Tính toán tổng số lượng và tổng tiền (kể cả khi giỏ hàng trống)
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongThanhTien = TongThanhTien();

            return View(lstGioHang);
        }

        public ActionResult GioHangPartial()
        {
            ViewBag.TongSoLuong = TongSoLuong();
            return PartialView();
        }

        public ActionResult XoaGioHang(int MaSP)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sp = lstGioHang.Single(s => s.iMaSach == MaSP);

            if (sp != null)
            {
                lstGioHang.RemoveAll(s => s.iMaSach == MaSP);
                return RedirectToAction("GioHang", "Home");
            }
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("GioHang", "Home");
            }
            return RedirectToAction("GioHang", "Home");
        }

        public ActionResult XoaGioHang_All()
        {
            List<GioHang> lstGioHang = LayGioHang();
            lstGioHang.Clear();
            return RedirectToAction("GioHang", "Home");
        }

        [HttpPost]
        public ActionResult CapNhatGioHang(int MaSP, FormCollection f)
        {
            List<GioHang> lstGioHang = LayGioHang();

            // Tối ưu: Dùng FirstOrDefault
            GioHang sp = lstGioHang.FirstOrDefault(s => s.iMaSach == MaSP);

            if (sp != null)
            {
                // Dùng TryParse để tránh lỗi hệ thống nếu nhập chữ
                if (int.TryParse(f["txtSoLuong"], out int soLuongMoi))
                {
                    if (soLuongMoi <= 0)
                    {
                        // YÊU CẦU 1: Nhập số 0 hoặc âm -> Xóa luôn khỏi giỏ
                        lstGioHang.Remove(sp);
                        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                    }
                    else
                    {
                        // KIỂM TRA TỒN KHO LẦN NỮA
                        var sach = db.SACH.FirstOrDefault(x => x.MaSach == MaSP);

                        if (sach != null)
                        {
                            if (soLuongMoi > sach.SoLuongTon)
                            {
                                // YÊU CẦU 2: Vượt tồn kho -> Chỉ báo lỗi, KHÔNG TỰ ĐỘNG SỬA SỐ
                                TempData["Error"] = $"Rất tiếc, sản phẩm này chỉ còn tối đa {sach.SoLuongTon} cuốn trong kho!";
                                // Lưu ý: Ta không chạm vào sp.iSoLuong ở đây, nên giỏ hàng sẽ giữ nguyên số cũ
                            }
                            else
                            {
                                // Hợp lệ -> Cập nhật số lượng mới
                                sp.iSoLuong = soLuongMoi;
                                TempData["Success"] = "Cập nhật số lượng thành công!";
                            }
                        }
                    }
                }
                else
                {
                    TempData["Error"] = "Vui lòng nhập số lượng hợp lệ!";
                }
            }

            return RedirectToAction("GioHang", "Home");
        }

        [HttpGet]
        public ActionResult DatHang()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "Bạn phải đăng nhập trước khi đặt hàng!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var gioHang = Session["GioHang"] as List<GioHang>;
            if (gioHang == null || !gioHang.Any())
            {
                return RedirectToAction("SanPham", "Home");
            }

            // ĐOẠN NÀY LẤY THÔNG TIN TÀI KHOẢN ĐỂ ĐIỀN SẴN RA VIEW
            int userID = (int)Session["UserID"];
            var user = db.TAIKHOAN.Find(userID);
            if (user != null)
            {
                ViewBag.DienThoaiMacDinh = user.DienThoai;
                ViewBag.DiaChiMacDinh = user.DiaChi;
            }

            return View(gioHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatHang(string diaChiGiaoHang, string soDienThoai)
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "Bạn phải đăng nhập!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var gioHang = Session["GioHang"] as List<GioHang>;
            if (gioHang == null || !gioHang.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("GioHang");
            }

            int userID = (int)Session["UserID"];
            var user = db.TAIKHOAN.Find(userID);

            // LOGIC TỰ ĐỘNG ĐIỀN THÔNG TIN (Nếu khách để trống)
            string finalPhone = string.IsNullOrWhiteSpace(soDienThoai) ? user.DienThoai : soDienThoai.Trim();
            string finalAddress = string.IsNullOrWhiteSpace(diaChiGiaoHang) ? user.DiaChi : diaChiGiaoHang.Trim();

            decimal tongTien = gioHang.Sum(g => g.iSoLuong * (decimal)g.dDonGia);

            // Kiểm tra voucher
            decimal soTienGiam = 0;
            VOUCHER voucherApDung = Session["VoucherApDung"] as VOUCHER;
            if (voucherApDung != null && Session["SoTienGiam"] != null)
            {
                soTienGiam = (decimal)Session["SoTienGiam"];
                tongTien -= soTienGiam;
            }

            // TẠO ĐƠN HÀNG MỚI (Lưu đúng tên cột bị thiếu chữ "i" dưới SQL)
            var donHang = new DONHANG
            {
                UserID = userID,
                NgayDatHang = DateTime.Now,
                DiaChiGiaoHang = finalAddress,
                SoDienThoaGiaoHang = finalPhone,        // Đã đồng bộ với SQL
                TongTien = tongTien,
                PhuongThucThanhToan = null,
                TinhTrangGiaoHang = "Chờ xử lý"
            };

            db.DONHANG.Add(donHang);
            db.SaveChanges();

            // Lưu thông tin voucher
            if (voucherApDung != null)
            {
                var donHangVoucher = new DONHANG_VOUCHER
                {
                    MaDonHang = donHang.MaDonHang,
                    MaVoucher = voucherApDung.MaVoucher,
                    SoTienGiam = soTienGiam
                };
                db.DONHANG_VOUCHER.Add(donHangVoucher);

                // Giảm số lượng voucher
                var voucher = db.VOUCHER.Find(voucherApDung.MaVoucher);
                if (voucher != null)
                {
                    voucher.SoLuong--;
                    if (voucher.SoLuong <= 0) voucher.TrangThai = false;
                }
            }

            // Lưu chi tiết đơn hàng (Cập nhật tồn kho)
            foreach (var item in gioHang)
            {
                var chiTiet = new CHITIETDONHANG
                {
                    MaDonHang = donHang.MaDonHang,
                    MaSach = item.iMaSach,
                    SoLuong = item.iSoLuong,
                    GiaBanTaiThoiDiem = (decimal)item.dDonGia
                };
                db.CHITIETDONHANG.Add(chiTiet);

                var sach = db.SACH.Find(item.iMaSach);
                if (sach != null)
                {
                    sach.SoLuongTon -= item.iSoLuong;
                    sach.SoLuongBan += item.iSoLuong;
                }
            }

            db.SaveChanges();

            // Xóa session
            Session["MaDonHang"] = donHang.MaDonHang;
            Session["GioHang"] = null;
            Session["VoucherApDung"] = null;
            Session["SoTienGiam"] = null;

            return RedirectToAction("ThanhToan", "Home", new { id = donHang.MaDonHang });
        }
        // ===================== LỊCH SỬ & CHI TIẾT ĐƠN HÀNG =====================

        public ActionResult LichSuDonHang()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            int userID = (int)Session["UserID"];

            var danhSach = db.DONHANG
                .Where(d => d.UserID == userID)
                .OrderByDescending(d => d.NgayDatHang)
                .ToList();

            return View(danhSach);
        }

        // Thêm vào HomeController.cs

        // Action hiển thị chi tiết đơn hàng (CẬP NHẬT)
        public ActionResult ChiTietDonHang(int ma)
        {
            var chiTiet = db.CHITIETDONHANG.Where(c => c.MaDonHang == ma).ToList();
            var don = db.DONHANG.Find(ma);

            if (don == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("LichSuDonHang");
            }

            // Kiểm tra quyền sở hữu
            if (Session["UserID"] != null && don.UserID != (int)Session["UserID"])
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này!";
                return RedirectToAction("LichSuDonHang");
            }

            ViewBag.Don = don;

            // Kiểm tra xem đã có yêu cầu hủy chưa
            var yeuCauHuy = db.HOANTRA.FirstOrDefault(h => h.MaDonHang == ma);
            ViewBag.YeuCauHuy = yeuCauHuy;

            return View(chiTiet);
        }

        // ===================== HỦY ĐƠN HÀNG MỚI =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeuCauHuyDonHang(int maDonHang, string lyDoHuy)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var donHang = db.DONHANG.Find(maDonHang);
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Kiểm tra quyền
                if (donHang.UserID != (int)Session["UserID"])
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy đơn hàng này!" });
                }

                // Chỉ cho phép hủy khi "Chờ xử lý"
                if (donHang.TinhTrangGiaoHang != "Chờ xử lý")
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái hiện tại!" });
                }

                // Kiểm tra đã có yêu cầu hủy chưa
                var yeuCauCu = db.HOANTRA.FirstOrDefault(h => h.MaDonHang == maDonHang);
                if (yeuCauCu != null)
                {
                    return Json(new { success = false, message = "Đơn hàng này đã có yêu cầu hủy!" });
                }

                // Tạo yêu cầu hủy
                var yeuCauHuy = new HOANTRA
                {
                    MaDonHang = maDonHang,
                    LyDo = lyDoHuy,
                    TrangThai = "Chờ xử lý",
                    NgayYeuCau = DateTime.Now
                };

                db.HOANTRA.Add(yeuCauHuy);

                // Cập nhật trạng thái đơn hàng
                donHang.TinhTrangGiaoHang = "Yêu cầu hủy";

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Yêu cầu hủy đơn hàng đã được gửi! Admin sẽ xử lý trong thời gian sớm nhất."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        // Action hủy yêu cầu hủy đơn (nếu khách đổi ý)
        [HttpPost]
        public ActionResult HuyYeuCauHuyDonHang(int maDonHang)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var donHang = db.DONHANG.Find(maDonHang);
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Kiểm tra quyền
                if (donHang.UserID != (int)Session["UserID"])
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" });
                }

                // Xóa yêu cầu hủy
                var yeuCauHuy = db.HOANTRA.FirstOrDefault(h => h.MaDonHang == maDonHang && h.TrangThai == "Chờ xử lý");
                if (yeuCauHuy != null)
                {
                    db.HOANTRA.Remove(yeuCauHuy);
                    donHang.TinhTrangGiaoHang = "Chờ xử lý";
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã hủy yêu cầu hủy đơn hàng!" });
                }

                return Json(new { success = false, message = "Không tìm thấy yêu cầu hủy!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        // ===================== THANH TOÁN =====================

        [HttpGet]
        public ActionResult ThanhToan(int id)
        {
            try
            {
                var donHang = db.DONHANG.Find(id);
                if (donHang == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("GioHang");
                }

                // Kiểm tra quyền sở hữu
                if (Session["UserID"] != null && donHang.UserID != (int)Session["UserID"])
                {
                    TempData["Error"] = "Bạn không có quyền truy cập đơn hàng này!";
                    return RedirectToAction("LichSuDonHang");
                }

                // Kiểm tra trạng thái
                if (donHang.PhuongThucThanhToan != null)
                {
                    TempData["Info"] = "Đơn hàng này đã được thanh toán!";
                    return RedirectToAction("ChiTietDonHang", new { ma = id });
                }

                var chiTiet = db.CHITIETDONHANG.Where(c => c.MaDonHang == id).ToList();
                ViewBag.DonHang = donHang;

                return View(chiTiet);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("GioHang");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThanhToan(int maDonHang, string phuongThucThanhToan)
        {
            try
            {
                var donHang = db.DONHANG.FirstOrDefault(d => d.MaDonHang == maDonHang);
                if (donHang == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("GioHang");
                }

                // Kiểm tra quyền
                if (Session["UserID"] != null && donHang.UserID != (int)Session["UserID"])
                {
                    TempData["Error"] = "Bạn không có quyền thanh toán đơn hàng này!";
                    return RedirectToAction("LichSuDonHang");
                }

                // Đã thanh toán
                if (donHang.PhuongThucThanhToan != null && donHang.TinhTrangThanhToan == "Đã thanh toán")
                {
                    TempData["Info"] = "Đơn hàng đã được thanh toán!";
                    return RedirectToAction("ChiTietDonHang", new { ma = maDonHang });
                }

                // 1. THANH TOÁN COD (Giao hàng thu tiền)
                if (phuongThucThanhToan == "Thanh toán khi nhận hàng (COD)")
                {
                    donHang.PhuongThucThanhToan = "COD";
                    donHang.TinhTrangGiaoHang = "Chờ xử lý";
                    donHang.TinhTrangThanhToan = "Chưa thanh toán"; // <--- LƯU TRẠNG THÁI COD

                    db.SaveChanges();

                    TempData["Success"] = "Đặt hàng thành công! Bạn sẽ thanh toán khi nhận hàng.";
                    return RedirectToAction("DatHangThanhCong", new { id = maDonHang });
                }

                // 2. THANH TOÁN VNPAY (Chuyển khoản trực tuyến)
                if (phuongThucThanhToan == "VNPay")
                {
                    donHang.PhuongThucThanhToan = "VNPay";
                    donHang.TinhTrangThanhToan = "Chờ thanh toán"; // <--- LƯU TRẠNG THÁI CHỜ VNPAY

                    // Phải lưu trạng thái vào CSDL TRƯỚC KHI chuyển hướng người dùng sang trang của VNPay
                    db.SaveChanges();

                    return ProcessVNPayPayment(donHang);
                }

                TempData["Error"] = "Phương thức thanh toán không hợp lệ!";
                return RedirectToAction("ThanhToan", new { id = maDonHang });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi thanh toán: " + ex.Message;
                return RedirectToAction("ThanhToan", new { id = maDonHang });
            }
        }

        // ===================== ĐẶT HÀNG THÀNH CÔNG =====================

        public ActionResult DatHangThanhCong(int? id)
        {
            if (!id.HasValue)
            {
                TempData["Error"] = "Không tìm thấy thông tin đơn hàng!";
                return RedirectToAction("LichSuDonHang");
            }

            var donHang = db.DONHANG.Find(id.Value);

            if (donHang == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại!";
                return RedirectToAction("LichSuDonHang");
            }

            // Kiểm tra quyền
            if (Session["UserID"] != null && donHang.UserID != (int)Session["UserID"])
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này!";
                return RedirectToAction("LichSuDonHang");
            }

            ViewBag.DonHang = donHang;
            return View();
        }

        // ===================== HỦY ĐƠN HÀNG =====================

        public ActionResult HuyDonHang(int id)
        {
            try
            {
                var donHang = db.DONHANG.Find(id);
                if (donHang == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("LichSuDonHang");
                }

                // Kiểm tra quyền
                if (Session["UserID"] != null && donHang.UserID != (int)Session["UserID"])
                {
                    TempData["Error"] = "Bạn không có quyền hủy đơn hàng này!";
                    return RedirectToAction("LichSuDonHang");
                }

                // Chỉ cho phép hủy nếu chưa thanh toán hoặc đang chờ xử lý
                if (donHang.TinhTrangGiaoHang == "Chờ xử lý" || donHang.PhuongThucThanhToan == null)
                {
                    donHang.TinhTrangGiaoHang = "Đã hủy";

                    // Hoàn lại số lượng sách
                    var chiTietDonHang = db.CHITIETDONHANG.Where(c => c.MaDonHang == id).ToList();
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
                    TempData["Success"] = "Đã hủy đơn hàng thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể hủy đơn hàng đã được xử lý!";
                }

                return RedirectToAction("LichSuDonHang");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("LichSuDonHang");
            }
        }



        // ==========================================
        // 1. TẠO URL GỬI SANG VNPAY (CHỐNG CACHE VNPAY 100%)
        // ==========================================
        private ActionResult ProcessVNPayPayment(DONHANG donHang)
        {
            try
            {
                // CODE CỨNG THÔNG SỐ (KHÔNG DÙNG WEB.CONFIG ĐỂ TRÁNH LỖI)
                string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                string vnp_TmnCode = "91DBIKLG";
                string vnp_HashSecret = "NIMV079MLJPTLAZR0KGF70XX3JYMJJF3";
                string vnp_Returnurl = "https://localhost:44341/Home/PaymentCallback";

                VnPayLibrary vnpay = new VnPayLibrary();

                // DÙNG TICKS ĐỂ MÃ GIAO DỊCH LÀ DUY NHẤT (Chống Lỗi 70 do trùng mã)
                string txnRef = donHang.MaDonHang.ToString() + "T" + DateTime.Now.Ticks.ToString();
                long amount = (long)(donHang.TongTien * 100);

                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", amount.ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");

                // TUYỆT ĐỐI KHÔNG DẤU CÁCH
                vnpay.AddRequestData("vnp_OrderInfo", "ThanhToanDonHang" + donHang.MaDonHang.ToString());
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", txnRef);

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                donHang.PhuongThucThanhToan = "VNPay - Đang chờ";
                donHang.TinhTrangThanhToan = "Chờ thanh toán";
                db.SaveChanges();

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối VNPay: " + ex.Message;
                return RedirectToAction("ThanhToan", new { id = donHang.MaDonHang });
            }
        }

        // ==========================================
        // 2. NHẬN KẾT QUẢ TỪ VNPAY TRẢ VỀ
        // ==========================================
        public ActionResult PaymentCallback()
        {
            try
            {
                if (Request.QueryString.Count > 0)
                {
                    string vnp_HashSecret = "NIMV079MLJPTLAZR0KGF70XX3JYMJJF3";
                    var vnpayData = Request.QueryString;
                    VnPayLibrary vnpay = new VnPayLibrary();

                    foreach (string s in vnpayData)
                    {
                        if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                        {
                            vnpay.AddResponseData(s, vnpayData[s]);
                        }
                    }

                    string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                    string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                    string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                    // Cắt bỏ đuôi chữ T để lấy ID thực tế
                    int orderId = 0;
                    if (vnp_TxnRef.Contains("T"))
                    {
                        orderId = int.Parse(vnp_TxnRef.Split('T')[0]);
                    }
                    else
                    {
                        orderId = int.Parse(vnp_TxnRef);
                    }

                    bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                    if (!checkSignature)
                    {
                        ViewBag.Message = "Chữ ký số không hợp lệ!";
                        return View("ThanhToanThatBai");
                    }

                    var donHang = db.DONHANG.FirstOrDefault(d => d.MaDonHang == orderId);
                    if (donHang != null)
                    {
                        if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                        {
                            donHang.PhuongThucThanhToan = "VNPay";
                            donHang.TinhTrangThanhToan = "Đã thanh toán";
                            donHang.TinhTrangGiaoHang = "Chờ xử lý";
                            db.SaveChanges();

                            TempData["Success"] = "Giao dịch VNPay thành công!";
                            return RedirectToAction("DatHangThanhCong", new { id = donHang.MaDonHang });
                        }
                        else
                        {
                            donHang.PhuongThucThanhToan = null;
                            donHang.TinhTrangThanhToan = "Thanh toán thất bại";
                            donHang.TinhTrangGiaoHang = "Chờ xử lý";
                            db.SaveChanges();

                            ViewBag.Message = "Giao dịch bị hủy hoặc xảy ra lỗi.";
                            ViewBag.OrderId = orderId;
                            return View("ThanhToanThatBai");
                        }
                    }
                }
                return RedirectToAction("SanPham", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Đã xảy ra lỗi: " + ex.Message;
                return View("ThanhToanThatBai");
            }
        }

       

        private string GetVNPayResponseMessage(string responseCode)
        {
            switch (responseCode)
            {
                case "07": return "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).";
                case "09": return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.";
                case "10": return "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần";
                case "11": return "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.";
                case "12": return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.";
                case "13": return "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).";
                case "24": return "Giao dịch không thành công do: Khách hàng hủy giao dịch";
                case "51": return "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.";
                case "65": return "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.";
                case "75": return "Ngân hàng thanh toán đang bảo trì.";
                case "79": return "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.";
                default: return "Giao dịch thất bại. Vui lòng thử lại sau!";
            }
        }

        // ===================== XỬ LÝ VOUCHER =====================

      
        // ===================== VOUCHER ACTIONS - THÊM VÀO HomeController.cs =====================

        // Action lấy danh sách voucher
        [HttpGet]
        public ActionResult LayDanhSachVoucher(string type = "discount")
        {
            try
            {
                var today = DateTime.Now;

                // Lấy voucher còn hiệu lực
                var vouchers = db.VOUCHER
                    .Where(v => v.TrangThai == true &&
                           v.NgayBatDau <= today &&
                           v.NgayKetThuc >= today &&
                           v.SoLuong > 0)
                    .Select(v => new
                    {
                        code = v.CodeVoucher,
                        title = v.CodeVoucher + " - Giảm " + v.GiaTriGiam + (v.KieuGiam == "Phần trăm" ? "%" : "đ"),
                        desc = "Đơn hàng từ " + v.DieuKienToiThieu + "đ",
                        expire = "HSD: " + v.NgayKetThuc.Value.ToString("dd/MM/yyyy"),
                        icon = v.KieuGiam == "Phần trăm" ? "%" : "₫",
                        giaTriGiam = v.GiaTriGiam,
                        kieuGiam = v.KieuGiam,
                        dieuKienToiThieu = v.DieuKienToiThieu
                    })
                    .ToList();

                return Json(new { success = true, data = vouchers }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading vouchers: {ex.Message}");
                return Json(new { success = false, message = "Lỗi tải voucher: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ⭐ CẢI TIẾN action áp dụng voucher với log chi tiết
        [HttpPost]
        public ActionResult ApDungVoucher(string codeVoucher)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ÁP DỤNG VOUCHER ===");
                System.Diagnostics.Debug.WriteLine($"Code nhận được: {codeVoucher}");

                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var gioHang = Session["GioHang"] as List<GioHang>;
                if (gioHang == null || !gioHang.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống!" });
                }

                // Tính tổng tiền
                decimal tongTien = gioHang.Sum(g => g.iSoLuong * (decimal)g.dDonGia);
                System.Diagnostics.Debug.WriteLine($"Tổng tiền giỏ hàng: {tongTien:N0}đ");

                // Tìm voucher
                var voucher = db.VOUCHER.FirstOrDefault(v => v.CodeVoucher == codeVoucher);

                if (voucher == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Không tìm thấy voucher trong database");
                    return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
                }

                System.Diagnostics.Debug.WriteLine($"✅ Tìm thấy voucher: {voucher.CodeVoucher}");
                System.Diagnostics.Debug.WriteLine($"   - Trạng thái: {voucher.TrangThai}");
                System.Diagnostics.Debug.WriteLine($"   - Số lượng: {voucher.SoLuong}");
                System.Diagnostics.Debug.WriteLine($"   - Ngày bắt đầu: {voucher.NgayBatDau}");
                System.Diagnostics.Debug.WriteLine($"   - Ngày kết thúc: {voucher.NgayKetThuc}");
                System.Diagnostics.Debug.WriteLine($"   - Điều kiện tối thiểu: {voucher.DieuKienToiThieu}");

                // Kiểm tra trạng thái
                if (voucher.TrangThai == false)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã bị vô hiệu hóa!" });
                }

                // Kiểm tra số lượng
                if (voucher.SoLuong <= 0)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng!" });
                }

                // Kiểm tra thời hạn
                var today = DateTime.Now;
                if (voucher.NgayBatDau > today)
                {
                    return Json(new { success = false, message = $"Mã giảm giá chưa có hiệu lực. Bắt đầu từ {voucher.NgayBatDau:dd/MM/yyyy}" });
                }

                if (voucher.NgayKetThuc < today)
                {
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn!" });
                }

                // Kiểm tra điều kiện tối thiểu
                if (tongTien < voucher.DieuKienToiThieu)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Đơn hàng phải đạt tối thiểu {voucher.DieuKienToiThieu:N0}đ để áp dụng mã này! (Hiện tại: {tongTien:N0}đ)"
                    });
                }

                // Tính số tiền giảm
                decimal soTienGiam = 0;
                if (voucher.KieuGiam == "Phần trăm")
                {
                    soTienGiam = tongTien * (voucher.GiaTriGiam ?? 0) / 100;
                }
                else // Tiền mặt
                {
                    soTienGiam = voucher.GiaTriGiam ?? 0;
                }

                // Không cho giảm quá tổng tiền
                if (soTienGiam > tongTien)
                {
                    soTienGiam = tongTien;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Tính toán thành công:");
                System.Diagnostics.Debug.WriteLine($"   - Kiểu giảm: {voucher.KieuGiam}");
                System.Diagnostics.Debug.WriteLine($"   - Giá trị giảm: {voucher.GiaTriGiam}");
                System.Diagnostics.Debug.WriteLine($"   - Số tiền giảm: {soTienGiam:N0}đ");
                System.Diagnostics.Debug.WriteLine($"   - Tổng sau giảm: {(tongTien - soTienGiam):N0}đ");

                // Lưu vào session
                Session["VoucherApDung"] = voucher;
                Session["SoTienGiam"] = soTienGiam;

                System.Diagnostics.Debug.WriteLine("✅ Đã lưu vào session");
                System.Diagnostics.Debug.WriteLine("======================");

                return Json(new
                {
                    success = true,
                    message = "Áp dụng mã giảm giá thành công!",
                    soTienGiam = soTienGiam,
                    tongTienSauGiam = tongTien - soTienGiam
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LỖI: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult XoaVoucher()
        {
            try
            {
                Session["VoucherApDung"] = null;
                Session["SoTienGiam"] = null;

                System.Diagnostics.Debug.WriteLine("✅ Đã xóa voucher khỏi session");

                return Json(new { success = true, message = "Đã xóa mã giảm giá!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        // ===== THÊM VÀO HomeController.cs =====

        // Action hiển thị trang thành viên
        public ActionResult ThanhVien()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            int userID = (int)Session["UserID"];

            // Lấy thông tin thành viên
            var thanhVien = db.THANHVIEN.FirstOrDefault(tv => tv.UserID == userID);

            if (thanhVien == null)
            {
                // Tạo thành viên mới với hạng Đồng
                thanhVien = new THANHVIEN
                {
                    UserID = userID,
                    HangThanhVien = "Đồng",
                    NgayCapNhat = DateTime.Now
                };
                db.THANHVIEN.Add(thanhVien);
                db.SaveChanges();
            }

            // Tính tổng tiền đã mua
            var tongTienDaMua = db.DONHANG
                .Where(d => d.UserID == userID &&
                       d.TinhTrangGiaoHang == "Đã giao hàng thành công")
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            // Đếm số đơn hàng
            var soDonHang = db.DONHANG
                .Count(d => d.UserID == userID &&
                      d.TinhTrangGiaoHang == "Đã giao hàng thành công");

            // Tự động nâng hạng
            string hangMoi = "Đồng";
            if (tongTienDaMua >= 10000000) // 10 triệu
            {
                hangMoi = "Vàng";
            }
            else if (tongTienDaMua >= 5000000) // 5 triệu
            {
                hangMoi = "Bạc";
            }

            if (thanhVien.HangThanhVien != hangMoi)
            {
                thanhVien.HangThanhVien = hangMoi;
                thanhVien.NgayCapNhat = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = $"Chúc mừng! Bạn đã được nâng hạng lên {hangMoi}!";
            }

            // Tính tiến độ đến hạng tiếp theo
            decimal tienCanThiet = 0;
            string hangTiepTheo = "";

            if (hangMoi == "Đồng")
            {
                tienCanThiet = 5000000 - tongTienDaMua;
                hangTiepTheo = "Bạc";
            }
            else if (hangMoi == "Bạc")
            {
                tienCanThiet = 10000000 - tongTienDaMua;
                hangTiepTheo = "Vàng";
            }

            ViewBag.ThanhVien = thanhVien;
            ViewBag.TongTienDaMua = tongTienDaMua;
            ViewBag.SoDonHang = soDonHang;
            ViewBag.TienCanThiet = tienCanThiet;
            ViewBag.HangTiepTheo = hangTiepTheo;
            ViewBag.TienDoNangHang = tienCanThiet > 0 ?
                (int)((tongTienDaMua / (tongTienDaMua + tienCanThiet)) * 100) : 100;

            return View();
        }

        // Action xem quyền lợi theo hạng
        public ActionResult QuyenLoiThanhVien(string hang)
        {
            var quyenLoi = new Dictionary<string, object>();

            switch (hang)
            {
                case "Đồng":
                    quyenLoi = new Dictionary<string, object>
            {
                { "giamGia", "3%" },
                { "diemTichLuy", "1 điểm / 10,000đ" },
                { "voucher", "Voucher 10k mỗi tháng" },
                { "hoTro", "Hỗ trợ email" }
            };
                    break;
                case "Bạc":
                    quyenLoi = new Dictionary<string, object>
            {
                { "giamGia", "5%" },
                { "diemTichLuy", "1.5 điểm / 10,000đ" },
                { "voucher", "Voucher 30k mỗi tháng" },
                { "hoTro", "Hỗ trợ hotline ưu tiên" },
                { "giaoHang", "Miễn phí ship đơn > 200k" }
            };
                    break;
                case "Vàng":
                    quyenLoi = new Dictionary<string, object>
            {
                { "giamGia", "10%" },
                { "diemTichLuy", "2 điểm / 10,000đ" },
                { "voucher", "Voucher 100k mỗi tháng" },
                { "hoTro", "Hỗ trợ 24/7 dedicated" },
                { "giaoHang", "Miễn phí ship toàn quốc" },
                { "suKien", "Ưu tiên tham gia sự kiện độc quyền" }
            };
                    break;
            }

            return Json(quyenLoi, JsonRequestBehavior.AllowGet);
        }
    }
}
    