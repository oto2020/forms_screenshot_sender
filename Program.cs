using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace ScreenshotToTrayApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private IKeyboardMouseEvents globalHook;

        public TrayApplicationContext()
        {
            // Инициализация иконки в трее
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Сделать скриншот", TakeScreenshot)
                }),
                Visible = true
            };

            // Действие по клику на иконке в трее
            trayIcon.MouseClick += TrayIcon_MouseClick;

            // Установка глобального хука для клавиатуры
            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_KeyDown;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // Проверяем, что клик был левой кнопкой мыши
            if (e.Button == MouseButtons.Left)
            {
                TakeScreenshot(sender, e);
            }
        }

        private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажаты ли клавиши Ctrl + Shift + S
            if (e.KeyCode == Keys.Home)
            {
                TakeScreenshot(sender, e);
            }
        }

        private async void TakeScreenshot(object sender, EventArgs e)
        {
            // Задаем область для скриншота
            int startX = 130;
            int startY = 225;
            int width = 1700;
            int height = 760;

            Rectangle bounds = new Rectangle(startX, startY, width, height);

            // Сделать скриншот указанной области
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // Копирование изображения в буфер обмена
                Clipboard.SetImage(bitmap);

                // Показать окно с превью
                ShowPreviewWindow(bitmap);
            }
        }
        private Bitmap UpscaleImage(Bitmap original)
        {
            // Увеличиваем размеры изображения в 2 раза (размножаем каждый пиксель на 4)
            int newWidth = original.Width * 2;
            int newHeight = original.Height * 2;

            Bitmap upscaled = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(upscaled))
            {
                // Установка интерполяции для улучшения качества
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return upscaled;
        }

        private async void SendImageToTelegram(string caption, Bitmap image)
        {
            // Увеличиваем изображение перед отправкой
            Bitmap upscaledImage = UpscaleImage(image);

            string chatId = "-4598240224"; // ID чата
            string token = "6389154487:AAEmOleHqPfoeLoAT7SEtVo4otc5wP6zUiI"; // Токен бота

            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    // Конвертация увеличенного изображения в массив байтов
                    using (var memoryStream = new MemoryStream())
                    {
                        upscaledImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Position = 0; // Сброс позиции потока

                        // Добавление изображения в запрос
                        form.Add(new StreamContent(memoryStream), "photo", "screenshot.png");
                        form.Add(new StringContent(caption), "caption");

                        // Отправка POST-запроса к API Telegram
                        var response = await client.PostAsync($"https://api.telegram.org/bot{token}/sendPhoto?chat_id={chatId}", form);
                        response.EnsureSuccessStatusCode(); // Проверка на успех
                    }
                }
            }
        }

        private void ShowPreviewWindow(Bitmap screenshot)
        {
            // Создание формы для отображения скриншота
            Form previewForm = new Form
            {
                Text = "Скриншот",
                Size = new Size(800, 510),
                StartPosition = FormStartPosition.CenterScreen
            };

            // Создание элемента PictureBox для отображения скриншота
            PictureBox pictureBox = new PictureBox
            {
                Image = screenshot,
                Dock = DockStyle.Top,
                Height = 400,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Создание панели для кнопок
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Height = 50, // Высота панели с кнопками
                Padding = new Padding(10)
            };

            // Кнопки
            Button groupProgramsButton = new Button { Text = "Групповые программы", Width = 180, Height = 50 };
            Button gymButton = new Button { Text = "Тренажерный зал", Width = 180, Height = 50 };
            Button aquaZoneButton = new Button { Text = "Аква-зона", Width = 180, Height = 50 };
            Button closeButton = new Button { Text = "Закрыть", Width = 200, Height = 50 };

            // Добавление обработчиков событий для кнопок
            groupProgramsButton.Click += (s, e) =>
            {
                SendImageToTelegram("Групповые программы", screenshot);
                groupProgramsButton.Enabled = false;
            };
            gymButton.Click += (s, e) =>
            {
                SendImageToTelegram("Тренажерный зал", screenshot);
                gymButton.Enabled = false;
            };
            aquaZoneButton.Click += (s, e) =>
            {
                SendImageToTelegram("Аква-зона", screenshot);
                aquaZoneButton.Enabled = false;
            };
            closeButton.Click += (s, e) => { previewForm.Close(); };

            // Добавление кнопок в панель
            buttonPanel.Controls.Add(aquaZoneButton);
            buttonPanel.Controls.Add(gymButton);
            buttonPanel.Controls.Add(groupProgramsButton);
            buttonPanel.Controls.Add(closeButton);

            // Добавление элементов на форму
            previewForm.Controls.Add(pictureBox);
            previewForm.Controls.Add(buttonPanel);

            // Отображение формы
            previewForm.ShowDialog();
        }

        private void Exit()
        {
            // Скрытие иконки в трее и завершение приложения
            trayIcon.Visible = false;

            // Удаление глобального хука
            globalHook.KeyDown -= GlobalHook_KeyDown;
            globalHook.Dispose();

            Application.Exit();
        }


    }
}
