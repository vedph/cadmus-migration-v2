---
title: "Renderers" 
layout: default
parent: Rendition
nav_order: 3
---

# Renderers

- [Renderers](#renderers)
  - [JSON Renderers](#json-renderers)
    - [Filters](#filters)
    - [Null Json Renderer](#null-json-renderer)
    - [XSLT Json Renderer](#xslt-json-renderer)
    - [TEI Standoff Apparatus Json Renderer](#tei-standoff-apparatus-json-renderer)

## JSON Renderers

Here you can find a list of builting JSON renderers.

### Filters

Under its `Options`, any renderer can have a `FilterKeys` property which is an array of filter keys, representing the filters used by that renderer, to be applied in the specified order.

These filters are specified in the `Filters` section of the [configuration](config).

### Null Json Renderer

- ID: `it.vedph.json-renderer.null`

Null JSON renderer. This is a pass-through filter, which just returns the received JSON. It can be used for diagnostic purposes, or to apply some filters to the received text.

### XSLT Json Renderer

XSLT-based JSON renderer. This can transform JSON, convert it into XML, transform XML with XSLT, and apply filters to the result.

- ID: `it.vedph.json-renderer.xslt`
- options:
  - `FrDecoration`: a boolean value indicating whether fragment decoration is enabled. When true, the JSON corresponding to a layer part gets an additional `_key` property in each of its `fragments` array items. This key is built from the layer type ID optionally followed by `|` plus the role ID, followed by the fragment's index.
  - `JsonExpressions`: the optional array of JSON transform expressions using JMES Path.
  - `QuoteStripping`: a boolean value indicating whether quotes wrapping a string result should be removed once JSON transforms have completed.
  - `Xslt`: the XSLT script.
  - `WrappedEntryNames`: the names of the XML elements representing entries derived from the conversion of a JSON array. When converting JSON into XML, any JSON array is converted into a list of entry elements. So, from a `guys` array with 3 entries you get 3 elements named `guys`. If you want to wrap these elements into an array parent element, set the name of the entries element as the key of this dictionary, and the name of the single entry element as the value of this dictionary (e.g. key=`guys`, value=`guy`, essentially plural=singular). If you need to set a namespace, add its prefix before colon, like `tei:div`. These prefixes are optionally defined in `Namespaces`.
  - `Namespaces`: an optional list of namespace prefixes and values in the form `prefix=namespace`, like e.g.`tei=http://www.tei-c.org/ns/1.0`.
  - `DefaultNsPrefix`: the prefix to use for the default namespace. When a document has a default namespace, and thus an empty prefix, we may still require a prefix, e.g. to query it via XPath. So if you are going to use XPath and you have a document with default namespace, set its prefix via this option.

The entries wrapper can be used when wrapping entries into a parent element is easier for your XSLT. Consider for instance the `entries` JSON array in these layer fragments (other properties of the layer part have been omitted for brevity):

```json
{
  "fragments": [
    {
      "lemma": "pulsosque",
      "location": "1.2",
      "tag": "marginal",
      "entries": [
        {
          "type": 0,
          "subrange": null,
          "tag": null,
          "value": "pulsisque",
          "normValue": "pulsisque",
          "isAccepted": true,
          "groupId": null,
          "witnesses": [
            {
              "value": "B",
              "note": "manus altera"
            },
            {
              "value": "O"
            }
          ],
          "authors": [
            {
              "value": "Cursius",
              "note": "(dubitanter)"
            }
          ]
        },
        {
          "type": 0,
          "value": "",
          "witnesses": [
            {
              "value": "Q"
            },
            {
              "value": "Z"
            }
          ]
        },
        {
          "type": 0,
          "value": "pulsosque",
          "normValue": "pulsosque",
          "witnesses": [
            {
              "value": "P"
            },
            {
              "value": "r"
            }
          ],
          "authors": [
            {
              "value": "Serv. ad Aen. 1,2",
              "note": "(de ablativo casu)"
            }
          ]
        },
        {
          "type": 3,
          "note": "non liquet an lacuna postulanda sit."
        }
      ]
    }
  ]
}
```

The resulting XML has an `entries` element for each entry, without a wrapper parent element:

```xml
<root>
    <fragments>
        <lemma>pulsosque</lemma>
        <location>1.2</location>
        <tag>marginal</tag>
        <entries>
            <type>0</type>
            <subrange />
            <tag />
            <value>pulsisque</value>
            <normValue>pulsisque</normValue>
            <isAccepted>true</isAccepted>
            <groupId />
            <witnesses>
                <value>B</value>
                <note>manus altera</note>
            </witnesses>
            <witnesses>
                <value>O</value>
            </witnesses>
            <authors>
                <value>Cursius</value>
                <note>(dubitanter)</note>
            </authors>
        </entries>
        <entries>
            <type>0</type>
            <value></value>
            <witnesses>
                <value>Q</value>
            </witnesses>
            <witnesses>
                <value>Z</value>
            </witnesses>
        </entries>
        <entries>
            <type>0</type>
            <value>pulsosque</value>
            <normValue>pulsosque</normValue>
            <witnesses>
                <value>P</value>
            </witnesses>
            <witnesses>
                <value>r</value>
            </witnesses>
            <authors>
                <value>Serv. ad Aen. 1,2</value>
                <note>(de ablativo casu)</note>
            </authors>
        </entries>
        <entries>
            <type>3</type>
            <note>non liquet an lacuna postulanda sit.</note>
        </entries>
    </fragments>
</root>
```

This can be fine if your XSLT just matches `entries`, as it usually is the case with apparatus; but in other cases, having a wrapper element could be useful. For instance, imagine a list of categories, where you want to wrap the list inside a HTML unordered list. In this case you would need to match the parent to open and close the unordered list (`ul` element); and then each child entry to generate the list item (`li`) elements. In this case, having a parent wrapper makes things easier.

So, if you use a configuration like this for apparatus entries:

```json
{
    "WrappedEntryNames": {
        "entries": "entry"
    }
}
```

you will rather get a structure like this:

```xml
<entries>
    <entry>...</entry>
    <entry>...</entry>
    <entry>...</entry>
</entries>
```

### TEI Standoff Apparatus Json Renderer

- ID: `it.vedph.json-renderer.tei-standoff.apparatus`
- options:
  - `ZeroVariant`: the text to output for a zero variant. A zero variant is a deletion, represented as a text variant with an empty value. When building an output, you might want to add some conventional text for it, e.g. `del.` (_delevit_), which is the default value.
  - `NotePrefix`: the note prefix. This is an optional string to be prefixed to the note text after a non-empty value in a `rdg` or `note` element value. Any apparatus entry can have a value, and an optional note; when it's a variant mostly it has a value, and optionally a note; when it's a note, mostly it has only the note without value. The default value is space.

As a sample, consider this configuration, used to export a Cadmus text + apparatus into standoff TEI XML:

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
      "Id": "it.vedph.json-renderer.tei-standoff.apparatus",
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

Here you have:

- a filter which just appends a newline (named `nl-appender`).
- a text part flattener for token-based text.
- a text block renderer for standoff TEI, named `tei`. This uses the `nl-appender` filter just to emit a newline after each `div`, to make the resulting output more readable.
- the apparatus standoff TEI JSON renderer. Note that its key is equal to the part type followed by `|` and the fragment type. This is required for Cadmus to match the renderer with the part. The `Id` instead is the ID of the software component. Should you want to render additional layers, just add more JSON renderers, one for each layer type.
- an item composer for standoff TEI. This uses the specified text flattener and block renderer, and the JSON renderer for the apparatus; it outputs its XML files into the specified output directory.
- an [item ID collector](collectors) which collects the IDs of all the items to be exported from a Cadmus MongoDB database. This filters the items to get only those with facet=`text`.
