using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxyServer
{
    class Program
    {
        private const string OriginServerUrl = "http://127.0.0.1:8080/"; 
        private const string ProxyAddress = "http://127.0.0.1:8000/";    

        static async Task Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(ProxyAddress);
            listener.Start();
            Console.WriteLine($"Прокси-сервер запущен на {ProxyAddress}");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                
                await HandleRequest(context);
            }
        }

        private static async Task HandleRequest(HttpListenerContext context)
        {
            string originUrl = OriginServerUrl + context.Request.RawUrl.TrimStart('/');
            Console.WriteLine($"Перенаправление запроса: {context.Request.HttpMethod} {originUrl}");

            try
            {
                using HttpClient httpClient = new HttpClient();
                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.HttpMethod), originUrl);

                foreach (string headerName in context.Request.Headers.AllKeys)
                {
                    if (!WebHeaderCollection.IsRestricted(headerName))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(headerName, context.Request.Headers[headerName]);
                    }
                }

                if (context.Request.HasEntityBody)
                {
                    using StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    // Поддержка динамических типов контента
                    string contentType = context.Request.ContentType.Split(';')[0]; // Получаем только основной тип (без параметров)
                    requestMessage.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

                context.Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                {
                    if (!context.Response.Headers.AllKeys.Contains(header.Key))
                    {
                        context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                    }
                }

                if (response.Content != null)
                {
                    byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                    context.Response.ContentType = response.Content.Headers.ContentType?.ToString();

                    if (context.Response.OutputStream.CanWrite)
                    {
                        await context.Response.OutputStream.WriteAsync(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] errorResponse = Encoding.UTF8.GetBytes("Ошибка на сервере прокси.");
                if (context.Response.OutputStream.CanWrite)
                {
                    await context.Response.OutputStream.WriteAsync(errorResponse, 0, errorResponse.Length);
                }
            }
            finally
            {
                // Закрываем поток только после завершения всех операций записи
                context.Response.OutputStream.Close();
            }
        }
    }
}