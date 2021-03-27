using System;
using System.Collections.Generic;

namespace PriceFalcon.Domain
{
    public class HtmlElementSelection
    {
        /// <summary>
        /// The inner text of the element.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Raw HTML of the element.
        /// </summary>
        public string Element { get; set; } = string.Empty;

        /// <summary>
        /// The parent elements of this element, first is immediate parent, up to the 10th parent if available.
        /// </summary>
        public IReadOnlyList<HtmlElementSummary> Lineage { get; set; } = Array.Empty<HtmlElementSummary>();
    }

    public class HtmlElementSummary
    {
        /// <summary>
        /// The type of the element, e.g. DIV, IMG, SPAN, etc.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// The HTML id attribute if present.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The HTML classes if present.
        /// </summary>
        public string? Classes { get; set; }

        /// <summary>
        /// The HTML name if present.
        /// </summary>
        public string? Name { get; set; }

        public override string ToString()
        {
            var tag = Tag.ToLowerInvariant();
            var id = string.IsNullOrWhiteSpace(Id) ? string.Empty : $" id='{Id}'";
            var name = string.IsNullOrWhiteSpace(Name) ? string.Empty : $" name='{Name}'";
            var classes = string.IsNullOrWhiteSpace(Classes) ? string.Empty : $" class='{Classes}'";
            return $"<{tag}{id}{name}{classes}/>";
        }
    }
}
