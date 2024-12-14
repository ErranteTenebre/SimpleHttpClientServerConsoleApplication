using Core.Entities;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Core.Dto;

namespace HttpClientConsole
{
    internal class Program
    {
        private const string _serverIp = "127.0.0.1";
        private const int _serverPort = 8000;
        private const string resource = "peoples";

        private static readonly int[] availableUserActions = { 1, 2, 3, 4, 5 };

        //Settings
        private static bool _useClientValidation = false;


        private static string BaseServerURL
        {
            get
            {
                return $"http://{_serverIp}:{_serverPort}/";
            }
        }
        static async Task Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();

            while (true)
            {
                Console.WriteLine(new string('-', 40)); 
                int userAction = 0;
                while (userAction <= 0)
                {
                    Console.WriteLine($"Выберите действие\n1 - Получение списка пользователей\n2 - Добавление нового пользователя\n3 - Редактирование пользователя по id\n4 - Удаление пользователя по id\n5 - Удаление всех пользователей");
                    if (!int.TryParse(Console.ReadLine(), out userAction) || !availableUserActions.Contains(userAction)) Console.WriteLine("Действие не существует");
                }
                Console.WriteLine();

                await HandleUserActionChoice(httpClient, userAction);
            }
        }

        private static void WritePersons(List<User> users)
        {
            Console.WriteLine("Список людей:");
            foreach (User user in users)
            {
                WriteUserInfo(user);
            }
        }

        private static void WriteUserInfo(User user)
        {
            Console.WriteLine("Айди - " + user.Id);
            Console.WriteLine("Имя - " + user.Name);
            Console.WriteLine("Возраст - " + user.Age);
            Console.WriteLine();
        }

        private async static Task HandleUserActionChoice(HttpClient httpClient, int action)
        {
            switch (action)
            {
                case 1:
                    {

                        await HandleGetPeoplesAction(httpClient);
                        break;
                    }
                case 2:
                    {
                        await HandleCreatePeopleAction(httpClient);
                        break;
                    }
                case 3:
                    {
                        await HandleEditUserAction(httpClient);
                        break;
                    }
                case 4:
                    {
                        await HandleRemoveUserByIdAction(httpClient);
                        break;
                    }
                case 5:
                    {
                        await HandleRemoveAllUsersAction(httpClient);
                        break;
                    }
            }
        }

        private static async Task HandleGetPeoplesAction(HttpClient httpClient)
        {
            try
            {
                // Создание запроса к серверу
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseServerURL}peoples");

                // Отправка запроса
                HttpResponseMessage responseMessage = await httpClient.SendAsync(httpRequestMessage);

                // Проверка успешности ответа
                if (responseMessage.IsSuccessStatusCode)
                {
                    string payload = await responseMessage.Content.ReadAsStringAsync();

                    // Попытка десериализации ответа
                    List<User> persons = JsonSerializer.Deserialize<List<User>>(payload);
                    if (persons != null)
                    {
                        WritePersons(persons);
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: не удалось десериализовать список пользователей.");
                    }
                }
                else
                {
                    Console.WriteLine($"Произошла ошибка при получении данных: {responseMessage.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка сети: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка десериализации данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неизвестная ошибка: {ex.Message}");
            }
        }

        private static async Task HandleCreatePeopleAction(HttpClient httpClient)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BaseServerURL}peoples/");

            string name = "";
            do
            {
                Console.WriteLine("Введите имя:");
                name = Console.ReadLine();
            } while  (_useClientValidation && string.IsNullOrEmpty(name) == true);

            int age = 0;
            do
            {
                Console.WriteLine("Введите возраст:");
                int.TryParse(Console.ReadLine(), out age);
                if (_useClientValidation && age < 0) Console.WriteLine("Возраст должен быть положительным");
            } while (_useClientValidation && age <= 0);
            Console.WriteLine();

            User person = new User(Guid.NewGuid().ToString(), name, age);

            string payload = JsonSerializer.Serialize(person);

            request.Content = new StringContent(payload, Encoding.UTF8);

            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();

                List<User> persons = JsonSerializer.Deserialize<List<User>>(responseText);

                WritePersons(persons);
            }
            else
            {
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Статус код - {response.StatusCode}, {responseMessage}");
            }
        }

        private static async Task HandleEditUserAction(HttpClient httpClient)
        {
            string userId = GetUserId("Введите id пользователя для редактирования (Формат должен соответствовать GUID)");

            string? userName = null;
            int userAge = 0;
            while (string.IsNullOrEmpty(userName)) {
                Console.WriteLine("Введите имя пользователя");
                userName = Console.ReadLine();
            }

            while (userAge <= 0) {
                Console.WriteLine("Введите возраст пользователя");

                string? value = Console.ReadLine();  

                if (!int.TryParse(value, out userAge)) Console.WriteLine("Возраст должен быть положительным числом");
            }         

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, BaseServerURL + $"/peoples/{userId}/");

            requestMessage.Content = new StringContent(JsonSerializer.Serialize(new EditUserDto(userName, userAge)), Encoding.UTF8);

            HttpResponseMessage responseMessage =  await httpClient.SendAsync(requestMessage);

            string responseContent = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.IsSuccessStatusCode) {
                Console.WriteLine("Данные пользователя изменены.");
            }
            else
            {
                Console.WriteLine("Произошла ошибка - " + responseContent);
            }
        }

        private static async Task HandleRemoveUserByIdAction(HttpClient httpClient)
        {
            string userId = GetUserId("Введите ID пользователя для удаления (Формат должен соответствовать GUID)");

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{BaseServerURL}peoples/{userId}/");

            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Пользователь с ID {userId} успешно удалён.");
            }
            else
            {
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка: {response.StatusCode}, {responseMessage}");
            }
        }

        private static string GetUserId(string message)
        {
            string userId = "";
            do
            {
                Console.WriteLine(message);
                userId = Console.ReadLine();
            } while (_useClientValidation && !Guid.TryParse(userId, out _));
            return userId;
        }

        static async Task HandleRemoveAllUsersAction(HttpClient client)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{BaseServerURL}peoples/");

            try
            {
                HttpResponseMessage response = await client.SendAsync(requestMessage);

                string responseMessage = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(responseMessage);
                }
                else {
                    Console.WriteLine("Произошла ошибка - " + responseMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}"); 
            }
        }
    }
}
