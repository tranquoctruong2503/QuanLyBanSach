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
        QUANLYBANSACH_NHOM06Entities db = new QUANLYBANSACH_NHOM06Entities();

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

        public ActionResult ThemGioHang(int ms, string strURL)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang SanPham = lstGioHang.Find(sp => sp.iMaSach == ms);

            if (SanPham == null)
            {
                SanPham = new GioHang(ms);
                lstGioHang.Add(SanPham);
            }
            else
            {
                SanPham.iSoLuong++;
            }

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("GioHang", "Home");
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

        public ActionResult CapNhatGioHang(int MaSP, FormCollection f)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sp = lstGioHang.Single(s => s.iMaSach == MaSP);

            if (sp != null)
            {
                sp.iSoLuong = int.Parse(f["txtSoLuong"].ToString());
            }

            return RedirectToAction("GioHang", "Home");
        }

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
            decimal tongTien = gioHang.Sum(g => g.iSoLuong * (decimal)g.dDonGia);

            // ⭐ Kiểm tra voucher
            decimal soTienGiam = 0;
            VOUCHER voucherApDung = Session["VoucherApDung"] as VOUCHER;
            if (voucherApDung != null && Session["SoTienGiam"] != null)
            {
                soTienGiam = (decimal)Session["SoTienGiam"];
                tongTien -= soTienGiam; // Trừ tiền giảm giá
            }

            var donHang = new DONHANG
            {
                UserID = userID,
                NgayDatHang = DateTime.Now,
                DiaChiGiaoHang = diaChiGiaoHang,
                TongTien = tongTien,
                PhuongThucThanhToan = null,
                TinhTrangGiaoHang = "Chờ xử lý"
            };

            db.DONHANG.Add(donHang);
            db.SaveChanges();

            // ⭐ Lưu thông tin voucher nếu có
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
                    if (voucher.SoLuong <= 0)
                    {
                        voucher.TrangThai = false;
                    }
                }
            }

            // Lưu chi tiết đơn hàng
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
                if (donHang.PhuongThucThanhToan != null)
                {
                    TempData["Info"] = "Đơn hàng đã được thanh toán!";
                    return RedirectToAction("ChiTietDonHang", new { ma = maDonHang });
                }

                // Thanh toán COD
                if (phuongThucThanhToan == "Thanh toán khi nhận hàng (COD)")
                {
                    donHang.PhuongThucThanhToan = "COD";
                    donHang.TinhTrangGiaoHang = "Chờ xử lý";
                    db.SaveChanges();

                    TempData["Success"] = "Đặt hàng thành công! Bạn sẽ thanh toán khi nhận hàng.";
                    return RedirectToAction("DatHangThanhCong", new { id = maDonHang });
                }

                // Thanh toán VNPay
                if (phuongThucThanhToan == "VNPay")
                {
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

        private ActionResult ProcessVNPayPayment(DONHANG donHang)
        {
            try
            {
                // Lấy cấu hình từ Web.config
                string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
                string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                string vnp_Returnurl = Url.Action("PaymentCallback", "Home", null, Request.Url.Scheme);

                // Kiểm tra cấu hình
                if (string.IsNullOrEmpty(vnp_Url) ||
                    string.IsNullOrEmpty(vnp_TmnCode) ||
                    string.IsNullOrEmpty(vnp_HashSecret))
                {
                    TempData["Error"] = "Cấu hình thanh toán VNPay chưa đầy đủ!";
                    return RedirectToAction("ThanhToan", new { id = donHang.MaDonHang });
                }

                // Tạo thư viện VNPay
                VnPayLibrary vnpay = new VnPayLibrary();

                // ⭐ FIX 1: Tạo mã giao dịch ngắn gọn hơn
                string txnRef = DateTime.Now.Ticks.ToString();

                // ⭐ FIX 2: Tính số tiền (VNPay yêu cầu nhân 100)
                long amount = (long)(donHang.TongTien * 100);

                // Lấy IP
                string ipAddr = Utils.GetIpAddress();

                // Tạo ngày giờ
                string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

                // ⭐ FIX 3: Thêm các tham số THEO THỨ TỰ ALPHABET CHÍNH XÁC
                vnpay.AddRequestData("vnp_Amount", amount.ToString());
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_CreateDate", createDate);
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", ipAddr);
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + donHang.MaDonHang);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_TxnRef", txnRef);
                vnpay.AddRequestData("vnp_Version", "2.1.0");

                // ⭐ FIX 4: THÊM THAM SỐ NÀY - RẤT QUAN TRỌNG!
                // Nếu không có vnp_BankCode, VNPay sẽ hiển thị trang chọn ngân hàng
                // Nhưng một số tài khoản sandbox yêu cầu phải có bankCode
                // vnpay.AddRequestData("vnp_BankCode", "VNPAYQR"); // Uncomment nếu cần

                // Tạo URL thanh toán
                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                // Log để debug
                System.Diagnostics.Debug.WriteLine("=== VNPay Payment URL ===");
                System.Diagnostics.Debug.WriteLine(paymentUrl);
                System.Diagnostics.Debug.WriteLine("TxnRef: " + txnRef);
                System.Diagnostics.Debug.WriteLine("Amount: " + amount);
                System.Diagnostics.Debug.WriteLine("========================");

                // ⭐ FIX 5: Lưu thông tin vào database TRƯỚC KHI chuyển hướng
                donHang.PhuongThucThanhToan = "VNPay - Đang chờ";
                donHang.TinhTrangGiaoHang = "Chờ thanh toán";
                db.SaveChanges();

                // Lưu session
                Session["PaymentTime_" + donHang.MaDonHang] = DateTime.Now;
                Session["TxnRef_" + donHang.MaDonHang] = txnRef;

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                TempData["Error"] = "Không thể kết nối đến VNPay: " + ex.Message;
                return RedirectToAction("ThanhToan", new { id = donHang.MaDonHang });
            }
        }

        // FIX: PaymentCallback
        public ActionResult PaymentCallback()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== PaymentCallback Start ===");

                if (Request.QueryString.Count > 0)
                {
                    string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                    var vnpayData = Request.QueryString;
                    VnPayLibrary vnpay = new VnPayLibrary();

                    // Log all query parameters
                    foreach (string s in vnpayData)
                    {
                        System.Diagnostics.Debug.WriteLine($"{s} = {vnpayData[s]}");

                        if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                        {
                            vnpay.AddResponseData(s, vnpayData[s]);
                        }
                    }

                    // Lấy thông tin từ response
                    string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                    string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                    string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                    string vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
                    string vnp_Amount = vnpay.GetResponseData("vnp_Amount");

                    // ⭐ FIX: Tìm đơn hàng bằng TxnRef trong session
                    int? orderId = null;
                    foreach (string key in Session.Keys)
                    {
                        if (key.StartsWith("TxnRef_") && Session[key].ToString() == vnp_TxnRef)
                        {
                            string orderIdStr = key.Replace("TxnRef_", "");
                            orderId = int.Parse(orderIdStr);
                            break;
                        }
                    }

                    if (!orderId.HasValue)
                    {
                        ViewBag.Message = "Không tìm thấy đơn hàng!";
                        return View("ThanhToanThatBai");
                    }

                    System.Diagnostics.Debug.WriteLine($"Found OrderId: {orderId}");

                    // Kiểm tra chữ ký
                    bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                    System.Diagnostics.Debug.WriteLine($"Signature Valid: {checkSignature}");

                    if (!checkSignature)
                    {
                        ViewBag.Message = "Chữ ký không hợp lệ!";
                        ViewBag.OrderId = orderId;
                        return View("ThanhToanThatBai");
                    }

                    // Tìm đơn hàng
                    var donHang = db.DONHANG.FirstOrDefault(d => d.MaDonHang == orderId);

                    if (donHang == null)
                    {
                        ViewBag.Message = "Không tìm thấy đơn hàng!";
                        ViewBag.OrderId = orderId;
                        return View("ThanhToanThatBai");
                    }

                    // Kiểm tra số tiền
                    decimal expectedAmount = donHang.TongTien * 100;
                    if (Convert.ToDecimal(vnp_Amount) != expectedAmount)
                    {
                        ViewBag.Message = $"Số tiền thanh toán không khớp!";
                        ViewBag.OrderId = orderId;
                        return View("ThanhToanThatBai");
                    }

                    // Kiểm tra kết quả thanh toán
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        // Thanh toán thành công
                        donHang.PhuongThucThanhToan = "VNPay";
                        donHang.TinhTrangGiaoHang = "Đã thanh toán - Chờ xử lý";
                        db.SaveChanges();

                        ViewBag.Message = "Thanh toán thành công!";
                        ViewBag.OrderId = orderId;
                        ViewBag.TransactionId = vnp_TransactionNo;
                        ViewBag.Amount = donHang.TongTien;

                        return View("ThanhToanThanhCong");
                    }
                    else
                    {
                        // Thanh toán thất bại
                        donHang.PhuongThucThanhToan = null;
                        donHang.TinhTrangGiaoHang = "Chờ xử lý";
                        db.SaveChanges();

                        ViewBag.Message = GetVNPayResponseMessage(vnp_ResponseCode);
                        ViewBag.OrderId = orderId;
                        ViewBag.ErrorCode = vnp_ResponseCode;

                        return View("ThanhToanThatBai");
                    }
                }

                return RedirectToAction("SanPham", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PaymentCallback: {ex.Message}");
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
    