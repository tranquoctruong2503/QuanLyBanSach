using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nhom06_QuanLyBanSah.Models;

namespace Nhom06_QuanLyBanSah.Models
{
    public class GioHang
    {
        QUANLYBANSACH_NHOM06Entities db = new QUANLYBANSACH_NHOM06Entities();

        public int iMaSach { get; set; }
        public string sTenSach { get; set; }
        public string sAnhBia { get; set; }
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }

        public double ThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }

        public GioHang(int MaSach)
        {
            iMaSach = MaSach;
            var s = db.SACH.SingleOrDefault(x => x.MaSach == MaSach);
            if (s != null)
            {
                sTenSach = s.TenSach;
                sAnhBia = s.AnhBia;
                dDonGia = s.GiaBan != null ? Convert.ToDouble(s.GiaBan) : 0;
                iSoLuong = 1;
            }
        }
    }

}
