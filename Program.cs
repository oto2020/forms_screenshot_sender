using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;

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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext(config));

            mutex.ReleaseMutex();
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
                    new MenuItem("Сделать скриншот", TakeScreenshot),
                    new MenuItem("Выход", Exit)
                }),
                Visible = true
            };

            MessageBox.Show("Чтобы отправить ВПТ, нажмите кнопку Home.", "Добро пожаловать", MessageBoxButtons.OK, MessageBoxIcon.Information);
            trayIcon.MouseClick += TrayIcon_MouseClick;

            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_KeyDown;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TakeScreenshot(sender, e);
            }
        }

        private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Home)
            {
                TakeScreenshot(sender, e);
            }
        }

        private async void TakeScreenshot(object sender, EventArgs e)
        {
            int startX = int.Parse(config["StartX"]);
            int startY = int.Parse(config["StartY"]);
            int width = int.Parse(config["Width"]);
            int height = int.Parse(config["Height"]);

            Rectangle bounds = new Rectangle(startX, startY, width, height);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                Clipboard.SetImage(bitmap);
                ShowPreviewWindow(bitmap);
            }
        }

        private Bitmap UpscaleImage(Bitmap original)
        {
            int newWidth = original.Width * 2;
            int newHeight = original.Height * 2;

            Bitmap upscaled = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return upscaled;
        }

        private void ShowPreviewWindow(Bitmap screenshot)
        {
            Form previewForm = new Form
            {
                Text = "Скриншот",
                Size = new Size(800, 515),
                StartPosition = FormStartPosition.CenterScreen
            };

            PictureBox pictureBox = new PictureBox
            {
                Image = screenshot,
                Dock = DockStyle.Top,
                Height = 400,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            TextBox commentBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 60,
                Multiline = false
            };

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Height = 60,
                Padding = new Padding(10)
            };

            Button groupProgramsButton = new Button { Text = "Групповые программы", Width = 170, Height = 40 };
            Button gymButton = new Button { Text = "Тренажерный зал", Width = 170, Height = 40 };
            Button aquaZoneButton = new Button { Text = "Аква-зона", Width = 170, Height = 40 };
            Button ftButton = new Button { Text = "ФТ", Width = 80, Height = 40 }; // New "ФТ" button
            Button closeButton = new Button { Text = "Закрыть", Width = 120, Height = 40 };

            groupProgramsButton.Click += (s, e) =>
            {
                SendImageToTelegram(config["GroupProgramsTelegram"] + " ВПТ ГП\nКомментарий: " + commentBox.Text, screenshot);
                groupProgramsButton.Enabled = false;
            };
            gymButton.Click += (s, e) =>
            {
                SendImageToTelegram(config["GymTelegram"] + " ВПТ ТЗ\nКомментарий: " + commentBox.Text, screenshot);
                gymButton.Enabled = false;
            };
            aquaZoneButton.Click += (s, e) =>
            {
                SendImageToTelegram(config["AquaZoneTelegram"] + " ВПТ Аква\nКомментарий: " + commentBox.Text, screenshot);
                aquaZoneButton.Enabled = false;
            };
            ftButton.Click += (s, e) =>
            {
                SendImageToTelegram(config["FTTelegram"] + " ФТ\nКомментарий: " + commentBox.Text, screenshot);
                ftButton.Enabled = false;
            };
            closeButton.Click += (s, e) => { previewForm.Close(); };

            // Add buttons to the panel in the desired order
            buttonPanel.Controls.Add(aquaZoneButton);
            buttonPanel.Controls.Add(gymButton);
            buttonPanel.Controls.Add(groupProgramsButton);
            buttonPanel.Controls.Add(ftButton); // Add "ФТ" button before "Закрыть"
            buttonPanel.Controls.Add(closeButton);

            previewForm.Controls.Add(commentBox);
            previewForm.Controls.Add(pictureBox);
            previewForm.Controls.Add(buttonPanel);

            previewForm.ShowDialog();
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
