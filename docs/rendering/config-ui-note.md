# Configuration Example - Note Part for UI

🌐 For more real-world examples of UI preview scripts, see the [Cadmus previews repository](https://github.com/vedph/cadmus-previews).

This example shows the configuration for previewing a note in the Cadmus editor UI. This essentially means using a JSON renderer to render XML code (derived from the JSON code representing a note) into HTML, via an XSLT script.

A note part model just includes a Markdown text, plus an optional tag string.

As the text is Markdown, to properly render it we use a filter. Filters are applied after the renderer has completed, so in this case they work on an HTML input. The XSLT script wraps the Markdown text into a mock element named `_md`. Then, the Markdown filter replaces all the text inside this element into its HTML rendition (as specified by the `Format` option).

```json
{
  "RendererFilters": [
    {
      "Keys": "markdown",
      "Id": "it.vedph.renderer-filter.markdown",
      "Options": {
        "MarkdownOpen": "<_md>",
        "MarkdownClose": "</_md>",
        "Format": "html"
      }
    }
  ],
  "JsonRenderers": [
    {
      "Keys": "it.vedph.note",
      "Id": "it.vedph.json-renderer.xslt",
      "Options": {
        "QuoteStripping": true,
        "Xslt": "<?xml version=\"1.0\" encoding=\"UTF-8\"?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:tei=\"http://www.tei-c.org/ns/1.0\" version=\"1.0\"><xsl:output method=\"html\" encoding=\"UTF-8\" omit-xml-declaration=\"yes\"/><xsl:template match=\"tag\"><p class=\"muted\"><xsl:value-of select=\".\"/></p></xsl:template><xsl:template match=\"text\"><div><_md><xsl:value-of select=\".\"/></_md></div></xsl:template><xsl:template match=\"root\"><xsl:apply-templates/></xsl:template><xsl:template match=\"*\"/></xsl:stylesheet>",
        "FilterKeys": ["markdown"]
      }
    }
  ],
  "TextPartFlatteners": [
    {
      "Keys": "it.vedph.token-text",
      "Id": "it.vedph.text-flattener.token"
    }
  ]
}
```

Breakdown:

- the renderer filter is used to convert Markdown into HTML. Markdown is assumed to be found wrapped in a mock `_md` element.
- an XSLT-based JSON renderer renders the note in a HTML `div` with rendered Markdown in it, optionally preceded by a `p` with the tag.
- the renderer uses the Markdown filter defined above.

For more readability, here is a formatted version of the above XSLT script parameter, where I added some comments:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:tei="http://www.tei-c.org/ns/1.0" version="1.0">
    <!-- output HTML -->
    <xsl:output method="html" encoding="UTF-8" omit-xml-declaration="yes"/>
    <!-- if there is a tag, output <p @muted> with its content -->
    <xsl:template match="tag">
        <p class="muted">
            <xsl:value-of select="."/>
        </p>
    </xsl:template>
    <!-- output note's text into a div, in a mock _md wrapper -->
    <xsl:template match="text">
        <div>
            <_md>
                <xsl:value-of select="."/>
            </_md>
        </div>
    </xsl:template>
    <!-- apply matching templates from root -->
    <xsl:template match="root">
        <xsl:apply-templates/>
    </xsl:template>
    <!-- drop everything else -->
    <xsl:template match="*"/>
</xsl:stylesheet>
```
