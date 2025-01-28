using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
        objects = new List<MyObject>();

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Пример запроса (не забудьте, что в таблице должно быть chatId)
                string query = "SELECT name, chatId, vpt_list FROM User";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["name"].ToString();
                        string vptList = reader["vpt_list"].ToString();
                        string chatId = reader["chatId"].ToString();

                        objects.Add(new MyObject
                        {
                            Name = name,
                            chatId = chatId,
                            Departments = vptList
                                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                .ToList()
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data from database: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
    }

    private void UpdateButtons(string filter)
    {
        // Приостанавливаем лейаут на всех панелях
        foreach (var panel in departmentPanels.Values)
            panel.SuspendLayout();

        try
        {
            // В каждой панели удаляем все контролы, кроме заголовка (Label)
            foreach (var kvp in departmentPanels)
            {
                var panel = kvp.Value;
                var header = panel.Controls[0]; // Заголовок — первый контрол

                panel.Controls.Clear();
                panel.Controls.Add(header);
            }

            // Фильтрация (без учёта регистра)
            var filteredObjects = string.IsNullOrWhiteSpace(filter)
                ? objects
                : objects.Where(obj =>
                      obj.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                  .ToList();

            // Для каждого объекта создаём кнопки в соответствующих отделах
            foreach (var obj in filteredObjects)
            {
                foreach (var department in obj.Departments)
                {
                    if (departmentPanels.TryGetValue(department, out var panel))
                    {
                        var button = new Button
                        {
                            Text = obj.Name,
                            AutoSize = false,
                            Size = new Size(180, 40), // под панель 200px
                            Font = new Font("Segoe UI", 12, FontStyle.Regular),
                            Tag = obj // Запоминаем объект
                        };

                        // Подписка на клик
                        button.Click += Button_Click;
                        panel.Controls.Add(button);
                    }
                }
            }
        }
        finally
        {
            // Включаем лейаут обратно
            foreach (var panel in departmentPanels.Values)
                panel.ResumeLayout(true);
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
        if (sender is Button btn && btn.Tag is MyObject obj)
        {
            // Вместо MessageBox.Show вызываем СВОЕ событие, передавая MyObject
            OnObjectClicked(obj);
        }
    }

    // Поднимаем событие ObjectClicked
    protected virtual void OnObjectClicked(MyObject obj)
    {
        // Создаём аргумент события
        var args = new MyObjectEventArgs { Data = obj };

        // Вызываем событие, если на него кто-то подписан
        ObjectClicked?.Invoke(this, args);
    }
}

// Класс с данными для события
public class MyObjectEventArgs : EventArgs
{
    public MyObject Data { get; set; }
}

public class MyObject
{
    public string Name { get; set; }
    public string chatId { get; set; }
    public List<string> Departments { get; set; }
}
