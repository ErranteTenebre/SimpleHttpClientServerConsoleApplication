using Core.Entities;
using HttpServer;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private const string _serverIp = "127.0.0.1";
    private const int _serverPort = 8080;
    private const string FilePath = "personsList.json"; 

    private static PersonStore _personStore = new PersonStore();

    static async Task Main(string[] args)
    {
        LoadPersonsFromFile();

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://{_serverIp}:{_serverPort}/peoples/");

        listener.Start();
        Console.WriteLine($"Сервер слушает по ip {_serverIp} на порту {_serverPort}");

        while (true)
        {
            HttpListenerContext httpContext = await listener.GetContextAsync();

            HttpListenerRequest request = httpContext.Request;

            HttpListenerResponse response = httpContext.Response;

            if (request.HttpMethod == "GET")
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_personStore.Persons));

                try
                {
                    await response.OutputStream.WriteAsync(responseBytes);
                    Console.Write("Отправлен ответ по действию 1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка при отправке ответа - " + ex.ToString());
                }
                finally
                {
                    response.OutputStream.Close();
                }
            }
            else if (request.HttpMethod == "POST")
            {
                string requestBody;
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBody = reader.ReadToEnd();
                }

                Person person = JsonSerializer.Deserialize<Person>(requestBody);
                _personStore.Add(person);

                // Сохраняем изменённый список в файл
                SavePersonsToFile();

                ConfigureHttpResponseHeaders(response);

                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_personStore.Persons));

                try
                {
                    await response.OutputStream.WriteAsync(responseBytes);
                    Console.Write("Отправлен ответ по действию 2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка при отправке ответа - " + ex.ToString());
                }
                finally
                {
                    response.OutputStream.Close();
                }
            }
        }
    }

    static void ConfigureHttpResponseHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
        response.Headers.Add("Access-Control-Allow-Headers", "*");
        response.Headers.Add("Content-type", "text/html");
    }

    static void LoadPersonsFromFile()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            try
            {
                var persons = JsonSerializer.Deserialize<List<Person>>(json);
                if (persons != null)
                {
                    foreach (var person in persons)
                    {
                        _personStore.Add(person);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при загрузке данных из файла: " + ex.Message);
            }
        }
        else
        {
            Console.WriteLine("Файл данных не найден, создадим новый.");
        }
    }
    static void SavePersonsToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(_personStore.Persons);
            File.WriteAllText(FilePath, json);
            Console.WriteLine("Данные сохранены в файл.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при сохранении данных в файл: " + ex.Message);
        }
    }
}