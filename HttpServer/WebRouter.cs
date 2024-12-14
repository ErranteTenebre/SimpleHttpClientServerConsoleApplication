using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpServer
{
    public class WebRouter
    {
        private readonly List<WebRoute> _routes = new();

        public void RegisterRoute(string template, Func<HttpListenerContext, Match, Task> handler)
        {
            var route = new WebRoute(template, handler);
            _routes.Add(route);
        }

        public async Task CheckRoute(string input, HttpListenerContext context)
        {
            foreach (var route in _routes)
            {
                Match match = route.Match(input);
                if (match.Success)
                {
                    await route.Handler(context, match);
                    return;
                }
            }

            Console.WriteLine("Маршрут не найден.");
            context.Response.StatusCode = 404;
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Маршрут не найден."));
            context.Response.OutputStream.Close();
        }
    }
}
