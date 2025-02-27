---
title: "Configuration Samples" 
layout: default
parent: Rendition
nav_order: 7
---

# Configuration Samples

- [Configuration Samples](#configuration-samples)
  - [Rendering Note Parts](#rendering-note-parts)
  - [Rendering Critical Apparatus Fragments](#rendering-critical-apparatus-fragments)
  - [Exporting Text with Apparatus in TEI](#exporting-text-with-apparatus-in-tei)
  - [Exporting Text Items in Plain Text](#exporting-text-items-in-plain-text)
    - [Requirements](#requirements)
    - [Data Architecture](#data-architecture)
    - [Configuration](#configuration)
    - [Sample Output](#sample-output)

Here I collect some real-world samples of render [configurations](overview.md#configuration).

>💡 For real-world examples of preview scripts, see the [Cadmus previews repository](https://github.com/vedph/cadmus-previews).

## Rendering Note Parts

This sample configuration is very basic, as it just renders the note part. A note part contains a Markdown text, plus an optional tag.

So, the XSLT-based renderer is used to convert the JSON code representing the node into XML, apply an XSLT transformation, and return an HTML result.

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
        "QuoteStripping ": true,
        "Xslt": "<?xml version=\"1.0\" encoding=\"UTF-8\"?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:tei=\"http://www.tei-c.org/ns/1.0\" version=\"1.0\"><xsl:output method=\"html\" encoding=\"UTF-8\" omit-xml-declaration=\"yes\"/><xsl:template match=\"tag\"><p class=\"muted\"><xsl:value-of select=\".\"/></p></xsl:template><xsl:template match=\"text\"><div><_md><xsl:value-of select=\".\"/></_md></div></xsl:template><xsl:template match=\"root\"><xsl:apply-templates/></xsl:template><xsl:template match=\"*\"/></xsl:stylesheet>",
        "FilterKeys": "markdown"
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

## Rendering Critical Apparatus Fragments

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

## Exporting Text with Apparatus in TEI

This sample configuration exports all the Cadmus text items into a TEI file for the text, and another TEI file for the critical apparatus, using standoff notation.

- an **item ID collector** collects all the text items (=all the items whose facet is `text`) from the underlying database, in their order.
- an **item composer** renders each text item in TEI standoff. This uses:
  - a **text parte flattener** for token-based texts, to flatten metatextual layer(s) with the base text.
  - a **text block renderer** for TEI standoff notation. In turn, this uses a simple **renderer filter** (`nl-appender`) to append a newline after each `div` element.
  - a **JSON renderer** for rendering the apparatus layer. This  will build `app` elements from each apparatus layer fragment. To add more layers, just add more renderers, each targeting a specific layer type.

```json
{
  "RendererFilters": [
    {
      "Keys": "nl-appender",
      "Id": "it.vedph.renderer-filter.appender",
      "Options": {
        "Text": "\r\n"
      }
    }
  ],
  "TextPartFlatteners": [
    {
      "Keys": "it.vedph.token-text",
      "Id": "it.vedph.text-flattener.token"
    }
  ],
  "TextBlockRenderers": [
    {
      "Keys": "tei",
      "Id": "it.vedph.text-block-renderer.tei-standoff",
      "Options": {
        "FilterKeys": ["nl-appender"]
      }
    }
  ],
  "JsonRenderers": [
    {
      "Keys": "it.vedph.token-text-layer|fr.it.vedph.apparatus",
      "Id": "it.vedph.json-renderer.tei-standoff.apparatus"
    }
  ],
  "ItemComposers": [
    {
      "Keys": "default",
      "Id": "it.vedph.item-composer.tei-standoff.fs",
      "Options": {
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextBlockRendererKey": "tei",
        "JsonRendererKeys": [
          "it.vedph.token-text-layer|fr.it.vedph.apparatus"
        ],
        "OutputDirectory": "c:\\users\\dfusi\\Desktop\\out"
      }
    }
  ],
  "ItemIdCollector": {
    "Id": "it.vedph.item-id-collector.mongo",
    "Options": {
      "FacetId": "text"
    }
  }
}
```

For instance, this is the first portion of the TEI text, for its first item. Rendition here is limited to the text itself, without contents in the header:

```xml
<body><div xml:id="r1_1"><seg xml:id="1_1_1">ADNARE AD EAM REM</seg>. <seg xml:id="1_1_3">Cic. de rep. II ‘Ut </seg><seg xml:id="1_1_4">ad eam</seg><seg xml:id="1_1_5"> urbem, quam</seg></div><div xml:id="r1_2"><seg xml:id="1_2_6">incolas, </seg><seg xml:id="1_2_7">possit</seg><seg xml:id="1_2_8"> adnare’</seg>.</div>
...
</body>
```

As you can see, `body` contains a set of children `div` elements, each representing a row of text blocks; its ID, starting with `r`, is built from the item's ordinal number plus the row's ordinal number. So, `r1_1` is item 1, row 1; while `r2_1` is item 2, row 1.

In turn, each of these "row" `div` elements contains text mixed with `seg` elements. These `seg` elements are used to wrap any arbitrarily defined portion of text under an ID, so that it can be referenced from a TEI standoff file. So, with reference to the text blocks model, each block here is either a text node (when it has no links), or a `seg` element (when it has 1 or more links).

Each `seg` element has an ID defined by `b` (=block) followed by the item's ordinal number, the text blocks row's ordinal number, and the block ordinal number.

This segmentation of the text is the result of [flattening](markup) text layers into a set of abstractions, the "text blocks", which just represent the maximum extent of text linked to the same set of annotations. This allows minimizing the requirements for text segmentation, by wrapping into `seg` only those portions of text which require to be linked from any of the layers selected for output. This approach is thus much more efficient than systematically wrapping the whole text in advance, using a fixed level of granularity (e.g. wrap each graphical word into an element with a unique ID). Here, wrapping occurs only when necessary, and might extend from 1 to N characters, with no predefined (and thus fixed) level of granularity.

You might notice here that not all the `seg` elements found in the text are referenced from the apparatus layer. That's because the sample text has many other layers besides apparatus; so, even though we are generating output only for apparatus, we stick to the blocks defined by flattening all the layers. This ensures that two different exports of the same text created by selecting different layers will produce the same segmentation.

So, in the end we have these blocks:

(a) from item 1, row 1:

- `1_1_1`: "ADNARE AD EAM REM"
- `1_1_3`: "Cic. de rep. II ‘Ut "
- `1_1_4`: "ad eam"
- `1_1_5`: " urbem, quam"

(b) from item 1, row 2:

- `1_2_6`: "incolas, "
- `1_2_7`: "possit"
- `1_2_8`: " adnare’"
- (no ID): "."

The corresponding TEI apparatus document (named after its layer type: `it.vedph.token-text-layer_fr.it.vedph.apparatus.xml`) for the first item is:

```xml
<TEI xmlns="http://www.tei-c.org/ns/1.0">
<standOff type="1">
  <div xml:id="92769c41-347b-48e0-a1e3-8f6ada7d89c2">
    <div>
      <app n="1" loc="#1_1_4">
        <rdg wit="#n1">adeam</rdg>
      </app>
      <app n="2" loc="#1_1_4">
        <rdg source="#Parrhasius">ad eam</rdg>
      </app>
    </div>
    <div>
      <app n="1" loc="#1_2_7">
        <rdg source="#Ursinus">_f._ possis</rdg>
      </app>
    </div>
  </div>
...
</standOff>
</TEI>
```

The apparatus entries are:

- `1_1_4` (targeting `ad eam`): `adeam` n1.
- `1_1_4` (targeting `ad eam`): `ad eam` Parrhasius.
- `1_2_7` (targeting `possit`): `_f._ possis` Ursinus.

## Exporting Text Items in Plain Text

This sample configuration exports a set of Cadmus text items (ignoring any layers) into a set of plain text files, to allow third party tools further process the resulting text.

### Requirements

Here the sample tool is a Chiron-based linguistic analyzer for prose rhythm, having as input a set of plain text files with the text to be analyzed. This text is preceded by a metadata header, where each metadatum is in a single line starting with `.` and having a name followed by `=` and its value. For instance, this is a document from _Constitutiones Sirmondianae_:

```txt
.date=333 Mai. 5
.date-value=333
.data=Dat. III nonas Maias Constantinopoli Dalmatio et Zenofilo conss.
.title=01 Imp(erator) Constantinus A(ugustus) ad Ablabium pp. 
Satis mīrātī sumus gravitātem tuam, quae plēna iūstitiae ac probae religiōnis est, clēmentiam nostram scīscitārī voluisse, quid dē sententiīs epīscopōrum vel ante moderātiō nostra cēnsuerit vel nunc servārī cupiāmus, Ablābī, parēns kārissime atque amantissime.
...
```

The analyzer has no other requirement for its input format. Yet, for other processing types, it might be useful to optionally pre-segment the text into sentences. For instance, this happens when dealing with NLP tokenizers in conjunction with Chiron-based linguistic analysis. In this case, we can apply a simple sentence splitter filter, which refactors the text layout to ensure that each line corresponds to a single sentence.

### Data Architecture

In our scenario, Cadmus text items have a facet equal to `text`, and use a `TokenTextPart` for the text. They also use layer parts, like critical apparatus; but here we are just interested in exporting the raw text.

As usual in Cadmus, the text is just a set of items, where each part contains a paragraph or a poetical composition cited in the context of a document. This ensures that every item stands on its own, and can get the required layers. These text portions are virtually grouped under each "work" by means of item group IDs.

The text being edited in Cadmus in this sample is _Sidonius Apollinaris_ letters. Their text is split into items at each paragraph or poetical composition, and each chunk of text belongs to a letter via its group ID, which has the form `N-NNN` where `N` is the book number (1-9) and `NNN` is the letter number in that book.

For instance, `1-002` is the second letter of the first book. This ensures that each text item contains only prose or poetry, and never a mix of the two. Poetic items are marked by a flag value of 8 (and by a final asterisk in their title).

So, we want to extract the raw text from each of these chunks, in their order, and create a new file for each letter. Also, we want some text preprocessing. For instance, many letters end with the salutation `vale`, like e.g. 1.1:

```txt
... sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur. vale.
```

As we are going to analyze prose rhythm, such salutations would introduce rumor in our analysis data. So, we want to remove them during export.

Also, the apostrophe character is used as a quote marker in these texts; so we want to replace `'` with `"`, to produce a text more compliant to the underlying character semantics defined by the Unicode standard, and honored in the linguistic analyzer.

Finally, if we are going to split text into sentences, it will be useful to move sentence-end punctuation like `.` after a quote marker, so that the sentence will not be cut leaving an orphaned closing quote.

We can easily accomplish all these preprocessing requirements using a replacement filter.

### Configuration

```json
{
  "RendererFilters": [
    {
      "Keys": "rep-filter",
      "Id": "it.vedph.renderer-filter.replace",
      "Options": {
        "Replacements": [
          {
            "Source": "([.;:?!])\\s+vale\\.[ ]*([\\r\\n]+)",
            "IsPattern": true,
            "Target": "$1$2",
            "Repetitions": 1
          },
          {
            "Source": "\\d+\\.\\s+",
            "IsPattern": true,
            "Target": "",
            "Repetitions": 1
          },
          {
            "Source": "'",
            "Target": "\"",
            "Repetitions": 1
          },
          {
            "Source": "([.?!])\"",
            "IsPattern": true,
            "Target": "\"$1"
          }
        ]
      }
    },
    {
      "Keys": "split-filter",
      "Id": "it.vedph.renderer-filter.sentence-split",
      "Options": {
        "EndMarkers": ".?!",
        "Trimming": true,
        "BlackOpeners": "(",
        "BlackClosers": ")",
        "CrLfRemoval": true
      }
    }
  ],
  "TextPartFlatteners": [
    {
      "Keys": "it.vedph.token-text",
      "Id": "it.vedph.text-flattener.token"
    }
  ],
  "TextBlockRenderers": [
    {
      "Keys": "txt",
      "Id": "it.vedph.text-block-renderer.txt",
      "Options": {
        "FilterKeys": ["rep-filter", "split-filter"]
      }
    }
  ],
  "ItemComposers": [
    {
      "Keys": "default",
      "Id": "it.vedph.item-composer.txt.fs",
      "Options": {
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextBlockRendererKey": "txt",
        "ItemGrouping": true,
        "OutputDirectory": "c:\\users\\dfusi\\Desktop\\out",
        "TextHead": ".author=Sidonius Apollinaris\r\n.date=v AD\r\n.date-value=450\r\n.title={item-title}\r\n"
      }
    }
  ],
  "ItemIdCollector": {
    "Id": "it.vedph.item-id-collector.mongo",
    "Options": {
      "FacetId": "text",
      "Flags": 8,
      "FlagMatching": 2
    }
  }
}
```

1. a replacer renderer filter is used to remove the final `vale` and eventual artifacts represented by paragraph numbers. To this end, we use a couple of regular expressions. This filter is defined with key `rep-filter`.
2. a sentence splitting filter is used to rearrange newlines so that each line corresponds to a sentence. This facilitates the usage of the target tool.
3. a text part flattener is used to flatten the token-based text part of each text item. This part's model has a list of lines, each with its text. These lines will become rows of text blocks; in this case, given that we include no layer in the output, we will just have a single block for each row.
4. a text block renderer is used to extract blocks as plain text. Also, once extracted the text gets filtered by the `rep-filter` defined above.
5. an item composer puts all these pieces together: it is a plain text, file-based composer, using the text flattener and block renderer defined above; it applies grouping, i.e. it will change its output file whenever a new group is found; uses the specified output directory, and prepends to each file a "header" with the format explained above. This header includes metadata placeholders between curly braces. For instance, `{item-title}` will be replaced by the title of each item being processed. File names instead will be equal to group IDs.
6. an item ID collector is used to collect all the text items (facet ID = `text`) from the MongoDB database containing Sidonius Apollinaris. Notice that an additional filter here is used to exclude poetic text from the export, as we do not want to have poetic text analyzed by a prose rhythm tool. So, we are excluding from collection all the items having flag 8 set (as flag 8 represents a poetic text in this database). Property `FlagMatching=2` means that we are matching all the items NOT having the specified flags set.

The command used in the CLI is (assuming that this configuration file is named `Preview-txt` under my desktop):

```ps1
./cadmus-mig render-items cadmus-sidon C:\Users\dfusi\Desktop\Preview-txt.json
```

### Sample Output

The first file output by this configuration, without the sentence splitting filter, would be:

```txt
.author=Sidonius Apollinaris
.date=v AD
.date-value=450
.title=1_001_001 Sidonius Constantio suo salutem.
Diu praecipis, domine maior, summa suadendi auctoritate, sicuti es in his quae deliberabuntur consiliosissimus, ut, si quae litterae paulo politiores varia occasione fluxerunt, prout eas causa persona tempus elicuit, omnes retractatis exemplaribus enucleatisque uno volumine includam, Quinti Symmachi rotunditatem, Gai Plinii disciplinam maturitatemque vestigiis praesumptiosis insecuturus.
nam de Marco Tullio silere melius puto, quem in stilo epistulari nec Iulius Titianus sub nominibus illustrium feminarum digna similitudine expressit. propter quod illum ceteri quique Frontonianorum utpote consectaneum aemulati, cur veternosum dicendi genus imitaretur, oratorum simiam nuncupaverunt. quibus omnibus ego immane dictu est quantum semper iudicio meo cesserim quantumque servandam singulis pronuntiaverim temporum suorum meritorumque praerogativam.
sed scilicet tibi parui tuaeque examinationi has <litterulas> non recensendas (hoc enim parum est) sed defaecandas, ut aiunt, limandasque commisi, sciens te immodicum esse fautorem non studiorum modo verum etiam studiosorum. quam ob rem nos nunc perquam haesitabundos in hoc deinceps famae pelagus impellis.
porro autem super huiusmodi opusculo tutius conticueramus, contenti versuum felicius quam peritius editorum opinione, de qua mihi iampridem in portu iudicii publici post lividorum latratuum Scyllas enavigatas sufficientis gloriae ancora sedet. sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur.
```

Note that here the original letter had a final `vale.` which has been removed by the filter.

By applying also sentence splitting, the result is:

```txt
.author=Sidonius Apollinaris
.date=v AD
.date-value=450
.title=1_001_001 Sidonius Constantio suo salutem.
Diu praecipis, domine maior, summa suadendi auctoritate, sicuti es in his quae deliberabuntur consiliosissimus, ut, si quae litterae paulo politiores varia occasione fluxerunt, prout eas causa persona tempus elicuit, omnes retractatis exemplaribus enucleatisque uno volumine includam, Quinti Symmachi rotunditatem, Gai Plinii disciplinam maturitatemque vestigiis praesumptiosis insecuturus.
nam de Marco Tullio silere melius puto, quem in stilo epistulari nec Iulius Titianus sub nominibus illustrium feminarum digna similitudine expressit.
propter quod illum ceteri quique Frontonianorum utpote consectaneum aemulati, cur veternosum dicendi genus imitaretur, oratorum simiam nuncupaverunt.
quibus omnibus ego immane dictu est quantum semper iudicio meo cesserim quantumque servandam singulis pronuntiaverim temporum suorum meritorumque praerogativam.
sed scilicet tibi parui tuaeque examinationi has <litterulas> non recensendas (hoc enim parum est) sed defaecandas, ut aiunt, limandasque commisi, sciens te immodicum esse fautorem non studiorum modo verum etiam studiosorum.
quam ob rem nos nunc perquam haesitabundos in hoc deinceps famae pelagus impellis.
porro autem super huiusmodi opusculo tutius conticueramus, contenti versuum felicius quam peritius editorum opinione, de qua mihi iampridem in portu iudicii publici post lividorum latratuum Scyllas enavigatas sufficientis gloriae ancora sedet.
sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur.
```

Now every line corresponds to a single sentence.

So, in the end we have exported a set of plain text files prepared with some metadata and preprocessing so that they can be easily ingested by the target analysis system without further processing. This will ["macronize"](https://github.com/Myrmex/alatius-macronizer-api) the text, and then proceed further with its prosodical and rhythmic analysis.
