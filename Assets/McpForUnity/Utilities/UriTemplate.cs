using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ModelContextProtocol
{
    internal class UriTemplate
    {
        private readonly string _template;
        private readonly List<string> _parameterNames = new List<string>();
        private readonly Regex _matchRegex;

        public UriTemplate(string template)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            
            var pattern = Regex.Escape(template);
            pattern = Regex.Replace(pattern, @"\{(\w+)\}", match =>
            {
                _parameterNames.Add(match.Groups[1].Value);
                return "(?<$1>[^/]+)";
            });
            
            _matchRegex = new Regex($"^{pattern}$", RegexOptions.Compiled);
        }

        public bool IsMatch(string uri)
        {
            return _matchRegex.IsMatch(uri);
        }

        public Dictionary<string, string> Match(string uri)
        {
            var match = _matchRegex.Match(uri);
            if (!match.Success)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            foreach (var name in _parameterNames)
            {
                result[name] = match.Groups[name].Value;
            }

            return result;
        }

        public string Expand(Dictionary<string, object> parameters)
        {
            var result = _template;
            foreach (var kvp in parameters)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            return result;
        }
    }
}
