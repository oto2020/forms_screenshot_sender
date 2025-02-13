// DatabaseHelper.cs
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Linq;

namespace forms_screenshot_sender
{
    public static class DatabaseHelper
    {
        private static readonly string connectionString = "Server=mysql.phys.su;Database=g1_fitness_dir_bot;Uid=igo4ek;Pwd=47sd$k32Geme!666;";

        // Получение списка пользователей
        public static List<MyObject> GetUsersFromDatabase()
        {
            List<MyObject> users = new List<MyObject>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT 
                        u.name, 
                        u.chatId, 
                        u.vpt_list, 
                        u.wishVptCount,
                        COUNT(v.id) AS factVptCount
                    FROM 
                        User u
                    LEFT JOIN 
                        VPTRequest v 
                        ON u.id = v.userId
                        AND YEAR(v.createdAt) = YEAR(CURRENT_DATE())
                        AND MONTH(v.createdAt) = MONTH(CURRENT_DATE())
                    GROUP BY 
                        u.id";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new MyObject
                            {
                                Name = reader["name"].ToString(),
                                chatId = reader["chatId"].ToString(),
                                factVptCount = Int32.Parse(reader["factVptCount"].ToString()),
                                wishVptCount = Int32.Parse(reader["wishVptCount"].ToString()),
                                Departments = reader["vpt_list"]
                                    .ToString()
                                    .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                    .ToList()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}");
                }
            }

