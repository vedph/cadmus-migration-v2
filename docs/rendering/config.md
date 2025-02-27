---
title: "Rendering Configuration" 
layout: default
parent: Rendition
nav_order: 2
---

# Rendering Configuration

>See also: [configuration samples](config-samples).

The Cadmus previewer relies on a JSON configuration consumed by a factory (`CadmusPreviewerFactory`), having these sections, all modeled as arrays of objects:

- `RendererFilters`: list of renderer filters, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, used in the scope of the configuration to reference each filter from other sections.
- `JsonRenderers`: list of JSON renderers, each named with a key, and having its component ID and eventual options. The key corresponds to the part type ID, optionally followed by `|` and its role ID in the case of a layer part. This allows mapping each part type to a specific renderer ID. This key is used in the scope of the configuration to reference each filter from/ other sections. Under options, any renderer can have a `FilterKeys` property which is an array of filter keys, representing the filters used by that renderer, to be applied in the specified order.
- `TextPartFlatteners`: list of text part flatteners, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, used in the scope of the configuration to reference each filter from other sections These are in charge of flattening the text of a base text part, which is a stage in building its block-based representation.
- `TextBlockRenderers`: List of text block renderers, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, used in the scope of the configuration to reference each filter from other sections. These text block renderers are used to produce some text output starting from text blocks, typically when exporting a layered text into some other format, e.g. XML TEI.
- `ItemComposers`: list of item composers, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, not used elsewhere in the context of the configuration. It is used as an argument for UI which process data export. Each composer can have among its options a `TextPartFlattenerKey` and a `TextBlockRendererKey`, referencing the corresponding components by their key, and a `JsonRendererKeys` array, referencing the corresponding JSON renderers by their key. Item composers are used to produce any type of output starting from an item with its parts.
- `ItemIdCollector`: a single item ID collector to use when required. It has the component ID, and eventual options. Item ID collectors are used to collect the IDs of all the items to be processed, in the desired order.

Each array in the configuration contains any number of JSON _objects_ having:

- an `Id` property.
- an optional `Options` object to configure the component. All the `JsonRenderers` can have a `FilterKeys` array property, specifying the filters to apply after its rendition. Each entry in the array is one of the filters keys as defined in the `RendererFilters` section.
- a `Keys` property is used to include the key(s) of the object type being processed. This is a string property, but several keys could be added by separating them with space. For the purposes of preview components anyway there is no need for more than a single key.

As a sample, consider this configuration:

- 3 renderer filters are defined with their keys: `thes-filter`, `rep-filter`, `md-filter`.
- 3 JSON renderers are defined for 3 different part types (a base text part, and two layer parts). The JSON renderer here is just a "null" renderer which passes back the received JSON, for diagnostic purposes; but any other renderer can be used and configured via its `Options` property.
- 1 text part flattener is defined for the token-based text part type.
- 1 text block renderer is defined to produce simple TEI from a layered text.
- 1 item composer is defined for a file-based TEI stand-off output, using a specific text part flattener and text block renderer, plus a number of JSON renderers, one for each layer part type.
- 1 item ID collector is defined, directly accessing a MongoDB database.

```json
{
  "RendererFilters": [
    {
      "Keys": "thes-filter",
      "Id": "it.vedph.renderer-filter.mongo-thesaurus"
    },
    {
      "Keys": "rep-filter",
      "Id": "it.vedph.renderer-filter.replace",
      "Options": {
        "Replacements": [
          {
            "Source": "hello",
            "Target": "HELLO"
          }
        ]
      }
    },
    {
      "Keys": "md-filter",
      "Id": "it.vedph.renderer-filter.markdown",
      "Options": {
        "MarkdownOpen": "<_md>",
        "MarkdownClose": "</_md>",
        "Format": "txt"
      }
    }
  ],
  "JsonRenderers": [
    {
      "Keys": "it.vedph.token-text",
      "Id": "it.vedph.json-renderer.null",
      "Options": {
        "FilterKeys": [ "thes-filter", "rep-filter", "md-filter" ]
      }
    },
    {
      "Keys": "it.vedph.token-text-layer|fr.it.vedph.comment",
      "Id": "it.vedph.json-renderer.null"
    },
    {
      "Keys": "it.vedph.token-text-layer|fr.it.vedph.orthography",
      "Id": "it.vedph.json-renderer.null"
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
      "Keys": "tei-standoff",
      "Id": "it.vedph.text-block-renderer.tei-standoff",
      "Options": {
        "RowOpen": "<div xml:id=\"r{y}\">",
        "RowClose": "</div>",
        "BlockOpen": "<seg xml:id=\"{b}\">",
        "BlockClose": "</seg>"
      }
    }
  ],
  "ItemComposers": [
    {
      "Keys": "text-item",
      "Id": "it.vedph.item-composer.tei-standoff.fs",
      "Options": {
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextBlockRendererKey": "tei-standoff",
        "JsonRendererKeys": [
          "it.vedph.token-text-layer|fr.it.vedph.comment",
          "it.vedph.token-text-layer|fr.it.vedph.orthography"
        ]
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
