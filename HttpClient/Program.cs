using Core.Entities;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace HttpClientConsole
{
    internal class Program
    {
        private const string _serverIp = "127.0.0.1";
        private const int _serverPort = 8080;
        private const string resource = "peoples";

        private static string RequestUri
        {
            get
            {
                return $"http://{_serverIp}:{_serverPort}/{resource}/";
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
                    Console.WriteLine($"Выберите действие\n1 - Получение списка людей\n2 - Добавление нового человека");
                    if (!int.TryParse(Console.ReadLine(), out userAction) || userAction <= 0) Console.WriteLine("Действие не существует");
                }
                Console.WriteLine();

                await HandleUserActionChoice(httpClient, userAction);
            }
        }

        private static void WritePersons(List<Person> persons)
        {
            Console.WriteLine("Список людей:");
            foreach (Person person in persons)
            {
                Console.WriteLine("Айди - " + person.id);
                Console.WriteLine("Имя - " + person.name);
                Console.WriteLine("Возраст - " + person.age);
                Console.WriteLine();
            }
        }

        private async static Task HandleUserActionChoice(HttpClient httpClient, int action)
        {
            switch (action)
            {
                case 1:
                    {
                        await GetPeoplesAction(httpClient);
                        break;
                    }
                case 2:
                    {
                        await CreatePeopleAction(httpClient);
                        break;
                    }
            }
        }

        private static async Task GetPeoplesAction(HttpClient httpClient)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUri);

            HttpResponseMessage responseMessage =  await httpClient.SendAsync(httpRequestMessage);

            if (responseMessage.IsSuccessStatusCode) {
                string payload = await responseMessage.Content.ReadAsStringAsync();

                List<Person> persons = JsonSerializer.Deserialize<List<Person>>(payload);
                WritePersons(persons);
            }
            else
            {
                Console.WriteLine("Произошла ошибка - " + responseMessage.StatusCode.ToString());   
            }

        }

        private async static Task CreatePeopleAction(HttpClient httpClient)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

            string name = "";
            while (string.IsNullOrEmpty(name) == true)
            {
                Console.WriteLine("Введите имя:");
                name = Console.ReadLine();
            }

            int age = 0;
            while (age <= 0)
            {
                Console.WriteLine("Введите возраст:");
                int.TryParse(Console.ReadLine(), out age);
                if (age < 0) Console.WriteLine("Возраст должен быть положительным");
            }
            Console.WriteLine();

            Person person = new Person(Guid.NewGuid().ToString(), name, age);

            string payload = JsonSerializer.Serialize(person);

            request.Content = new StringContent(payload, Encoding.UTF8);

            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();

                List<Person> persons = JsonSerializer.Deserialize<List<Person>>(responseText);

                WritePersons(persons);
            }
            else
            {
                Console.WriteLine(response.StatusCode);
            }
        }
    }
}
