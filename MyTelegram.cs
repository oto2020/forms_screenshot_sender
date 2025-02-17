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
            using (var memoryStream = new MemoryStream())
            {
                upscaledImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                // 1. Формируем инлайн-клавиатуру
                var inlineKeyboard = new
                {
                    inline_keyboard = new[]
                    {
                new[]
                {
                    new { text = "✅ Беру", callback_data = $"vpt_status@accepted@{vptRequestId}" },
                    new { text = "❌ Не беру", callback_data = $"vpt_status@rejected@{vptRequestId}" }
                }
            }
                };
                // 2. Сериализуем клавиатуру в JSON
                string inlineKeyboardJson = JsonConvert.SerializeObject(inlineKeyboard);

                // 3. Формируем form-data, сразу добавляя "reply_markup"
                var form = new MultipartFormDataContent
                {
                    { new StreamContent(memoryStream), "photo", "screenshot.png" },
                    { new StringContent(chatId), "chat_id" },
                    { new StringContent(caption, Encoding.UTF8), "caption" },
                    { new StringContent(inlineKeyboardJson, Encoding.UTF8), "reply_markup" }
                };

                // 4. Делаем один запрос sendPhoto
                var response = await client.PostAsync($"{_apiUrl}/sendPhoto", form);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Ошибка отправки фото: {jsonResponse}",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return null;
                }

                // 5. Парсим ответ, чтобы при необходимости достать file_id
                JObject json = JObject.Parse(jsonResponse);
                string fileId = json["result"]?["photo"]?.Last?["file_id"]?.ToString();

                if (string.IsNullOrEmpty(fileId))
                {
                    MessageBox.Show("Ошибка получения file_id!",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return null;
                }

                // Возвращаем file_id, если нужно использовать дальше
                return fileId;
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
