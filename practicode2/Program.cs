namespace practicode2
{
    internal static class Program
    {
        private static async Task Main()
        {
            Console.Write("Enter a page URL (or a local .html file path): ");
            string? input = Console.ReadLine()?.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(input))
                return;

            var serializer = new HtmlSerializer();
            string html;

            if (File.Exists(input))
            {
                html = await File.ReadAllTextAsync(input);
            }
            else if (Uri.TryCreate(input, UriKind.Absolute, out Uri? address) &&
                     (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps))
            {
                html = await serializer.Load(address.ToString());
            }
            else
            {
                Console.WriteLine("Enter a valid HTTP/HTTPS URL or an existing file path.");
                return;
            }

            HtmlElement root = serializer.BuildTree(serializer.ParseHtml(html));
            Console.WriteLine($"Parsed {root.Descendants().Count() - 1} HTML elements.");

            while (true)
            {
                Console.Write("\nSelector (empty to exit): ");
                string? query = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(query))
                    break;

                try
                {
                    Selector selector = Selector.Parse(query);
                    var results = root.FindBySelector(selector);

                    Console.WriteLine($"Found {results.Count} element(s):");
                    foreach (HtmlElement el in results)
                    {
                        string text = el.InnerHtml.Length > 40 ? el.InnerHtml[..40] + "..." : el.InnerHtml;
                        Console.WriteLine($"  <{el.Name}> id='{el.Id}' classes=[{string.Join(", ", el.Classes)}] text='{text}'");
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
