using Cadmus.Export.Filters;
using Cadmus.General.Parts;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Export.Test;

public sealed class XsltJsonRendererTest
{
    private static XDocument GetSampleDocument(int count = 3)
    {
        XDocument doc = new(new XElement("root"));
        for (int i = 0; i < count; i++)
        {
            doc.Root!.Add(new XElement("entries",
                new XElement("a", i),
                new XElement("b", i)));
        }
        return doc;
    }

    [Fact]
    public void WrapXmlArrays_Single_Changed()
    {
        Dictionary<XName, XName> map = new()
        {
            ["entries"] = "entry"
        };
        XDocument doc = GetSampleDocument(1);

        XsltJsonRenderer.WrapXmlArrays(doc, map);

        Assert.NotNull(doc.Root!.Element("entries"));
        for (int i = 0; i < 1; i++)
        {
            XElement? entry = doc.Root.Element("entries")!
                .Elements("entry").Skip(i).FirstOrDefault();
            Assert.NotNull(entry);
            Assert.Equal($"<entry><a>{i}</a><b>{i}</b></entry>",
                entry.ToString(SaveOptions.DisableFormatting));
        }
    }

    [Fact]
    public void WrapXmlArrays_Array_Changed()
    {
        Dictionary<XName, XName> map = new()
        {
            ["entries"] = "entry"
        };
        XDocument doc = GetSampleDocument();

        XsltJsonRenderer.WrapXmlArrays(doc, map);

        Assert.NotNull(doc.Root!.Element("entries"));
        for (int i = 0; i < 3; i++)
        {
            XElement? entry = doc.Root.Element("entries")!
                .Elements("entry").Skip(i).FirstOrDefault();
            Assert.NotNull(entry);
            Assert.Equal($"<entry><a>{i}</a><b>{i}</b></entry>",
                entry.ToString(SaveOptions.DisableFormatting));
        }
    }

    [Fact]
    public void Render_XsltOnly_Ok()
    {
        XsltJsonRenderer renderer = new();
        renderer.Configure(new XsltJsonRendererOptions
        {
            Xslt = TestHelper.LoadResourceText("TokenTextPart.xslt")
        });
        TokenTextPart text = CadmusPreviewerTest.GetSampleTextPart();
        string json = JsonSerializer.Serialize(text, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string result = renderer.Render(json);

        Assert.NotNull(result);
        Assert.Equal("[CIL 1,23]\r\n1  que bixit\r\n2  annos XX\r\n", result);
    }

    [Fact]
    public void Render_XsltOnlyWithArrayWrap_Ok()
    {
        XsltJsonRenderer renderer = new();
        renderer.Configure(new XsltJsonRendererOptions
        {
            Xslt = TestHelper.LoadResourceText("TokenTextPartWrap.xslt"),
            WrappedEntryNames = new Dictionary<string, string>
            {
                ["lines"] = "line"
            }
        });
        TokenTextPart text = CadmusPreviewerTest.GetSampleTextPart();
        string json = JsonSerializer.Serialize(text, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string result = renderer.Render(json);

        Assert.NotNull(result);
        Assert.Equal("[CIL 1,23]\r\n1  que bixit\r\n2  annos XX\r\n", result);
    }

    [Fact]
    public void Render_JmesPathOnly_Ok()
    {
        XsltJsonRenderer renderer = new();
        renderer.Configure(new XsltJsonRendererOptions
        {
            JsonExpressions = new[] { "root.citation" },
            QuoteStripping = true
        });
        TokenTextPart text = CadmusPreviewerTest.GetSampleTextPart();
        string json = JsonSerializer.Serialize(text, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string result = renderer.Render(json);

        Assert.NotNull(result);
        Assert.Equal("CIL 1,23", result);
    }

    [Fact]
    public void Render_JmesPathOnlyMd_Ok()
    {
        XsltJsonRenderer renderer = new();
        renderer.Configure(new XsltJsonRendererOptions
        {
            JsonExpressions = new[] { "root.text" },
            QuoteStripping = true,
        });
        MarkdownRendererFilter filter = new();
        filter.Configure(new MarkdownRendererFilterOptions
        {
            Format = "html"
        });
        renderer.Filters.Add(filter);

        NotePart note = new()
        {
            CreatorId = "zeus",
            UserId = "zeus",
            Text = "This is a *note* using MD"
        };
        string json = JsonSerializer.Serialize(note, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string result = renderer.Render(json);

        Assert.NotNull(result);
        Assert.Equal("<p>This is a <em>note</em> using MD</p>\n", result);
    }
}
