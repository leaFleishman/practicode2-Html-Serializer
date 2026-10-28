namespace practicode2
{
    public class HtmlElement
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Attributes { get; set; } = new();
        public List<string> Classes { get; set; } = new();
        public string InnerHtml { get; set; } = string.Empty;
        public List<HtmlElement> Children { get; set; } = new();
        public HtmlElement? Parent { get; set; }

        public IEnumerable<HtmlElement> Descendants()
        {
            Queue<HtmlElement> queue = new();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                HtmlElement current = queue.Dequeue();
                yield return current;

                foreach (HtmlElement child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }
        public IEnumerable<HtmlElement> Ancestors()
        {
            HtmlElement? current = Parent;

            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }
    }
}









namespace practicode2
{
    public static class HtmlElementExtensions
    {
        public static HashSet<HtmlElement> FindBySelector(
     this HtmlElement element,
     Selector selector)
        {
            var results = new HashSet<HtmlElement>();

            Search(element, selector, results);

            return results;
        }

        private static void Search(
            HtmlElement element,
            Selector? selector,
            HashSet<HtmlElement> results)
        {
            if (selector == null)
                return;

            var descendants = element.Descendants()
                .Where(descendant => !ReferenceEquals(descendant, element));

            var matchingElements = descendants
                .Where(descendant => Matches(descendant, selector))
                .ToHashSet(); // מניעת כפילויות גם ברמות הביניים

            // אם זה הסלקטור האחרון, מוסיפים את התוצאות
            if (selector.Child == null)
            {
                foreach (var match in matchingElements)
                {
                    results.Add(match);
                }

                return;
            }

            // ממשיכים לסלקטור הבא
            foreach (var match in matchingElements)
            {
                Search(match, selector.Child, results);
            }
        }

        private static bool Matches(
            HtmlElement element,
            Selector selector)
        {
            // בדיקת שם התגית
            if (!string.IsNullOrEmpty(selector.TagName) &&
                !element.Name.Equals(
                    selector.TagName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // בדיקת ID
            if (!string.IsNullOrEmpty(selector.Id))
            {
                if (!string.Equals(element.Id, selector.Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            // כל המחלקות שבסלקטור חייבות להתאים
            foreach (string className in selector.Classes)
            {
                if (!element.Classes.Contains(className))
                    return false;
            }

            return true;
        }
    }
}