            return users;
        }

        // Проверка или создание записи ScreenshotUser
        public static void CheckOrCreateScreenshotUser(ScreenshotUser user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM ScreenshotUser WHERE uniqueId = @uniqueId";
                    MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@uniqueId", user.UniqueId);

                    int recordCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (recordCount > 0) return;

                    string insertQuery = @"
                    INSERT INTO ScreenshotUser 
                    (uniqueId, Sender, create_time, HomeStartX, HomeStartY, HomeWidth, HomeHeight, 
                     F11StartX, F11StartY, F11Width, F11Height, UpscaleFactor, BorderColor, BorderThickness)
                    VALUES 
                    (@uniqueId, @Sender, CURRENT_TIMESTAMP, @HomeStartX, @HomeStartY, @HomeWidth, @HomeHeight, 
                     @F11StartX, @F11StartY, @F11Width, @F11Height, @UpscaleFactor, @BorderColor, @BorderThickness)";

                    MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@uniqueId", user.UniqueId);
                    insertCommand.Parameters.AddWithValue("@Sender", user.Sender);
                    insertCommand.Parameters.AddWithValue("@HomeStartX", user.HomeStartX);
                    insertCommand.Parameters.AddWithValue("@HomeStartY", user.HomeStartY);
                    insertCommand.Parameters.AddWithValue("@HomeWidth", user.HomeWidth);
                    insertCommand.Parameters.AddWithValue("@HomeHeight", user.HomeHeight);
                    insertCommand.Parameters.AddWithValue("@F11StartX", user.F11StartX);
                    insertCommand.Parameters.AddWithValue("@F11StartY", user.F11StartY);
                    insertCommand.Parameters.AddWithValue("@F11Width", user.F11Width);
                    insertCommand.Parameters.AddWithValue("@F11Height", user.F11Height);
                    insertCommand.Parameters.AddWithValue("@UpscaleFactor", user.UpscaleFactor);
                    insertCommand.Parameters.AddWithValue("@BorderColor", user.BorderColor);
                    insertCommand.Parameters.AddWithValue("@BorderThickness", user.BorderThickness);

                    insertCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании записи: " + ex.Message);
                }
            }
        }

        // Загрузка данных ScreenshotUser из базы
        public static void LoadScreenshotUser(ScreenshotUser user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string selectQuery = @"
                    SELECT Sender, HomeStartX, HomeStartY, HomeWidth, HomeHeight, 
                           F11StartX, F11StartY, F11Width, F11Height, UpscaleFactor, 
                           BorderColor, BorderThickness
                    FROM ScreenshotUser 
                    WHERE uniqueId = @uniqueId";

                    MySqlCommand selectCommand = new MySqlCommand(selectQuery, connection);
                    selectCommand.Parameters.AddWithValue("@uniqueId", user.UniqueId);

                    using (MySqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user.Sender = reader["Sender"].ToString();
                            user.HomeStartX = reader.GetInt32("HomeStartX");
                            user.HomeStartY = reader.GetInt32("HomeStartY");
                            user.HomeWidth = reader.GetInt32("HomeWidth");
                            user.HomeHeight = reader.GetInt32("HomeHeight");
                            user.F11StartX = reader.GetInt32("F11StartX");
                            user.F11StartY = reader.GetInt32("F11StartY");
                            user.F11Width = reader.GetInt32("F11Width");
                            user.F11Height = reader.GetInt32("F11Height");
                            user.UpscaleFactor = reader.GetInt32("UpscaleFactor");
                            user.BorderColor = reader["BorderColor"].ToString();
                            user.BorderThickness = reader.GetInt32("BorderThickness");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
                }
            }
        }

        // Обновление записи ScreenshotUser
        public static void UpdateScreenshotUser(ScreenshotUser user)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string updateQuery = @"
                    UPDATE ScreenshotUser 
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
                    updateCommand.Parameters.AddWithValue("@uniqueId", user.UniqueId);
                    updateCommand.Parameters.AddWithValue("@Sender", user.Sender);
                    updateCommand.Parameters.AddWithValue("@HomeStartX", user.HomeStartX);
                    updateCommand.Parameters.AddWithValue("@HomeStartY", user.HomeStartY);
                    updateCommand.Parameters.AddWithValue("@HomeWidth", user.HomeWidth);
                    updateCommand.Parameters.AddWithValue("@HomeHeight", user.HomeHeight);
                    updateCommand.Parameters.AddWithValue("@F11StartX", user.F11StartX);
                    updateCommand.Parameters.AddWithValue("@F11StartY", user.F11StartY);
                    updateCommand.Parameters.AddWithValue("@F11Width", user.F11Width);
                    updateCommand.Parameters.AddWithValue("@F11Height", user.F11Height);
                    updateCommand.Parameters.AddWithValue("@UpscaleFactor", user.UpscaleFactor);
                    updateCommand.Parameters.AddWithValue("@BorderColor", user.BorderColor);
                    updateCommand.Parameters.AddWithValue("@BorderThickness", user.BorderThickness);

                    updateCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении записи: " + ex.Message);
                }
            }
        }


        public static bool UpdateVPTRequestPhoto(long requestId, string photo)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string updateQuery = "UPDATE VPTRequest SET photo = @photo WHERE id = @requestId";
                    MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@photo", photo);
                    updateCommand.Parameters.AddWithValue("@requestId", requestId);

                    int rowsAffected = updateCommand.ExecuteNonQuery();

                    return rowsAffected > 0; // Возвращает true, если запись обновлена
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении фото: " + ex.Message);
                    return false;
                }
            }
        }


        public static long SaveVPTRequestToDatabase(ScreenshotUser screenshotUser, string phoneNumber, string comment, string visitTime, string chatId, string photo, string goal)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Получаем userId по chatId
                    string getUserQuery = "SELECT id FROM User WHERE chatId = @chatId LIMIT 1";
                    MySqlCommand getUserCommand = new MySqlCommand(getUserQuery, connection);
                    getUserCommand.Parameters.AddWithValue("@chatId", chatId);

                    object userIdObj = getUserCommand.ExecuteScalar();

                    if (userIdObj == null)
                    {
                        MessageBox.Show("Ошибка: Пользователь с таким chatId не найден в базе данных.");
                        return -1;
                    }

                    long userId = Convert.ToInt64(userIdObj);

                    // Добавляем новую запись в VPTRequest
                    string insertQuery = @"
                INSERT INTO VPTRequest (userId, screenshotUserId, visitTime, phoneNumber, comment, createdAt, status, photo, goal)
                VALUES (@userId, @screenshotUserId, @visitTime, @phoneNumber, @comment, CURRENT_TIMESTAMP, 'none', @photo, @goal);
                SELECT LAST_INSERT_ID();";

                    MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@userId", userId);
                    insertCommand.Parameters.AddWithValue("@screenshotUserId", screenshotUser.UniqueId);
                    insertCommand.Parameters.AddWithValue("@visitTime", visitTime);
                    insertCommand.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                    insertCommand.Parameters.AddWithValue("@comment", comment);
                    insertCommand.Parameters.AddWithValue("@photo", photo);
                    insertCommand.Parameters.AddWithValue("@goal", goal);

                    object insertedIdObj = insertCommand.ExecuteScalar();

                    if (insertedIdObj != null)
                    {
                        return Convert.ToInt64(insertedIdObj);
                    }
                    else
                    {
                        return -1; // Ошибка при получении ID
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении заявки: " + ex.Message);
                    return -1;
                }
            }
        }

    }
}
