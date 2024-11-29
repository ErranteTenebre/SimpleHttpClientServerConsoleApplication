using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private const string _serverIp = "127.0.0.1";
    private const int _serverPort = 8080;

    private static string responseString = "<html><div>Привет</div></html>";
    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://{_serverIp}:{_serverPort}/");

        listener.Start();

        Console.WriteLine($"Сервер слушает по ip {_serverIp} на порту {_serverPort}");

        while (true)
        {
            HttpListenerContext httpContext = await listener.GetContextAsync();

            HttpListenerRequest request = httpContext.Request;

            HttpListenerResponse response = httpContext.Response;

            if (request.Headers["Content-Length"] == null)
            {
                response.StatusCode = 411;
                response.OutputStream.Close();
            }

            ConfigureHttpResponseHeaders(response);

            byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

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
        response.Headers.Add("Access-Control-Allow-Methods", "GET");
        response.Headers.Add("Access-Control-Allow-Headers", "*");

        response.Headers.Add("Content-type", "text/html");
    }
}