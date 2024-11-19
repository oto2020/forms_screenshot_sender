using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using Microsoft.Win32;

namespace ScreenshotToTrayApp
{
    static class Program
    {
        private static Mutex mutex = new Mutex(true, "{5A8F5B39-FA5D-4B91-AFCF-6F5FDF16F973}");
        private static Dictionary<string, string> config;

        [STAThread]
        static void Main()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Программа уже запущена", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load configuration from the file
            config = LoadConfiguration("config.txt");
            if (config == null)
            {
                MessageBox.Show("Ошибка при загрузке конфигурации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AddToStartup();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext(config));

            mutex.ReleaseMutex();
        }

        public static void AddToStartup()
        {
            // Имя приложения для автозапуска
            string appName = "YourAppName"; // Замените на имя вашего приложения
            string appPath = Application.ExecutablePath;

            // Путь в реестре для автозапуска
            string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath, true))
            {
                if (key == null)
                {
                    MessageBox.Show("Ошибка: невозможно получить доступ к реестру.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверка, добавлена ли программа в автозапуск
                object value = key.GetValue(appName);
                if (value == null)
                {
                    // Добавление программы в автозапуск
                    key.SetValue(appName, appPath);
                    MessageBox.Show("Программа добавлена в автозапуск.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Программа уже находится в автозапуске.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


        private static Dictionary<string, string> LoadConfiguration(string filePath)
        {
            try
            {
                var config = new Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (line.Contains("="))
                    {
                        var parts = line.Split(new[] { '=' }, 2);
                        config[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                // Ensure the UpscaleFactor exists and is a valid integer
                if (!config.ContainsKey("UpscaleFactor") || !int.TryParse(config["UpscaleFactor"], out _))
                {
                    throw new Exception("Invalid or missing UpscaleFactor in configuration.");
                }

                return config;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении файла конфигурации: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

       
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private IKeyboardMouseEvents globalHook;
        private Dictionary<string, string> config;

        public TrayApplicationContext(Dictionary<string, string> config)
        {
            this.config = config;
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Выход", Exit)
                }),
                Visible = true
            };

            MessageBox.Show("Чтобы отправить ВПТ, нажмите кнопку Home или F11. Координаты скриншота задаются в config.txt", "Добро пожаловать", MessageBoxButtons.OK, MessageBoxIcon.Information);

            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_KeyDown;
        }



        private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Home)
            {
                int startX = int.Parse(config["HomeStartX"]);
                int startY = int.Parse(config["HomeStartY"]);
                int width = int.Parse(config["HomeWidth"]);
                int height = int.Parse(config["HomeHeight"]);

                TakeScreenshot(startX, startY, width, height);
            }
            else if (e.KeyCode == Keys.F11)
            {
                int startX = int.Parse(config["F11StartX"]);
                int startY = int.Parse(config["F11StartY"]);
                int width = int.Parse(config["F11Width"]);
                int height = int.Parse(config["F11Height"]);

                TakeScreenshot(startX, startY, width, height);
            }
        }

        private async void TakeScreenshot(int startX, int startY, int width, int height)
        {
            Rectangle bounds = new Rectangle(startX, startY, width, height);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // Draw rounded border
                DrawRoundedBorder(bitmap, config["BorderColor"], int.Parse(config["BorderThickness"]));

                // Clipboard.SetImage(bitmap);
                ShowPreviewWindow(bitmap);
            }
        }

        private void DrawRoundedBorder(Bitmap bitmap, string colorName, int thickness)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Color color = Color.FromName(colorName);
                using (Pen pen = new Pen(color, thickness))
                {
                    int radius = 50; // Adjust the radius of the corners
                                     // Create the path for rounded rectangle
                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(0, 0, radius, radius, 180, 90);
                    path.AddArc(bitmap.Width - radius, 0, radius, radius, 270, 90);
                    path.AddArc(bitmap.Width - radius, bitmap.Height - radius, radius, radius, 0, 90);
                    path.AddArc(0, bitmap.Height - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    // Draw the border
                    g.DrawPath(pen, path);
                }
            }
        }

        private Bitmap UpscaleImage(Bitmap original)
        {
            // Read the upscale factor from config
            int upscaleFactor = int.Parse(config["UpscaleFactor"]);

            int newWidth = original.Width * upscaleFactor;
            int newHeight = original.Height * upscaleFactor;

            Bitmap upscaled = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return upscaled;
        }
        public string GetSelectedRadioButtonText(Panel panel)
        {
            // Перебираем все элементы управления на панели
            foreach (RadioButton radioButton in panel.Controls.OfType<RadioButton>())
            {
                // Если радиокнопка выбрана, возвращаем ее текст
                if (radioButton.Checked)
                {
                    return radioButton.Text;
                }
            }

            // Если ни одна кнопка не выбрана, возвращаем пустую строку или сообщение по умолчанию
            return string.Empty;
        }

        private void ShowPreviewWindow(Bitmap screenshot)
        {
            Bitmap borderedScreenshot = screenshot;

            Form previewForm = new Form
            {
                Text = "Скриншот",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterScreen
            };

            PictureBox pictureBox = new PictureBox
            {
                Image = screenshot,
                Dock = DockStyle.Top,
                Height = 400,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Panel for input fields
            FlowLayoutPanel inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(10)
            };

            Label commentLabel = new Label { Text = "Комментарий:", AutoSize = true, Margin = new Padding(0, 8, 5, 0) };
            TextBox commentBox = new TextBox { Width = 200, Height = 30 };

            Label phoneLabel = new Label { Text = "Телефон:", AutoSize = true, Margin = new Padding(10, 8, 5, 0) };
            TextBox phoneBox = new TextBox { Width = 200, Height = 30 };

            inputPanel.Controls.Add(commentLabel);
            inputPanel.Controls.Add(commentBox);
            inputPanel.Controls.Add(phoneLabel);
            inputPanel.Controls.Add(phoneBox);

            // Time selection panel with radio buttons
            FlowLayoutPanel timePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(10)
            };

            Label timeLabel = new Label { Text = "Когда клиент может посетить ВПТ:", AutoSize = true, Margin = new Padding(0, 8, 5, 0) };
            timePanel.Controls.Add(timeLabel);

            RadioButton wholeDayRadio = new RadioButton { Text = "Весь день", AutoSize = true };
            RadioButton morningRadio = new RadioButton { Text = "Утро", AutoSize = true };
            RadioButton lunchRadio = new RadioButton { Text = "Обед", AutoSize = true };
            RadioButton eveningRadio = new RadioButton { Text = "Вечер", AutoSize = true };

            timePanel.Controls.Add(wholeDayRadio);
            timePanel.Controls.Add(morningRadio);
            timePanel.Controls.Add(lunchRadio);
            timePanel.Controls.Add(eveningRadio);

            // Panel for action buttons
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(10)
            };

            Button groupProgramsButton = new Button { Text = "Групповые программы", Width = 170, Height = 40 };
            Button gymButton = new Button { Text = "Тренажерный зал", Width = 170, Height = 40 };
            Button aquaZoneButton = new Button { Text = "Аква-зона", Width = 170, Height = 40 };
            Button closeButton = new Button { Text = "Закрыть", Width = 120, Height = 40 };

            groupProgramsButton.Click += (s, e) =>
            {
                if (ValidateInputs(commentBox, phoneBox, new RadioButton[] { wholeDayRadio, morningRadio, lunchRadio, eveningRadio }))
                {
                    SendImageToTelegram(config["GroupProgramsTelegram"] + " ВПТ ГП\nКомментарий: " + commentBox.Text + "\n📞 " + phoneBox.Text.Replace(" ", "") + "\nВремя: " + GetSelectedRadioButtonText(timePanel) + "\nОтправитель: " + config["Sender"], borderedScreenshot);
                    groupProgramsButton.Enabled = false;
                }
            };

            gymButton.Click += (s, e) =>
            {
                if (ValidateInputs(commentBox, phoneBox, new RadioButton[] { wholeDayRadio, morningRadio, lunchRadio, eveningRadio }))
                {
                    SendImageToTelegram(config["GymTelegram"] + " ВПТ ТЗ\nКомментарий: " + commentBox.Text + "\n📞 " + phoneBox.Text.Replace(" ", "") + "\nВремя: " + GetSelectedRadioButtonText(timePanel) + "\nОтправитель: " + config["Sender"], borderedScreenshot);
                    gymButton.Enabled = false;
                }
            };

            aquaZoneButton.Click += (s, e) =>
            {
                if (ValidateInputs(commentBox, phoneBox, new RadioButton[] { wholeDayRadio, morningRadio, lunchRadio, eveningRadio }))
                {
                    SendImageToTelegram(config["AquaZoneTelegram"] + " ВПТ Аква\nКомментарий: " + commentBox.Text + "\n📞 " + phoneBox.Text.Replace(" ", "") + "\nВремя: " + GetSelectedRadioButtonText(timePanel) + "\nОтправитель: " + config["Sender"], borderedScreenshot);
                    aquaZoneButton.Enabled = false;
                }
            };

            closeButton.Click += (s, e) => { previewForm.Close(); };

            buttonPanel.Controls.Add(aquaZoneButton);
            buttonPanel.Controls.Add(gymButton);
            buttonPanel.Controls.Add(groupProgramsButton);
            buttonPanel.Controls.Add(closeButton);

            previewForm.Controls.Add(inputPanel);
            previewForm.Controls.Add(timePanel);
            previewForm.Controls.Add(pictureBox);
            previewForm.Controls.Add(buttonPanel);

            previewForm.ShowDialog();
        }

        private bool ValidateInputs(TextBox commentBox, TextBox phoneBox, RadioButton[] radioButtons)
        {
            if (string.IsNullOrWhiteSpace(phoneBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните телефон.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool isTimeSelected = false;
            foreach (RadioButton radio in radioButtons)
            {
                if (radio.Checked)
                {
                    isTimeSelected = true;
                    break;
                }
            }

            if (!isTimeSelected)
            {
                MessageBox.Show("Пожалуйста, выберите время.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateInputs(TextBox commentBox, TextBox phoneBox)
        {
            if (string.IsNullOrWhiteSpace(phoneBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните телефон.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }


        private async void SendImageToTelegram(string caption, Bitmap image)
        {
            Bitmap upscaledImage = UpscaleImage(image);
            string chatId = config["ChatId"];
            string token = config["BotToken"];

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

                        var response = await client.PostAsync($"https://api.telegram.org/bot{token}/sendPhoto?chat_id={chatId}", form);
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
