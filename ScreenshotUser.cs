// ScreenshotUser.cs
using System;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace forms_screenshot_sender
{
    public class ScreenshotUser
    {
        // Поля для данных
        public string UniqueId { get; set; }
        public string Sender { get; set; }
        public DateTime CreateTime { get; set; }
        public int HomeStartX { get; set; }
        public int HomeStartY { get; set; }
        public int HomeWidth { get; set; }
        public int HomeHeight { get; set; }
        public int F11StartX { get; set; }
        public int F11StartY { get; set; }
        public int F11Width { get; set; }
        public int F11Height { get; set; }
        public int UpscaleFactor { get; set; }
        public string BorderColor { get; set; }
        public int BorderThickness { get; set; }

        private string connectionString = "Server=mysql.phys.su;Database=remarks;Uid=igo4ek;Pwd=47sd$k32Geme!666;";

        // Конструктор, который проверяет и создает/актуализирует запись в базе данных
        public ScreenshotUser()
        {
            // Устанавливаем дефолтные значения
            Sender = "Координатор";
            HomeStartX = 150;
            HomeStartY = 210;
            HomeWidth = 860;
            HomeHeight = 810;
            F11StartX = 150;
            F11StartY = 210;
            F11Width = 860;
            F11Height = 810;
            UpscaleFactor = 2;
            BorderColor = "Silver";
            BorderThickness = 20;

            // Получаем уникальный ID
            UniqueId = UniqueIdProvider.GetUniqueId();

            // Проверяем или создаем запись в базе данных
            CheckOrCreateRecord();
            // обновляем все вышеперечисленные поля инфой из БД, так как инфа из БД важней этой статики
            LoadFromDatabase();
        }

        // Получение уникального ID
        public async Task<string> GetUniqueIdAsync()
        {
            try
            {
                // Получаем имя пользователя
                string userName = Environment.UserName;

                // Получаем имя компьютера
                string machineName = Environment.MachineName;

                // Получаем серийный номер диска через WMI
                string volumeSerialNumber = await Task.Run(() =>
                {
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = 'C:'"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                return obj["VolumeSerialNumber"]?.ToString() ?? "Unknown";
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return "Unknown";
                    }
                    return "Unknown";
                });

                // Формируем уникальный идентификатор
                return $"{userName}_{machineName}_{volumeSerialNumber}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании уникального идентификатора: {ex.Message}");
                return "Unknown_UniqueId";
            }
        }

        public void UpdateRecord()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Запрос на обновление всех полей
                    string updateQuery = @"
                UPDATE remarks.ScreenshotUser 
                SET 
                    Sender = @Sender, 
                    HomeStartX = @HomeStartX, 
                    HomeStartY = @HomeStartY, 
                    HomeWidth = @HomeWidth, 
                    HomeHeight = @HomeHeight, 
                    F11StartX = @F11StartX, 
                    F11StartY = @F11StartY, 
                    F11Width = @F11Width, 
                    F11Height = @F11Height, 
                    UpscaleFactor = @UpscaleFactor, 
                    BorderColor = @BorderColor, 
                    BorderThickness = @BorderThickness
                WHERE uniqueId = @uniqueId";

                    MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection);

                    // Добавляем параметры для обновления
                    updateCommand.Parameters.AddWithValue("@uniqueId", UniqueId);
                    updateCommand.Parameters.AddWithValue("@Sender", Sender);
                    updateCommand.Parameters.AddWithValue("@HomeStartX", HomeStartX);
                    updateCommand.Parameters.AddWithValue("@HomeStartY", HomeStartY);
                    updateCommand.Parameters.AddWithValue("@HomeWidth", HomeWidth);
                    updateCommand.Parameters.AddWithValue("@HomeHeight", HomeHeight);
                    updateCommand.Parameters.AddWithValue("@F11StartX", F11StartX);
                    updateCommand.Parameters.AddWithValue("@F11StartY", F11StartY);
                    updateCommand.Parameters.AddWithValue("@F11Width", F11Width);
                    updateCommand.Parameters.AddWithValue("@F11Height", F11Height);
                    updateCommand.Parameters.AddWithValue("@UpscaleFactor", UpscaleFactor);
                    updateCommand.Parameters.AddWithValue("@BorderColor", BorderColor);
                    updateCommand.Parameters.AddWithValue("@BorderThickness", BorderThickness);

                    // Выполняем запрос на обновление
                    int rowsAffected = updateCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // MessageBox.Show("Запись успешно обновлена для " + UniqueId);
                    }
                    else
                    {
                        MessageBox.Show("Запись с UniqueId " + UniqueId + " не найдена.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении записи: " + ex.Message);
                }
            }
        }

        // обновляет поля из базы данных если запись по uniqueId найдена
        public void LoadFromDatabase()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // SQL-запрос для получения данных по uniqueId
                    string selectQuery = @"
                SELECT Sender, HomeStartX, HomeStartY, HomeWidth, HomeHeight, 
                       F11StartX, F11StartY, F11Width, F11Height, UpscaleFactor, 
                       BorderColor, BorderThickness
                FROM remarks.ScreenshotUser 
                WHERE uniqueId = @uniqueId";

                    MySqlCommand selectCommand = new MySqlCommand(selectQuery, connection);
                    selectCommand.Parameters.AddWithValue("@uniqueId", UniqueId);

                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read()) // Если запись найдена
                        {
                            // Обновляем поля объекта из базы данных
                            Sender = reader["Sender"].ToString();
                            HomeStartX = reader.GetInt32("HomeStartX");
                            HomeStartY = reader.GetInt32("HomeStartY");
                            HomeWidth = reader.GetInt32("HomeWidth");
                            HomeHeight = reader.GetInt32("HomeHeight");
                            F11StartX = reader.GetInt32("F11StartX");
                            F11StartY = reader.GetInt32("F11StartY");
                            F11Width = reader.GetInt32("F11Width");
                            F11Height = reader.GetInt32("F11Height");
                            UpscaleFactor = reader.GetInt32("UpscaleFactor");
                            BorderColor = reader["BorderColor"].ToString();
                            BorderThickness = reader.GetInt32("BorderThickness");

                            // MessageBox.Show("Поля объекта успешно обновлены из базы данных.");
                        }
                        else
                        {
                            MessageBox.Show("Запись с UniqueId не найдена.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных из базы данных: " + ex.Message);
                }
            }
        }



        // Метод для работы с базой данных (проверка или создание/актуализация записи)
        // Метод для работы с базой данных (проверка или создание записи)
        public void CheckOrCreateRecord()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Создаем команду для проверки существования записи
                    string checkQuery = "SELECT COUNT(*) FROM remarks.ScreenshotUser WHERE uniqueId = @uniqueId";
                    MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@uniqueId", UniqueId);

                    // Выполняем запрос проверки
                    int recordCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    // Если запись существует, ничего не делаем
                    if (recordCount > 0)
                    {
                        return;
                    }

                    // Если запись не существует, создаем новую запись
                    string insertQuery = @"
            INSERT INTO remarks.ScreenshotUser 
            (uniqueId, Sender, create_time, HomeStartX, HomeStartY, HomeWidth, HomeHeight, 
             F11StartX, F11StartY, F11Width, F11Height, UpscaleFactor, BorderColor, BorderThickness)
            VALUES 
            (@uniqueId, @Sender, CURRENT_TIMESTAMP, @HomeStartX, @HomeStartY, @HomeWidth, @HomeHeight, 
             @F11StartX, @F11StartY, @F11Width, @F11Height, @UpscaleFactor, @BorderColor, @BorderThickness)";

                    MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@uniqueId", UniqueId);
                    insertCommand.Parameters.AddWithValue("@Sender", Sender);
                    insertCommand.Parameters.AddWithValue("@HomeStartX", HomeStartX);
                    insertCommand.Parameters.AddWithValue("@HomeStartY", HomeStartY);
                    insertCommand.Parameters.AddWithValue("@HomeWidth", HomeWidth);
                    insertCommand.Parameters.AddWithValue("@HomeHeight", HomeHeight);
                    insertCommand.Parameters.AddWithValue("@F11StartX", F11StartX);
                    insertCommand.Parameters.AddWithValue("@F11StartY", F11StartY);
                    insertCommand.Parameters.AddWithValue("@F11Width", F11Width);
                    insertCommand.Parameters.AddWithValue("@F11Height", F11Height);
                    insertCommand.Parameters.AddWithValue("@UpscaleFactor", UpscaleFactor);
                    insertCommand.Parameters.AddWithValue("@BorderColor", BorderColor);
                    insertCommand.Parameters.AddWithValue("@BorderThickness", BorderThickness);

                    insertCommand.ExecuteNonQuery();
                    MessageBox.Show("Создана новая запись в БД для " + UniqueId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }


    }
    public static class UniqueIdProvider
    {
        // Статическая переменная для хранения результата
        private static string _cachedUniqueId;

        // Синхронный метод для получения уникального идентификатора
        public static string GetUniqueId()
        {
            // Если идентификатор уже вычислен, возвращаем его
            if (!string.IsNullOrEmpty(_cachedUniqueId))
            {
                return _cachedUniqueId;
            }

            // Иначе выполняем вычисление
            _cachedUniqueId = CalculateUniqueId();
            return _cachedUniqueId;
        }

        // Метод для вычисления уникального идентификатора
        private static string CalculateUniqueId()
        {
            try
            {
                string userName = Environment.UserName;
                string machineName = Environment.MachineName;
                string volumeSerialNumber = "Unknown";

                // Получение серийного номера через WMI
                try
                {
                    using (var searcher = new System.Management.ManagementObjectSearcher("SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = 'C:'"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            volumeSerialNumber = obj["VolumeSerialNumber"]?.ToString() ?? "Unknown";
                            break;
                        }
                    }
                }
                catch
                {
                    volumeSerialNumber = "Unknown";
                }

                return $"{userName}_{machineName}_{volumeSerialNumber}";
            }
            catch
            {
                return "Unknown_UniqueId";
            }
        }
    }

}
