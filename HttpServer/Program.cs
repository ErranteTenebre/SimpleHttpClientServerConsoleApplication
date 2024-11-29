using Core.Entities;
using HttpServer;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private const string _serverIp = "127.0.0.1";
    private const int _serverPort = 8080;

    private static PersonStore _personStore = new PersonStore();
    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://{_serverIp}:{_serverPort}/peoples/");

        listener.Start();

        Console.WriteLine($"Сервер слушает по ip {_serverIp} на порту {_serverPort}");

        while (true)
        {
            HttpListenerContext httpContext = await listener.GetContextAsync();

            HttpListenerRequest request = httpContext.Request;

            string requestBody;
            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                requestBody = reader.ReadToEnd();
            }

            Person person = JsonSerializer.Deserialize<Person>(requestBody);
            _personStore.Add(person);

            HttpListenerResponse response = httpContext.Response;

            ConfigureHttpResponseHeaders(response);

            byte[] responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_personStore.Persons));

            try
            {
                await response.OutputStream.WriteAsync(responseBytes);
                Console.WriteLine("Ответ отправлен");
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

    static void ConfigureHttpResponseHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
        response.Headers.Add("Access-Control-Allow-Headers", "*");

        response.Headers.Add("Content-type", "text/html");
    }
}