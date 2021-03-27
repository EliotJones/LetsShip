using System;
using HtmlAgilityPack;

namespace PriceFalcon.Web.Services
{
    public static class IframeHtmlPreparer
    {
        public static string PrepareHtml(string html, Uri website)
        {
            var doc = new HtmlDocument();

            var server = website.AbsoluteUri.Replace(website.AbsolutePath, string.Empty).TrimEnd('/');

            doc.LoadHtml(html);

            var scripts = doc.DocumentNode.SelectNodes("//script");

            foreach (var script in scripts)
            {
                script.Remove();
            }

            var links = doc.DocumentNode.SelectNodes("//link");

            foreach (var link in links)
            {
                var attr = link.Attributes["href"];

                if (attr?.Value?.StartsWith("//") == true)
                {
                    attr.Value = "https:" + attr.Value;
                }
            }

            var images = doc.DocumentNode.SelectNodes("//img");

            foreach (var image in images)
            {
                var attr = image.Attributes["lazy-src"];

                if (attr?.Value?.StartsWith("/") == true)
                {
                    attr.Value = server + attr.Value;
                }

                var srcAttr = image.Attributes["src"];

                if (srcAttr?.Value?.StartsWith("/") == true)
                {
                    srcAttr.Value = server + srcAttr.Value;
                }
            }

            var anchors = doc.DocumentNode.SelectNodes("//a");

            foreach (var anchor in anchors)
            {
                var attr = anchor.Attributes["href"];

                attr?.Remove();

                anchor.Attributes["target"]?.Remove();
            }

            var labels = doc.DocumentNode.SelectNodes("//label");

            foreach (var label in labels)
            {
                var attr = label.Attributes["for"];

                attr?.Remove();
            }

            const string jqueryScript =
                @"<script src=""https://code.jquery.com/jquery-3.6.0.slim.min.js"" integrity=""sha256-u7e5khyithlIdTpu22PHhENmPcRdFiHRjhAuHcs05RI="" crossorigin=""anonymous""></script>";

            var jakeweary = HtmlNode.CreateNode(jqueryScript);
            var minWidth = HtmlNode.CreateNode(
                @"
            <style>
                body { min-width: 900px; }
            </style>");

            var head = doc.DocumentNode.SelectSingleNode("//head");
            head.PrependChild(jakeweary);
            head.PrependChild(minWidth);

            var myscript = HtmlNode.CreateNode(
                @"
            <script type='text/javascript'>
                function getParentPayloadString(elem) {
                    const html = $(elem).prop('outerHTML');

                    const lineage = [];

                    let item = elem;
                    let i = 0;
                    while (!!item && i < 10) {
                        item = $(item).parent();
                        i++;
                        if (!item || item.length === 0) {
                            break;
                        }

                        item = item[0];
                        const tag = item.nodeName;
                        const id = item.id;
                        const classes = item.className;
                        lineage.push({
                            tag: tag,
                            id: id,
                            classes: classes,
                            name: item.getAttribute('name')
                        });
                    }

                    const result = {
                        element: html,
                        lineage: lineage,
                        text: $(elem).text()
                    };

                    return JSON.stringify(result);
                }

                $(function() {
                    $('*').click(function(e) {
                        if (document.lastHighlighted != null) {
                            $(document.lastHighlighted).css('background-color', '');
                        }

                        $(this).css('background-color', 'yellow');
                        document.lastHighlighted = this;
                        e.stopPropagation();

                        const elem = this;
                        const message = getParentPayloadString(elem);
                        // Let the parent website know.
                        window.parent.postMessage(message, '*');
                    });
                });
            </script>");

            var body = doc.DocumentNode.SelectSingleNode("//body");
            body.AppendChild(myscript);

            return doc.DocumentNode.OuterHtml;
        }
    }
}
