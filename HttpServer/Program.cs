using Core.Dto;
using Core.Entities;
using HttpServer;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    private const string _serverIp = "127.0.0.1";
    private const int _serverPort = 8080;
    private const string UsersStoreFileName = "personsList.json";

    private static readonly string[] prefixes = new string[] { $"http://{_serverIp}:{_serverPort}/peoples/" };

    private static UserStore _userStore = new UserStore();
    private readonly List<WebRoute> _routes = new();

    static async Task Main(string[] args)
    {
        LoadPersonsFromFile();

        HttpListener listener = new HttpListener();

        foreach (string prefix in prefixes)
        {
            listener.Prefixes.Add(prefix);
        }

        var router = new WebRouter();

        router.RegisterRoute("get /peoples", async (context, match) =>
        {
            await HandleGetUsersAction(context);
        });

        router.RegisterRoute("post /peoples", async (context, match) =>
        {
            await HandleCreateUserAction(context);
        });

        router.RegisterRoute("delete /peoples/{id}", async (context, match) =>
        {
            string userId = match.Groups["id"].Value;
            await HandleDeleteUserAction(context, userId);
        });

        router.RegisterRoute("delete /peoples", async (context, match) =>
        {
            await HandleDeleteAllAction(context);
        });

        router.RegisterRoute("put /peoples/{id}", async (context, match) =>
        {
            string userId = match.Groups["id"].Value;
            await HandleEditUserAction(context, userId);
        });

        listener.Start();
        Console.WriteLine($"Сервер слушает по ip {_serverIp} на порту {_serverPort}");

        while (true)
        {
            HttpListenerContext httpContext = await listener.GetContextAsync();

            HttpListenerRequest request = httpContext.Request;
            HttpListenerResponse response = httpContext.Response;

            string routeKey = $"{httpContext.Request.HttpMethod} {httpContext.Request.RawUrl}".ToLower();

            await router.CheckRoute(routeKey, httpContext);

            ConfigureHttpResponseHeaders(response);
        }

        static void LoadPersonsFromFile()
        {
            if (File.Exists(UsersStoreFileName))
            {
                string json = File.ReadAllText(UsersStoreFileName);
                try
                {
                    var persons = JsonSerializer.Deserialize<List<User>>(json);
                    if (persons != null)
                    {
                        foreach (var person in persons)
                        {
                            _userStore.Add(person);
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

                var newPersonsList = new List<User>();

                string emptyJson = JsonSerializer.Serialize(newPersonsList);

                try
                {
                    File.WriteAllText(UsersStoreFileName, emptyJson);
                    Console.WriteLine("Файл данных был успешно создан.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при создании файла: " + ex.Message);
                }
            }
        }

        static async Task HandleGetUsersAction(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            HttpListenerResponse response = context.Response;

            try
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_userStore.Users));
                response.Headers.Add("Content-type", "text/plain");
                response.ContentType = "text/json";
                response.ContentLength64 = responseBytes.Length;
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(responseBytes);
                Console.Write("Отправлен ответ по действию 1");
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(ex.Message));
                Console.WriteLine("Произошла ошибка - " + ex.ToString());
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        static async Task HandleCreateUserAction(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;

            HttpListenerResponse response = context.Response;

            string requestBody;
            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                requestBody = reader.ReadToEnd();
            }

            try
            {
                User user = JsonSerializer.Deserialize<User>(requestBody);

                if (user == null || string.IsNullOrEmpty(user.Name) || user.Age <= 0) throw new InvalidOperationException("Неверный формат данных");

                _userStore.Add(user);

                SaveUsersToFile();

                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_userStore.Users));

                response.Headers.Add("Content-type", "text/plain");
                response.ContentLength64 = responseBytes.Length;
                response.StatusCode = 200;

                await response.OutputStream.WriteAsync(responseBytes);
            }
            catch (JsonException)
            {
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Ошибка: некорректный формат JSON"));
            }
            catch (InvalidOperationException ex)
            {
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Ошибка:" + ex.Message));
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Внутренняя ошибка сервера:" + ex.Message));
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        static async Task HandleDeleteUserAction(HttpListenerContext context, string userId)
        {
            HttpListenerResponse response = context.Response;
            HttpListenerRequest request = context.Request;

            string[] urlParts = request.RawUrl.Split("/", StringSplitOptions.RemoveEmptyEntries);

            try {          

                if (!Guid.TryParse(userId, out _))
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Формат id пользователя не соответствует формату Guid"));
                    response.OutputStream.Close();
                    return;
                }

                bool isUserDeleted = _userStore.Remove(userId);

                if (isUserDeleted)
                {
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Пользователь с id {userId} был успешно удален"));
                }
                else
                {
                    SaveUsersToFile();
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Пользователь с указанным id не существует"));
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Внутренняя ошибка сервера - " + ex.Message));
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
    }
    static void SaveUsersToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(_userStore.Users);
            File.WriteAllText(UsersStoreFileName, json);
            Console.WriteLine("Данные сохранены в файл.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при сохранении данных в файл: " + ex.Message);
        }
    }

    static void ConfigureHttpResponseHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
        response.Headers.Add("Access-Control-Allow-Headers", "*");
    }
    static async Task HandleDeleteAllAction(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        _userStore.Clear();

        try
        {
            SaveUsersToFile();
            response.StatusCode = 200;
            response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Список пользователей успешно очищен"));
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Внутренняя ошибка сервера - " + ex.Message));
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    static async Task HandleEditUserAction(HttpListenerContext context, string userId)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;


        if (!Guid.TryParse(userId, out _))
        {
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("id пользователя не соответствует формату GUID"));
            response.OutputStream.Close();
            return;
        }

        try
        {
            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                string requestBody = reader.ReadToEnd();

                EditUserDto editUserDto = JsonSerializer.Deserialize<EditUserDto>(requestBody);

               if (_userStore.Edit(userId, editUserDto.Name, editUserDto.Age))
                {
                    SaveUsersToFile();
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Пользователь с id {userId} успешно изменен"));
                }
                else
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Пользователя с id {userId} не существует"));
                }
            }
        }
        catch (Exception ex) {
            response.StatusCode = 500;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Внутренняя ошибка сервера - " + ex.Message));
        }
        finally { response.OutputStream.Close(); }   
    }
}