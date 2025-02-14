// DynamicPanel.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using forms_screenshot_sender;
using MySql.Data.MySqlClient;

public class DynamicPanel : UserControl
{
    // ПУБЛИЧНОЕ СОБЫТИЕ, на которое можно подписаться извне
    public event EventHandler<MyObjectEventArgs> ObjectClicked;

    public TextBox searchBox;
    private FlowLayoutPanel mainContainer;
    private Dictionary<string, FlowLayoutPanel> departmentPanels; // Panels for each department
    public List<MyObject> objects; // List of objects from the database
    private string connectionString; // Database connection string

    public DynamicPanel(string connectionString)
    {
        this.connectionString = connectionString;

        // Initialize UI
        InitializeUI();

        // Load data from the database
        LoadDataFromDatabase();

        // Create panels for departments
        CreateDepartmentPanels();
    }

    private void InitializeUI()
    {
        // Создаем TextBox для поиска
        searchBox = new TextBox { Width = 200, Height = 30 };
        searchBox.TextChanged += SearchBox_TextChanged;

        // Инициализируем главный контейнер (горизонтальная компоновка)
        mainContainer = new DoubleBufferedFlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,   // Горизонтальная прокрутка при избытке
            Padding = new Padding(2),
            Width = 600,
            Height = 400
        };

        // Добавляем mainContainer в UserControl
        Controls.Add(mainContainer);

        // Словарь для панелей отделов
        departmentPanels = new Dictionary<string, FlowLayoutPanel>();
    }

    private void LoadDataFromDatabase()
    {
        objects = DatabaseHelper.GetUsersFromDatabase();
    }

    public FlowLayoutPanel GetMainPanel()
    {
        // Возвращаем главный контейнер
        return mainContainer;
    }

    private void CreateDepartmentPanels()
    {
        // Получаем уникальные отделы
        var uniqueDepartments = objects
            .SelectMany(o => o.Departments)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        foreach (var department in uniqueDepartments)
        {
            // Создаем панель для одного «отдела» (фиксированный размер)
            var panel = new DoubleBufferedFlowLayoutPanel
            {
                AutoSize = false,
                Width = 200,
                Height = 350,

                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = false
            };

            // Заголовок
            var headerLabel = new Label
            {
                Text = "Группа: " + department,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(headerLabel);

            // Сохраняем в словарь
            departmentPanels[department] = panel;

            // Добавляем панель в общий контейнер
            mainContainer.Controls.Add(panel);
        }

        // Отображаем все объекты (без фильтра)
        UpdateButtons("");
    }

    // Двойная буферизация для плавной перерисовки
    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
        }

        // Для правильного расчета размеров при AutoSize
        public override Size GetPreferredSize(Size proposedSize)
        {
            return base.GetPreferredSize(new Size(Width, 0));
        }
    }

    private void UpdateButtons(string filter)
    {
        mainContainer.SuspendLayout();

        foreach (var panel in departmentPanels.Values)
        {
            panel.SuspendLayout();
        }

        try
        {
            var filteredObjects = string.IsNullOrWhiteSpace(filter)
                ? objects
                : objects.Where(obj =>
                      obj.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                  .ToList();

            foreach (var kvp in departmentPanels)
            {
                var panel = kvp.Value;
                var header = panel.Controls[0];

                panel.Controls.Clear();
                panel.Controls.Add(header);
            }

            foreach (var department in departmentPanels.Keys)
            {
                var panel = departmentPanels[department];

                var departmentObjects = filteredObjects
                    .Where(o => o.Departments.Contains(department))
                    .OrderByDescending(o => o.wishVptCount - o.factVptCount) // Сортируем по нехватке заявок
                    .ThenBy(o => o.wishVptCount - o.factVptCount <= 0) // Перемещаем тех, кому не требуется заявка, вниз
                    .ToList();

                foreach (var obj in departmentObjects)
                {
                    string buttonText = $"{obj.Name} ({obj.factVptCount}/{obj.wishVptCount})";

                    var button = new Button
                    {
                        Text = buttonText,
                        Width = 180,
                        MinimumSize = new Size(0, 40),
                        Font = new Font("Segoe UI", 10, FontStyle.Regular),
                        Tag = Tuple.Create(obj, department)
                    };

                    var textSize = TextRenderer.MeasureText(
                        button.Text,
                        button.Font,
                        new Size(button.Width - button.Padding.Horizontal, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl
                    );

                    button.Height = Math.Max(
                        button.MinimumSize.Height,
                        textSize.Height + button.Padding.Vertical + 20
                    );

                    button.Click += Button_Click;
                    panel.Controls.Add(button);
                }
            }
        }
        finally
        {
            foreach (var panel in departmentPanels.Values)
            {
                panel.Height = panel.Controls.Count * 63;
                panel.ResumeLayout(true);
            }

            mainContainer.ResumeLayout(true);
            this.PerformLayout();
        }
    }



    private void SearchBox_TextChanged(object sender, EventArgs e)
    {
        var filter = searchBox.Text;
        UpdateButtons(filter);
    }

    // Метод обработки клика внутри DynamicPanel
    private void Button_Click(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is Tuple<MyObject, string> tag)
        {
            MyObject obj = tag.Item1;
            string department = tag.Item2;

            // Вызываем событие, передавая объект и отдел
            OnObjectClicked(obj, department);
        }
    }



    // Поднимаем событие ObjectClicked
    protected virtual void OnObjectClicked(MyObject obj, string department)
    {
        // Создаём аргумент события
        var args = new MyObjectEventArgs { Data = obj, Department = department };

        // Вызываем событие, если на него кто-то подписан
        ObjectClicked?.Invoke(this, args);
    }

}

// Класс с данными для события
public class MyObjectEventArgs : EventArgs
{
    public MyObject Data { get; set; }
    public string Department { get; set; } // Добавляем свойство
}


public class MyObject
{
    public string Name { get; set; }
    public string chatId { get; set; }
    public int wishVptCount { get; set; }
    public int factVptCount { get; set; }
    public List<string> Departments { get; set; }
}
