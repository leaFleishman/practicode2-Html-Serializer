using System.Text.Json;

namespace practicode2
{
    public class HtmlHelper
    {
        private static readonly HtmlHelper _htmlHelper = new HtmlHelper();

        public static HtmlHelper Instance => _htmlHelper;

        public string[] AllTags { get; }
        public string[] TagsWithoutClosingTag { get; }

        private HtmlHelper()
        {
            AllTags = LoadTags("HtmlTags.json");
            TagsWithoutClosingTag = LoadTags("HtmlVoidTags.json");
        }

        private static string[] LoadTags(string fileName)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "data",
                fileName
            );

            string json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<string[]>(json)
                   ?? throw new InvalidDataException(
                       $"The JSON file '{path}' contains no tag list."
                   );
        }
    }
}