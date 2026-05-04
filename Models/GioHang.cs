using System;
using System.Collections.Generic;

namespace Nhom06_QuanLyBanSah.Models
{
    public class GioHang
    {
        public int iMaSach { get; set; }
        public string sTenSach { get; set; }
        public string sAnhBia { get; set; }
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }

        public double ThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }

        // Constructor rỗng (Bắt buộc phải có để hệ thống lưu Session an toàn)
        public GioHang() { }

        // Constructor nhận dữ liệu truyền vào từ Controller
        public GioHang(int maSach, string tenSach, string anhBia, double donGia, int soLuong = 1)
        {
            iMaSach = maSach;
            sTenSach = tenSach;
            sAnhBia = anhBia;
            dDonGia = donGia;
            iSoLuong = soLuong;
        }
    }
}