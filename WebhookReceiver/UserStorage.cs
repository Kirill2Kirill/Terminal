using System;
using System.IO;
using System.Xml;
using Newtonsoft.Json;

namespace WebhookReceiver
{

    public static class UserStorage
    {
        private static readonly string UsersDirectory = "Users";
        private static readonly string UsersFilePath = Path.Combine(UsersDirectory, "users.json");

        public static void Initialize()
        {
            // Создаем папку "Users", если её нет
            if (!Directory.Exists(UsersDirectory))
            {
                Directory.CreateDirectory(UsersDirectory);
            }

            // Создаем файл "users.json", если его нет
            if (!File.Exists(UsersFilePath))
            {
                var users = new List<User>
            {
                new User { Name = "Пользователь1" },
                new User { Name = "Пользователь2" }
            };

                File.WriteAllText(UsersFilePath, JsonConvert.SerializeObject(users, Newtonsoft.Json.Formatting.Indented));
            }
        }

        public static async Task<List<User>> ReadUsersAsync()
        {
            if (File.Exists(UsersFilePath))
            {
                var text = await File.ReadAllTextAsync(UsersFilePath);
                return JsonConvert.DeserializeObject<List<User>>(text);
            }
            return new List<User>();
        }

        public static void UpdateUsers(List<User> updatedUsers)
        {
            File.WriteAllText(UsersFilePath, JsonConvert.SerializeObject(updatedUsers, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
