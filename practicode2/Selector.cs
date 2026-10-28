

using System.Text.RegularExpressions;

namespace practicode2
{
    public class Selector
    {


        public string Id { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public List<string> Classes { get; set; } = new();
        public Selector? Parent { get; set; }
        public Selector? Child { get; set; }


        public static Selector Parse(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty.");

            // חלוקה לרמות לפי רווחים
            string[] parts = Regex.Split(query.Trim(), @"\s+");

            // יצירת השורש
            Selector root = new Selector();
            Selector currentSelector = root;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                // חלוקה לפי # ונקודה, תוך שמירה על המפרידים
                MatchCollection matches = Regex.Matches(
                    part,
                    @"([.#])?([^.#]+)"
                );

                foreach (Match match in matches)
                {
                    string prefix = match.Groups[1].Value;
                    string value = match.Groups[2].Value;

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    // חיפוש לפי ID
                    if (prefix == "#")
                    {
                        currentSelector.Id = value;
                    }
                    // חיפוש לפי Class
                    else if (prefix == ".")
                    {
                        currentSelector.Classes.Add(value);
                    }
                    // חיפוש לפי שם תגית
                    else
                    {
                        bool isValidTag = HtmlHelper.Instance.AllTags.Any(
                            tag => NormalizeTagName(tag).Equals(
                                value,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                        if (!isValidTag)
                            throw new ArgumentException($"'{value}' is not a valid HTML tag name.");

                        currentSelector.TagName = value;
                    }
                }

                // יצירת ילד רק אם נשארה רמה נוספת בשאילתה
                if (i < parts.Length - 1)
                {
                    currentSelector.Child = new Selector
                    {
                        Parent = currentSelector
                    };

                    currentSelector = currentSelector.Child;
                }
            }

            return root;
        }

        private static string NormalizeTagName(string tag)
        {
            return tag.Trim()
                .TrimStart('<')
                .TrimStart('/')
                .TrimEnd('>')
                .Trim();
        }
    }


}
