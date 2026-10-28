using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace practicode2
{
    public class HtmlSerializer
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly HtmlHelper _helper = HtmlHelper.Instance;

        // שלב 1: טעינת HTML מכתובת אינטרנט
        public async Task<string> Load(string url)
        {
            using HttpResponseMessage response =
                await _client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // שלב 2: פירוק ה-HTML לתגיות ולטקסט
        public List<string> ParseHtml(string html)
        {
            ArgumentNullException.ThrowIfNull(html);

            return Regex.Split(
                    html,
                    """(<!--[\s\S]*?-->|<(?:[^'""<>]|"[^"]*"|'[^']*')*>)"""
                )
                .Where(part => part.Length > 0)
                .ToList();
        }

        // שלב 3: בניית עץ האלמנטים
        public HtmlElement BuildTree(List<string> parts)
        {
            var root = new HtmlElement
            {
                Id = string.Empty,
                Name = "root",
                Attributes = new List<string>(),
                Classes = new List<string>(),
                InnerHtml = "",
                Children = new List<HtmlElement>(),
                Parent = null
            };

            HtmlElement currentElement = root;
            HtmlElement? rawTextElement = null; // script / style
            foreach (string rawPart in parts)
            {
                string part = rawPart;

                if (part.Length == 0)
                    continue;

                // בתוך script/style הכול טקסט גולמי עד התגית הסוגרת
                if (rawTextElement != null)
                {
                    bool closesRaw = Regex.IsMatch(
                        part,
                        $@"^<\s*/\s*{Regex.Escape(rawTextElement.Name)}\s*>$",
                        RegexOptions.IgnoreCase);

                    if (!closesRaw)
                    {
                        rawTextElement.InnerHtml += part;
                        continue;
                    }

                    rawTextElement = null;
                }

                if (part.StartsWith("<!--", StringComparison.Ordinal) ||
                    part.StartsWith("<!", StringComparison.Ordinal))
                {
                    continue;
                }

                // טקסט שאינו תגית
                if (!part.StartsWith("<") || !part.EndsWith(">"))
                {
                    AppendText(currentElement, part);
                    continue;
                }

                // זיהוי שם התגית, תגית סוגרת ומאפיינים
                Match match = Regex.Match(
                    part.Trim(),
                    """^<\s*(?<closing>/)?\s*(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\b(?<rest>(?:[^'""<>]|"[^"]*"|'[^']*')*)>$"""
                );

                if (!match.Success)
                    continue;

                string name = match.Groups["name"].Value;

                bool isClosing = match.Groups["closing"].Success;
                string rest = match.Groups["rest"].Value;
                bool isSelfClosing = Regex.IsMatch(rest, @"/\s*$");

                // הגענו לסוף מסמך ה-HTML
                if (isClosing &&
                    name.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // תגית סוגרת: חזרה לאלמנט האב
                if (isClosing)
                {
                    HtmlElement? matchingElement = currentElement;
                    while (matchingElement != null &&
                           !matchingElement.Name.Equals(
                               name,
                               StringComparison.OrdinalIgnoreCase))
                    {
                        matchingElement = matchingElement.Parent;
                    }

                    if (matchingElement?.Parent != null)
                        currentElement = matchingElement.Parent;

                    continue;
                }

                // בדיקה אם התגית נמצאת ברשימת התגיות המוכרות
                bool isKnownTag = _helper.AllTags.Any(tag =>
                    NormalizeTagName(tag).Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase));

                if (!isKnownTag)
                    continue;

                string attributesText = Regex.Replace(rest, @"/\s*$", "");

                List<string> attributes =
                    ParseAttributes(attributesText);

                List<string> classes =
                    ParseClasses(attributesText);
                string id = GetAttributeValue(attributesText, "id") ?? string.Empty;

                // יצירת האלמנט
                var element = new HtmlElement
                {
                    Id = id,
                    Name = name,
                    Attributes = attributes,
                    Classes = classes,
                    InnerHtml = "",
                    Children = new List<HtmlElement>(),
                    Parent = currentElement
                };

                // הוספת האלמנט לרשימת הילדים של האב
                currentElement.Children.Add(element);

                // בדיקה אם זו תגית ללא תגית סגירה
                bool isVoidTag = _helper.TagsWithoutClosingTag.Any(tag =>
                    NormalizeTagName(tag).Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase));

                // יורדים לאלמנט החדש רק אם הוא יכול להכיל ילדים
                if (!isVoidTag && !isSelfClosing)
                {
                    currentElement = element;

                    if (name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        rawTextElement = element;
                    }
                }
            }

            return root;
        }

        private static void AppendText(HtmlElement element, string text)
        {
            bool preservesWhitespace = false;
            for (HtmlElement? ancestor = element; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor.Name.Equals("pre", StringComparison.OrdinalIgnoreCase) ||
                    ancestor.Name.Equals("textarea", StringComparison.OrdinalIgnoreCase))
                {
                    preservesWhitespace = true;
                    break;
                }
            }

            if (preservesWhitespace)
            {
                element.InnerHtml += text;
                return;
            }

            string normalizedText = Regex.Replace(text, @"\s+", " ");
            if (string.IsNullOrWhiteSpace(normalizedText))
                return;

            element.InnerHtml += normalizedText;
        }

        private static string? GetAttributeValue(
            string attributesText,
            string attributeName)
        {
            Match match = Regex.Match(
                attributesText,
                $@"(?:^|\s){Regex.Escape(attributeName)}\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)'|(?<unquoted>[^\s>]+))",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            if (match.Groups["double"].Success)
                return match.Groups["double"].Value;
            if (match.Groups["single"].Success)
                return match.Groups["single"].Value;
            return match.Groups["unquoted"].Value;
        }

        // הסרת סוגריים משם תגית אם קיימים בקובץ JSON
        private static string NormalizeTagName(string tag)
        {
            return tag.Trim()
                .TrimStart('<')
                .TrimStart('/')
                .TrimEnd('>')
                .Trim()
                .ToLowerInvariant();
        }

        // פירוק Attributes לרשימת מחרוזות
        private static List<string> ParseAttributes(string attributesText)
        {
            var attributes = new List<string>();

            MatchCollection matches = Regex.Matches(
                attributesText,
                @"(?<name>[^\s=/>]+)(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+))?"
            );

            foreach (Match match in matches)
            {
                attributes.Add(match.Value.Trim());
            }

            return attributes;
        }

        // חילוץ ערכי class לרשימה נפרדת
        private static List<string> ParseClasses(string attributesText)
        {
            Match match = Regex.Match(
                attributesText,
                @"(?:^|\s)class\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)'|(?<unquoted>[^\s>]+))",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return new List<string>();

            string classValue =
                match.Groups["double"].Success
                    ? match.Groups["double"].Value
                    : match.Groups["single"].Success
                        ? match.Groups["single"].Value
                        : match.Groups["unquoted"].Value;

            return Regex.Split(classValue.Trim(), @"\s+")
                .Where(className => className.Length > 0)
                .ToList();
        }
    }
}