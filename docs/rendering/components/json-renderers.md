---
title: "JSON Renderers" 
layout: default
parent: Rendition
nav_order: 3
---

# JSON Renderers

- [JSON Renderers](#json-renderers)
  - [Null Json Renderer](#null-json-renderer)
  - [XSLT Json Renderer](#xslt-json-renderer)

>💡 Under its `Options`, any renderer can have a `FilterKeys` property which is an array of filter keys, representing the filters used by that renderer, to be applied in the specified order. These filters are defined in the `RendererFilters` section of the [configuration](../config).

## Null Json Renderer

👉 A pass-through filter, which just returns the received JSON. It can be used for diagnostic purposes, or to apply some filters to the received text.

- ID: `it.vedph.json-renderer.null`

## XSLT Json Renderer

👉 XSLT-based JSON renderer. This can transform JSON, convert it into XML, transform XML with XSLT, and apply filters to the result.

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
