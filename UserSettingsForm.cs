// UserSettingsForm.cs
using System;
using System.Windows.Forms;

namespace forms_screenshot_sender
{
    public partial class UserSettingsForm : Form
    {
        public ScreenshotUser screenshotUser;

        public UserSettingsForm(ScreenshotUser user)
        {
            user.CheckOrCreateRecord();
            // Устанавливаем размеры окна
            this.Width = 400;
            this.Height = 600;

            // Запрещаем изменение размера окна
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            screenshotUser = user;
            // Инициализация динамических элементов
            InitializeDynamicControls();
        }


        private void InitializeDynamicControls()
        {
            // Размеры и положение элементов
            int labelWidth = 120;
            int textBoxWidth = 180;
            int labelHeight = 25;
            int textBoxHeight = 25;
            int verticalSpacing = 35;
            int startY = 20;

            // Список всех полей для отображения на форме
            var fields = new (string label, string value)[]
            {
                ("UniqueId", screenshotUser.UniqueId),
                ("Sender", screenshotUser.Sender),
                ("HomeStartX", screenshotUser.HomeStartX.ToString()),
                ("HomeStartY", screenshotUser.HomeStartY.ToString()),
                ("HomeWidth", screenshotUser.HomeWidth.ToString()),
                ("HomeHeight", screenshotUser.HomeHeight.ToString()),
                ("F11StartX", screenshotUser.F11StartX.ToString()),
                ("F11StartY", screenshotUser.F11StartY.ToString()),
                ("F11Width", screenshotUser.F11Width.ToString()),
                ("F11Height", screenshotUser.F11Height.ToString()),
                ("UpscaleFactor", screenshotUser.UpscaleFactor.ToString()),
                ("BorderColor", screenshotUser.BorderColor),
                ("BorderThickness", screenshotUser.BorderThickness.ToString())
            };

            // Создаем динамические элементы для каждой пары "label" и "value"
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                // Создаем Label
                var label = new Label
                {
                    Text = field.label,
                    Width = labelWidth,
                    Height = labelHeight,
                    Top = startY + i * verticalSpacing,
                    Left = 20
                };

                // Создаем TextBox для отображения значения
                var textBox = new TextBox
                {
                    Text = field.value,
                    Width = textBoxWidth,
                    Height = textBoxHeight,
                    Top = startY + i * verticalSpacing,
                    Left = 160
                };

                // Добавляем элементы на форму
                Controls.Add(label);
                Controls.Add(textBox);
            }

            // Кнопка сохранения
            var saveButton = new Button
            {
                Text = "Сохранить",
                Width = 100,
                Height = 30,
                Top = startY + fields.Length * verticalSpacing + 20,
                Left = 20
            };
            saveButton.Click += SaveButton_Click;

            // Добавляем кнопку на форму
            Controls.Add(saveButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Проходим по всем элементам и сохраняем их значения
            int i = 0;
            foreach (Control control in Controls)
            {
                if (control is TextBox textBox)
                {
                    switch (i)
                    {
                        case 0: screenshotUser.UniqueId = textBox.Text; break;
                        case 1: screenshotUser.Sender = textBox.Text; break;
                        case 2: screenshotUser.HomeStartX = int.Parse(textBox.Text); break;
                        case 3: screenshotUser.HomeStartY = int.Parse(textBox.Text); break;
                        case 4: screenshotUser.HomeWidth = int.Parse(textBox.Text); break;
                        case 5: screenshotUser.HomeHeight = int.Parse(textBox.Text); break;
                        case 6: screenshotUser.F11StartX = int.Parse(textBox.Text); break;
                        case 7: screenshotUser.F11StartY = int.Parse(textBox.Text); break;
                        case 8: screenshotUser.F11Width = int.Parse(textBox.Text); break;
                        case 9: screenshotUser.F11Height = int.Parse(textBox.Text); break;
                        case 10: screenshotUser.UpscaleFactor = int.Parse(textBox.Text); break;
                        case 11: screenshotUser.BorderColor = textBox.Text; break;
                        case 12: screenshotUser.BorderThickness = int.Parse(textBox.Text); break;
                    }
                    i++;
                }
            }

            // Перезагружаем или обновляем запись в базе данных
            screenshotUser.UpdateRecord();

            // Закрытие формы после сохранения
            this.Close();
        }
    }
}
