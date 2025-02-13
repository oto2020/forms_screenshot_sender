using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Для обработки JSON

namespace forms_screenshot_sender
{
    internal class MyTelegram
    {
        // Закрытый статический токен
        private static readonly string _token = "7868384563:AAFiQQPVCXp79lzHzJZZG5B3BuKJc84mE28";
        private static readonly string _apiUrl = $"https://api.telegram.org/bot{_token}";


        // ✅ Метод отправки фото в Telegram с инлайновыми кнопками (в одной строке) и возврата file_id
        private static async Task<string> SendImageToTelegram(string caption, Bitmap image, string chatId, long vptRequestId)
        {
            Bitmap upscaledImage = UpscaleImage(image);

            using (var client = new HttpClient())
            {
                using (var memoryStream = new MemoryStream())
                {
                    upscaledImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    memoryStream.Position = 0;

                    // ✅ Загружаем фото в Telegram и получаем file_id
                    var form = new MultipartFormDataContent
            {
                { new StreamContent(memoryStream), "photo", "screenshot.png" },
                { new StringContent(chatId), "chat_id" },
                { new StringContent(caption, Encoding.UTF8), "caption" }
            };

                    var response = await client.PostAsync($"{_apiUrl}/sendPhoto", form);
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Ошибка отправки фото: {jsonResponse}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    // ✅ Парсим JSON-ответ и получаем `file_id`
                    JObject json = JObject.Parse(jsonResponse);
                    string fileId = json["result"]?["photo"]?.Last?["file_id"]?.ToString();

                    if (string.IsNullOrEmpty(fileId))
                    {
                        MessageBox.Show("Ошибка получения file_id!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    // ✅ Отправляем сообщение с инлайн-кнопками в ОДНОЙ СТРОКЕ
                    var keyboard = new
                    {
                        chat_id = chatId,
                        photo = fileId,
                        caption = caption,
                        reply_markup = new
                        {
                            inline_keyboard = new[]
                            {
                        new[]
                        {
                            new { text = "✅ Беру", callback_data = $"vpt_status@accepted@{vptRequestId}" },
                            new { text = "❌ Не беру", callback_data = $"vpt_status@rejected@{vptRequestId}" }
                        }
                    }
                        }
                    };

                    string keyboardJson = JsonConvert.SerializeObject(keyboard);
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/sendPhoto")
                    {
                        Content = new StringContent(keyboardJson, Encoding.UTF8, "application/json")
                    };

                    var keyboardResponse = await client.SendAsync(request);
                    string keyboardJsonResponse = await keyboardResponse.Content.ReadAsStringAsync();

                    if (!keyboardResponse.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Ошибка отправки клавиатуры: {keyboardJsonResponse}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    return fileId; // 🔥 Возвращаем `file_id`
                }
            }
        }

        // ✅ Метод для увеличения изображения
        private static Bitmap UpscaleImage(Bitmap original)
        {
            int upscaleFactor = 2; // Увеличиваем в 2 раза
            int newWidth = original.Width * upscaleFactor;
            int newHeight = original.Height * upscaleFactor;

            Bitmap upscaled = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return upscaled;
        }

        // ✅ Публичный метод для отправки фото и возврата `file_id`
        public static async Task<string> SendScreenshotAsync(string caption, Bitmap image, string chatId, long vptRequestId)
        {
            return await SendImageToTelegram(caption, image, chatId, vptRequestId);
        }

        // ✅ Синхронный метод для получения `file_id`
        public static string SendScreenshotSync(string caption, Bitmap image, string chatId, long vptRequestId)
        {
            return Task.Run(() => SendScreenshotAsync(caption, image, chatId, vptRequestId)).Result;
        }
    }
}
