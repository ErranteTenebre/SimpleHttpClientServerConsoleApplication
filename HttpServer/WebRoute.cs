using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpServer
{
    public class WebRoute
    {
        private readonly Regex _regex;
        public Func<HttpListenerContext, Match, Task> Handler { get; }

        public WebRoute(string template, Func<HttpListenerContext, Match, Task> handler)
        {
            Handler = handler;
            _regex = BuildRegexFromTemplate(template);
        }

        public Match Match(string input)
        {
            return _regex.Match(input);
        }

        private static Regex BuildRegexFromTemplate(string template)
        {
            var pattern = "^" + Regex.Replace(template, @"\{(\w+)\}", @"(?<$1>[^/]+)") + "/?$";
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
