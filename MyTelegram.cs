using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace forms_screenshot_sender
{
    internal class MyTelegram
    {
        // Закрытый статический токен
        private static string _token = "7868384563:AAFiQQPVCXp79lzHzJZZG5B3BuKJc84mE28";

        // Оставляем ваши закомментированные методы (теперь статические и private)
        // При желании можно раскомментировать и доработать логику upscaleFactor
        // из внешнего источника (config). Здесь для примера задан фиксированный 2.
        private static Bitmap UpscaleImage(Bitmap original)
        {
            // Read the upscale factor from config
            // int upscaleFactor = int.Parse(config["UpscaleFactor"]);
            int upscaleFactor = 2; // Задаём произвольное значение, если нет config

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

        // Аналогично, делаем метод SendImageToTelegram статическим и private
        // Если нужно вызывать его снаружи, замените на public static async Task
        private static async void SendImageToTelegram(string caption, Bitmap image, string chatId)
        {
            // Сначала увеличиваем картинку
            Bitmap upscaledImage = UpscaleImage(image);

            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        upscaledImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Position = 0;

                        form.Add(new StreamContent(memoryStream), "photo", "screenshot.png");
                        form.Add(new StringContent(caption), "caption");

                        // Подставляем токен из приватного поля
                        var response = await client.PostAsync(
                            $"https://api.telegram.org/bot{_token}/sendPhoto?chat_id={chatId}",
                            form);

                        response.EnsureSuccessStatusCode();
                    }
                }
            }
        }

        // Если хотите вызывать SendImageToTelegram снаружи (из других частей кода),
        // сделайте его-обёртку public:
        public static async Task SendScreenshotAsync(string caption, Bitmap image, string chatId)
        {
            // Здесь вызываем приватный метод
            SendImageToTelegram(caption, image, chatId);
        }
    }
}
