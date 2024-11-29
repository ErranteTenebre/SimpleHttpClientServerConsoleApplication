using Core.Entities;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HttpClient2
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

                    Console.WriteLine("Список людей:");
                    WritePersons(persons);
                }
                else
                {
                    Console.WriteLine(response.StatusCode);
                }
            }
        }

        static void WritePersons(List<Person> persons)
        {
            foreach (Person person in persons)
            {
                Console.WriteLine("Айди - " + person.id);
                Console.WriteLine("Имя - " + person.name);
                Console.WriteLine("Возраст - " + person.age);
                Console.WriteLine();
            }
        }
    }
}
