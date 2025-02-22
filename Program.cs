﻿// Program.cs
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
using System.Management;
using forms_screenshot_sender;
using Org.BouncyCastle.Asn1.Crmf;

namespace ScreenshotToTrayApp
{
    static class Program
    {
        private static Mutex mutex = new Mutex(true, "{5A8F5B39-FA5D-4B91-AFCF-6F5FDF16F973}");

        [STAThread]
        static void Main()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Программа уже запущена", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());

            mutex.ReleaseMutex();
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private IKeyboardMouseEvents globalHook;

        public TrayApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Выход", Exit)
                }),
                Visible = true
            };

            // Создаем экземпляр класса ScreenshotUser
            ScreenshotUser screenshotUser = new ScreenshotUser();

            // Выводим текущие данные на форму или консоль (если нужно)
            //MessageBox.Show($"Unique ID: {screenshotUser.UniqueId}\nSender: {screenshotUser.Sender}");

            MessageBox.Show("Здравствуйте, "+ screenshotUser.Sender + "!\n\nЧтобы отправить ВПТ, нажмите кнопку Home или F11. Координаты скриншота и ваше имя задаются в настройках\n\nВаш uniqueId: " + screenshotUser.UniqueId, "Добро пожаловать", MessageBoxButtons.OK, MessageBoxIcon.Information);




            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_KeyDown;
        }

        private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Home)
            {
                ScreenshotUser screenshotUser = new ScreenshotUser();
                TakeScreenshot(screenshotUser, screenshotUser.HomeStartX, screenshotUser.HomeStartY, screenshotUser.HomeWidth, screenshotUser.HomeHeight);
            }
            else if (e.KeyCode == Keys.F11)
            {
                ScreenshotUser screenshotUser = new ScreenshotUser();
                TakeScreenshot(screenshotUser, screenshotUser.F11StartX, screenshotUser.F11StartY, screenshotUser.F11Width, screenshotUser.F11Height);
            }
        }

        private async void TakeScreenshot(ScreenshotUser screenshotUser, int startX, int startY, int width, int height)
        {
            Rectangle bounds = new Rectangle(startX, startY, width, height);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // Draw rounded border
                DrawRoundedBorder(bitmap, screenshotUser.BorderColor, screenshotUser.BorderThickness);

                // Clipboard.SetImage(bitmap);
                ShowPreviewWindow(bitmap, screenshotUser);
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
        
        private void ShowPreviewWindow(Bitmap screenshot, ScreenshotUser screenshotUser)
        {
            // --- Создаём форму ---
            Form previewForm = new Form
            {
                Text = "Скриншот",
                StartPosition = FormStartPosition.Manual,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = new Icon(Path.Combine(Application.StartupPath, "app_icon.ico"))
            };

            // Задаём ширину 800, высоту = рабочая область экрана
            var screen = Screen.PrimaryScreen.WorkingArea;
            int formWidth = 800;
            int formHeight = screen.Height;
            int formLeft = screen.Left + (screen.Width - formWidth) / 2;
            int formTop = screen.Top;
            previewForm.SetBounds(formLeft, formTop, formWidth, formHeight);
            previewForm.TopMost = true;

            // Главная таблица на 2 строки (50% / 50%)
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65f));
            previewForm.Controls.Add(mainLayout);

            // ======================
            //       Верх (50%)
            // ======================
            Panel topPanel = new Panel { Dock = DockStyle.Fill };
            mainLayout.Controls.Add(topPanel, 0, 0);

            // Картинка (скриншот)
            PictureBox pictureBox = new PictureBox
            {
                Image = screenshot,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            topPanel.Controls.Add(pictureBox);

            // Кнопка «Настройки»
            Button settingsButton = new Button
            {
                Text = "⚙ Настройки",
                Width = 120,
                Height = 40,
                Location = new Point(10, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            settingsButton.Click += (o, e) =>
            {
                var settingsForm = new UserSettingsForm(screenshotUser);
                settingsForm.ShowDialog();
            };
            topPanel.Controls.Add(settingsButton);

            // Кнопка «Закрыть»
            Button closeButton = new Button
            {
                Text = "Закрыть",
                Width = 120,
                Height = 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            closeButton.Location = new Point(topPanel.ClientSize.Width - closeButton.Width - 10, 10);
            closeButton.Click += (s, e) => previewForm.Close();
            topPanel.Controls.Add(closeButton);

            // Картинку отправляем "назад"
            pictureBox.SendToBack();

            // При изменении размеров topPanel — сдвигаем кнопку «Закрыть» вправо
            topPanel.Resize += (s, e) =>
            {
                closeButton.Location = new Point(
                    topPanel.ClientSize.Width - closeButton.Width - 10,
                    10
                );
            };

            // ======================
            //       Низ (50%)
            // ======================
            FlowLayoutPanel bottomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainLayout.Controls.Add(bottomFlow, 0, 1);

            // Центрируем элементы при ресайзе
            bottomFlow.SizeChanged += (sender, e) =>
            {
                int clientW = bottomFlow.ClientSize.Width;
                foreach (Control ctrl in bottomFlow.Controls)
                {
                    if (ctrl is Panel p)
                    {
                        int leftMargin = Math.Max((clientW - p.Width) / 2, 0);
                        p.Margin = new Padding(leftMargin, 2, 0, 2);
                    }
                }
            };

            // ----------------------
            //   combinedPanel — горизонтальный блок
            // ----------------------
            FlowLayoutPanel combinedPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(2),
                Width = 700
            };

            // --- Панель №1 ---
            FlowLayoutPanel panel1 = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(2),
                Width = 300
            };

            // Создаём поля
            Label commentLabel = new Label { Text = "Комментарий:", AutoSize = true };
            TextBox commentBox = new TextBox { Width = 200, Height = 25 };

            Label phoneLabel = new Label { Text = "Телефон:", AutoSize = true };
            TextBox phoneBox = new TextBox { Width = 200, Height = 25 };
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                string phonePattern = @"^\+7\s?\(\d{3}\)\s?\d{3}-?\d{2}-?\d{2}$"; // Регулярное выражение для проверки номера

                if (System.Text.RegularExpressions.Regex.IsMatch(clipboardText, phonePattern))
                {
                    phoneBox.Text = clipboardText;
                }
            }


            Label searchLabel = new Label { Text = "Поиск тренера:", AutoSize = true };

            panel1.Controls.Add(commentLabel);
            panel1.Controls.Add(commentBox);
            panel1.Controls.Add(phoneLabel);
            panel1.Controls.Add(phoneBox);
            panel1.Controls.Add(searchLabel);

            // --- Панель №2 ---
            FlowLayoutPanel panel2 = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(2),
                Width = 300
            };

            Label timeLabel = new Label { Text = "Когда клиент может посетить ВПТ:", AutoSize = true };
            RadioButton wholeDayRadio = new RadioButton { Text = "Весь день", AutoSize = true };
            RadioButton morningRadio = new RadioButton { Text = "Утро", AutoSize = true };
            RadioButton lunchRadio = new RadioButton { Text = "Обед", AutoSize = true };
            RadioButton eveningRadio = new RadioButton { Text = "Вечер", AutoSize = true };

            panel2.Controls.Add(timeLabel);
            panel2.Controls.Add(wholeDayRadio);
            panel2.Controls.Add(morningRadio);
            panel2.Controls.Add(lunchRadio);
            panel2.Controls.Add(eveningRadio);

            // Вставляем panel1 и panel2 в combinedPanel
            combinedPanel.Controls.Add(panel2);
            combinedPanel.Controls.Add(panel1);

            // Добавляем combinedPanel в bottomFlow
            bottomFlow.Controls.Add(combinedPanel);

            // ----------------------
            //   DynamicPanel
            // ----------------------
            string connectionString = "Server=mysql.phys.su;Database=g1_fitness_dir_bot;Uid=igo4ek;Pwd=47sd$k32Geme!666;";
            DynamicPanel dynamicPanel = new DynamicPanel(connectionString);

            // Готовим массив радиокнопок, чтобы ValidateInputs знал, какие проверять
            RadioButton[] timeRadios = new RadioButton[] { wholeDayRadio, morningRadio, lunchRadio, eveningRadio };

            // Подписываемся на событие ObjectClicked
            // Подписываемся на событие ObjectClicked
            dynamicPanel.ObjectClicked += (sender, args) =>
            {
                if (!ValidateInputs(commentBox, phoneBox, timeRadios))
                {
                    return; // Прерываем, если не заполнены поля
                }

                string cleanedPhone = phoneBox.Text.Replace(" ", "").Replace("(", "").Replace(")", "");
                phoneBox.Text = cleanedPhone;

                string comment = commentBox.Text;
                string timeSelected = GetSelectedRadioButtonText(panel2);
                string goal = args.Department; // Используем переданный department как goal

                // Формируем caption
                string caption = $"{screenshotUser.Sender}\nОтдел: {goal}\nКомментарий: {comment}\n📞 {cleanedPhone}\nВремя: {timeSelected}\n";

                // Получаем чатID и имя тренера
                string chatId = args.Data.chatId;
                string trainerName = args.Data.Name;

                // Сохраняем в БД с переданным goal (department)
                long vptRequestId = DatabaseHelper.SaveVPTRequestToDatabase(screenshotUser, cleanedPhone, comment, timeSelected, chatId, "", goal);

                // Отправляем скриншот с текстом в Telegram
                string photoUrl = MyTelegram.SendScreenshotSync(caption, screenshot, chatId, vptRequestId);

                // Обновляем ссылку на фото в БД
                DatabaseHelper.UpdateVPTRequestPhoto(vptRequestId, photoUrl);

                // Сообщаем пользователю
                //MessageBox.Show($"Клиент передан тренеру {trainerName} ({chatId})");
                previewForm.Close();

            };

            // Добавляем поле поиска DynamicPanel в panel1 (если нужно)
            panel1.Controls.Add(dynamicPanel.searchBox);

            // Добавляем саму панель DynamicPanel
            FlowLayoutPanel dpMainPanel = dynamicPanel.GetMainPanel();
            dpMainPanel.Width = 700;
            dpMainPanel.AutoSize = true;
            dpMainPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            bottomFlow.Controls.Add(dpMainPanel);

            // Показываем форму
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


        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
