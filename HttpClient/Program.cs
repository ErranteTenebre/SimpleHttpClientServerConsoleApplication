using System.Text;

namespace HttpClient2
{
    internal class Program
    {
        private const string _serverIp = "127.0.0.1";
        private const int _serverPort = 8080;
        private static readonly string requestURI = $"http://{_serverIp}:{_serverPort}/";

        private const string payload = "Hi world, I'm Aidar";
        static async Task Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestURI);

            request.Content = new StringContent(payload, Encoding.UTF8);

            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();

                Console.WriteLine(responseText);
            }
            else
            {
                Console.WriteLine(response.StatusCode);
            }
        }
    }
}
