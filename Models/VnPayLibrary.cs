using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Nhom06_QuanLyBanSah.Models
{
    public class VnPayLibrary
    {
        private SortedList<string, string> _requestData =
            new SortedList<string, string>(new VnPayCompare());

        private SortedList<string, string> _responseData =
            new SortedList<string, string>(new VnPayCompare());

        // ================== REQUEST ==================
        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        // ================== RESPONSE ==================
        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.ContainsKey(key) ? _responseData[key] : string.Empty;
        }

        // ================== CREATE PAYMENT URL ==================
        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();

            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    if (data.Length > 0)
                    {
                        data.Append("&");
                    }
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));
                }
            }

            string queryString = data.ToString();
            string signData = queryString;

            // Tạo secure hash
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, signData);

            // URL cuối cùng
            string paymentUrl = baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return paymentUrl;
        }

        // ================== VALIDATE RESPONSE SIGNATURE ==================
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            StringBuilder data = new StringBuilder();

            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value) &&
                    kv.Key != "vnp_SecureHash" &&
                    kv.Key != "vnp_SecureHashType")
                {
                    if (data.Length > 0)
                    {
                        data.Append("&");
                    }
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value));
                }
            }

            string myChecksum = Utils.HmacSHA512(secretKey, data.ToString());

            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    // ================== SORT KEY A → Z ==================
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    // ================== UTILS ==================
    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (byte b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        public static string GetIpAddress()
        {
            try
            {
                string ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ip))
                {
                    string[] ipRange = ip.Split(',');
                    ip = ipRange[0];
                }
                else
                {
                    ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }

                if (string.IsNullOrEmpty(ip) || ip == "::1")
                {
                    ip = "127.0.0.1";
                }

                return ip;
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}