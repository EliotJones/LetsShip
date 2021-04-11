using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using PriceFalcon.Domain;

namespace PriceFalcon.Crawler
{
    public static class XPathCalculator
    {
        public static bool TryGetXPath(string html, HtmlElementSelection selection, out string xpath)
        {
            xpath = null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var element = new HtmlDocument();
            element.LoadHtml(selection.Element);
            var tagType = element.DocumentNode.FirstChild.Name;

            var idAttr = element.DocumentNode.FirstChild.Attributes["id"];

            if (idAttr != null && !string.IsNullOrWhiteSpace(idAttr.Value))
            {
                xpath = $"//{tagType}[@id='{idAttr.Value}']";
                var idNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                if (idNodes.Count == 1)
                {
                    return true;
                }
            }

            var classesAttr = element.DocumentNode.FirstChild.Attributes["class"];

            if (classesAttr != null && !string.IsNullOrWhiteSpace(classesAttr.Value))
            {
                xpath = $"//{tagType}[@class='{classesAttr.Value}']";
                var classNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                if (classNodes.Count == 1)
                {
                    return true;
                }
            }

            var itemPropAttr = element.DocumentNode.FirstChild.Attributes["itemprop"];

            if (itemPropAttr != null && !string.IsNullOrWhiteSpace(itemPropAttr.Value))
            {
                xpath = $"//{tagType}[@itemprop='{itemPropAttr.Value}']";
                var itemPropNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                if (itemPropNodes.Count == 1)
                {
                    return true;
                }
            }

            var dataRoleAttr = element.DocumentNode.FirstChild.Attributes["data-role"];

            if (dataRoleAttr != null && !string.IsNullOrWhiteSpace(dataRoleAttr.Value))
            {
                xpath = $"//{tagType}[@data-role='{dataRoleAttr.Value}']";
                var dataRoleNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                if (dataRoleNodes.Count == 1)
                {
                    return true;
                }
            }

            var previous = new List<HtmlElementSummary>();
            foreach (var parent in selection.Lineage.Take(5))
            {
                var parentIdAttr = parent.Id;

                if (!string.IsNullOrWhiteSpace(parentIdAttr))
                {
                    var idNodes = doc.DocumentNode.SelectNodes($"//{parent.Tag.ToLowerInvariant()}[@id='{parentIdAttr!}']").ToList();

                    if (idNodes.Count == 1)
                    {
                        if (previous.Count > 0)
                        {

                        }
                        // Inspect
                    }
                }

                var parentClassAttr = parent.Classes;

                if (!string.IsNullOrWhiteSpace(parentClassAttr))
                {
                    xpath = $"//{parent.Tag.ToLowerInvariant()}[@class='{parentClassAttr}']";

                    var classNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                    if (classNodes.Count != 1)
                    {
                        previous.Add(parent);
                        continue;
                    }

                    var matchExact = string.Equals(classNodes[0].InnerText.Trim(), selection.Text.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (matchExact)
                    {
                        return true;
                    }

                    if (previous.Count == 0)
                    {
                        previous.Add(parent);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(previous[0].Classes))
                    {
                        // The most exact nested selector.
                        xpath = $"{xpath}//{previous[0].Tag.ToLowerInvariant()}[@class='{previous[0].Classes}']";

                        classNodes = doc.DocumentNode.SelectNodes(xpath).ToList();

                        if (classNodes.Count == 1 && string.Equals(classNodes[0].InnerText.Trim(), selection.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    

                    if (classNodes.Count == 1 && classNodes[0].InnerText == selection.Text)
                    {
                        return true;
                    }
                }

                previous.Add(parent);
            }

            return false;
        }
    }
}