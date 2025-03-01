# Configuration Example - Apparatus Part for UI

🌐 For more real-world examples of UI preview scripts, see the [Cadmus previews repository](https://github.com/vedph/cadmus-previews).

This example shows the configuration for previewing an apparatus in the Cadmus editor UI. This essentially means using a JSON renderer to render XML code (derived from the JSON code representing a note) into HTML, via an XSLT script.

This sample configuration is used to render a fragment from a critical apparatus layer part.

When rendering the apparatus, we want to include the lemma from the base text in it. In Cadmus, the base text is a separate part, and any number of layer parts can be connected to it, by just tossing all these parts in the same box (item).

So, when rendering the apparatus we have no text available to extract the lemma from. To this end, we rather use the [token-based text extractor filter](filters.md#token-based-text-extractor-filter). This filter can be used to extract text from any fragment coordinates, when using a token-based text part (which is the prevalent case).

Thus, the configuration first includes this filter, which requires to access the underlying Cadmus database to retrieve the base text part and extract text from it. This is why this filter includes `mongo` in its ID: this is the targeted database type. Anyway, you do not need to specify a connection string in the configuration: this is rather provided by the framework.

As the filter is applied after the XSLT filter has completed its transformation, it looks for the `location` XML element, derived from the JSON property with the same name. So, its regular expression pattern targets all the occurrences of this element and extracts from it the coordinates.

Armed with these coordinates, the filter fetches the base text from the database, and locates and extracts the portion identified by them. It then ensures that this portion is not too long, as we do not want an entire line or so to be fully included in the apparatus as a lemma; rather, in this case we want to just quote its initial and final part, replacing the rest with an ellipsis.

This is what is accomplished by turning on the `TextCutting` option, and setting its mode to 3 (which cuts the body, leaving head and tail at both sides). The limit here is 80 characters, with a tolerance of ±5 characters.

Once the text has been optionally cut, it is inserted in the provided template at the `{text}` placeholder. The result is that our text gets output wrapped in a classed span element, like `<span class=\"apparatus-lemma\">the text here</span>`.

The XSLT transformation has been adapted to this further processing by just copying the location element unchanged in its output, e.g.:

```xslt
<xsl:template match=\"location\"><location><xsl:value-of select=\".\"/></location></xsl:template>
```

So, here is what happens:

1. first, the JSON code for the selected fragment is extracted from apparatus text layer part in the database. Among other properties, the fragment has `location` with a value like `6.2`.
2. the XSLT-based renderer transforms JSON into XML, and XML via XSLT into HTML.
3. the filter kicks in, and matches `<location>6.2</location>` in the output. It then fetches the text corresponding to coordinates `6.2`, optionally cuts it if too long, and replaces it with an HTML span element of a specific class (`apparatus-lemma`). The final result is thus an apparatus including also the text fetched from a distinct part in the same item.

```json
{
  "RendererFilters": [
    {
      "Keys": "token-extractor",
      "Id": "it.vedph.renderer-filter.mongo-token-extractor",
      "Options": {
        "LocationPattern": "<location>([^<]+)</location>",
        "TextTemplate": "<span class=\"apparatus-lemma\">{text}</span>",
        "TextCutting": true,
        "Mode": 3,
        "Limit": 80,
        "MinusLimit": 5,
        "PlusLimit": 5
      }
    }
  ],
  "JsonRenderers": [
    {
      "Keys": "it.vedph.token-text-layer|fr.it.vedph.apparatus",
      "Id": "it.vedph.json-renderer.xslt",
      "Options": {
        "FilterKeys": [
          "token-extractor"
        ],
        "Xslt": "<?xml version=\"1.0\" encoding=\"UTF-8\"?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" exclude-result-prefixes=\"xs\" version=\"1.0\"><xsl:output method=\"html\" /><xsl:template match=\"lemma\"><span class=\"apparatus-lemma\"><xsl:value-of select=\".\"/></span></xsl:template><xsl:template match=\"location\"><location><xsl:value-of select=\".\"/></location></xsl:template><xsl:template match=\"witnesses\"><span class=\"apparatus-w-value\"><xsl:value-of select=\"value\"/></span><xsl:if test=\"note\"><span class=\"apparatus-w-note\"><xsl:text> </xsl:text><xsl:value-of select=\"note\"/><xsl:text> </xsl:text></span></xsl:if></xsl:template><xsl:template match=\"authors\"><xsl:text> </xsl:text><span class=\"apparatus-a-value\"><xsl:value-of select=\"value\"/></span><xsl:if test=\"note\"><xsl:text> </xsl:text><span class=\"apparatus-a-note\"><xsl:value-of select=\"note\"/></span></xsl:if><xsl:text> </xsl:text></xsl:template><xsl:template match=\"entries\"><xsl:variable name=\"nr\"><xsl:number/></xsl:variable><xsl:if test=\"$nr &gt; 1\"><span class=\"apparatus-sep\">| </span></xsl:if><xsl:if test=\"tag\"><span class=\"apparatus-tag\"><xsl:value-of select=\"tag\"/></span><xsl:text> </xsl:text></xsl:if><xsl:if test=\"subrange\"><span class=\"apparatus-subrange\"><xsl:value-of select=\"subrange\"/></span><xsl:text> </xsl:text></xsl:if><xsl:if test=\"string-length(value) &gt; 0\"><span class=\"apparatus-value\"><xsl:value-of select=\"value\"/></span><xsl:text> </xsl:text></xsl:if><xsl:choose><xsl:when test=\"type = 0\"><xsl:if test=\"string-length(value) = 0\"><span class=\"apparatus-type\">del. </span></xsl:if></xsl:when><xsl:when test=\"type = 1\"><span class=\"apparatus-type\">ante lemma </span></xsl:when><xsl:when test=\"type = 2\"><span class=\"apparatus-type\">post lemma </span></xsl:when></xsl:choose><xsl:if test=\"note\"><span class=\"apparatus-note\"><xsl:value-of select=\"note\"/></span><xsl:text> </xsl:text></xsl:if><xsl:apply-templates/></xsl:template><xsl:template match=\"root\"><xsl:apply-templates/></xsl:template><xsl:template match=\"*\"/></xsl:stylesheet>"
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
