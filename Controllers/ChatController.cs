using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// LƯU Ý: Bạn nhớ đổi "Nhom06_QuanLyBanSach" thành đúng tên Project của bạn (tên thư mục gốc)
namespace Nhom06_QuanLyBanSach.Controllers
{
    public class ChatController : Controller
    {
        // Khai báo API Key của Gemini
        private readonly string _apiKey = "AIzaSyBjWNpWYre0Yd8xn4rcmmMq3pW8_mmxjgM";
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            // 1. Kiểm tra nếu người dùng không nhập gì
            if (string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, error = "Tin nhắn không được để trống" });
            }

            try
            {
                // 2. Cấu hình System Prompt (định hướng cho AI)
                var systemPrompt = "Bạn là trợ lý AI thân thiện của ALPHA BOOK - cửa hàng sách trực tuyến hàng đầu Việt Nam. Nhiệm vụ: Tư vấn, giới thiệu sách, và trả lời câu hỏi về dịch vụ. Quy tắc: Luôn trả lời bằng tiếng Việt. Ngắn gọn (2-4 câu). Nếu không biết, gợi ý liên hệ hotline: (028) 123 4567.";
                var fullPrompt = $"{systemPrompt}\n\nCâu hỏi của khách hàng: {message}";

                // 3. Đóng gói dữ liệu để gửi cho Google
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = fullPrompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024
                    }
                };

                // 4. Mở kết nối mạng và gửi đi
                using (var client = new HttpClient())
                {
                    // Biến dữ liệu thành chuỗi JSON
                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                    // Gửi Post Request
                    var response = await client.PostAsync($"{_apiUrl}?key={_apiKey}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // 5. Nếu Google trả lời thành công, bóc tách lấy câu chữ
                        var responseString = await response.Content.ReadAsStringAsync();
                        var responseData = JObject.Parse(responseString);

                        var replyText = responseData["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                        // Trả về cho file giao diện (HTML/JS)
                        return Json(new { success = true, reply = replyText });
                    }
                    else
                    {
                        // 6. Nếu API Key sai hoặc hết hạn
                        var errorText = await response.Content.ReadAsStringAsync();
                        return Json(new { success = false, error = $"Lỗi từ Google API: {response.StatusCode}" });
                    }
                }
            }
            catch (Exception ex)
            {
                // 7. Nếu mạng bị lỗi hoặc code bị lỗi
                return Json(new { success = false, error = "Lỗi máy chủ: " + ex.Message });
            }
        }
    }
}