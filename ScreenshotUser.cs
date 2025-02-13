// ScreenshotUser.cs
using System;
using System.Windows.Forms;

namespace forms_screenshot_sender
{
    public class ScreenshotUser
    {
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

        // Конструктор
        public ScreenshotUser()
        {
            // Дефолтные значения
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
            DatabaseHelper.CheckOrCreateScreenshotUser(this);

            // Загружаем данные из базы (заменяет дефолтные значения)
            DatabaseHelper.LoadScreenshotUser(this);
        }

        // Метод обновления данных в БД
        public void UpdateRecord()
        {
            DatabaseHelper.UpdateScreenshotUser(this);
        }
    }

    // Провайдер уникального ID
    public static class UniqueIdProvider
    {
        private static string _cachedUniqueId;

        public static string GetUniqueId()
        {
            if (!string.IsNullOrEmpty(_cachedUniqueId))
                return _cachedUniqueId;

            _cachedUniqueId = CalculateUniqueId();
            return _cachedUniqueId;
        }

        private static string CalculateUniqueId()
        {
            try
            {
                string userName = Environment.UserName;
                string machineName = Environment.MachineName;
                string volumeSerialNumber = "Unknown";

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
