---
title: "Rendering Configuration" 
layout: default
parent: Rendition
nav_order: 2
---

# Rendering Configuration

>See also: [configuration samples](config-samples).

The Cadmus rendering system is configured via an external JSON document, consumed by a factory (`CadmusRenderingFactory`), having these sections, all modeled as arrays of objects:

- `ItemIdCollector`: a single item ID collector to use when required. It has the component ID, and eventual options. Item ID collectors are used to collect the IDs of all the items to be processed, in the desired order.
- `ContextSuppliers`: list of renderer context suppliers, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, used in the scope of the configuration to reference each filter from other sections.
- `TextTreeFilters`: list of text tree filters, each named with a key, and having its component ID and eventual options.
- `RendererFilters`: list of renderer filters, each named with a key, and having its component ID and eventual options.
- `JsonRenderers`: list of JSON renderers, each named with a key, and having its component ID and eventual options. The key corresponds to the part type ID, optionally followed by `|` and its role ID in the case of a layer part. This allows mapping each part type to a specific renderer ID. This key is used in the scope of the configuration to reference each filter from/ other sections. Among its `Options`, a renderer can have a `FilterKeys` property which is an array of filter keys, representing the filters used by that renderer, to be applied in the specified order.
- `TextPartFlatteners`: list of text part flatteners, each named with a key, and having its component ID and eventual options. These are in charge of flattening the text of a base text part, which is a stage in building its block-based representation.
- `TextTreeRenderers`: list of text tree renderers, each named with a key, and having its component ID and eventual options.
- `ItemComposers`: list of item composers, each named with a key, and having its component ID and eventual options. The key is an arbitrary string, not used elsewhere in the context of the configuration. It is used as an argument for UI which process data export. Each composer can have these properties among its options:
  - `ContextSupplierKeys`: keys referring to context suppliers (as defined in `ContextSuppliers`).
  - `TextPartFlattenerKey`: key referring to the text part flattener (as defined in `TextPartFlatteners`).
  - `TextTreeFilterKeys`: keys referring to text tree filters (as defined in `TextTreeFilters`).
  - `TextTreeRendererKey`: key referring to the text tree renderer (as defined in `TextTreeRenderers`).
  - `JsonRendererKeys` array, referencing the corresponding JSON renderers by their key.

Each array in the configuration contains any number of JSON _objects_, all having:

- an `Id` property.
- an optional `Options` object to configure the component.
- a `Keys` property is used to include the key(s) of the object type being processed. This is a string property, but several keys could be added by separating them with space. For the purposes of most components anyway there is no need for more than a single key.

As a sample, consider this configuration:

- 3 renderer filters are defined with their keys: `thes-filter`, `rep-filter`, `md-filter`.
- 3 JSON renderers are defined for 3 different part types (a base text part, and two layer parts). The JSON renderer here is just a "null" renderer which passes back the received JSON, for diagnostic purposes; but any other renderer can be used and configured via its `Options` property.
- 1 text part flattener is defined for the token-based text part type.
- 1 text block renderer is defined to produce simple TEI from a layered text.
- 1 item composer is defined for a file-based TEI stand-off output, using a specific text part flattener and text block renderer, plus a number of JSON renderers, one for each layer part type.
- 1 item ID collector is defined, directly accessing a MongoDB database.

```json
{
  "ItemIdCollector": {
    "Id": "it.vedph.item-id-collector.mongo",
    "Options": {
      "FacetId": "text"
    }
  },
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
  "TextTreeRenderers": [
    {
      "Keys": "tei-standoff",
      "Id": "it.vedph.text-tree-renderer.tei-off-linear"
    }
  ],
  "ItemComposers": [
    {
      "Keys": "text-item",
      "Id": "it.vedph.item-composer.tei-standoff.fs",
      "Options": {
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextTreeRendererKey": "tei-standoff",
        "JsonRendererKeys": [
          "it.vedph.token-text-layer|fr.it.vedph.comment",
          "it.vedph.token-text-layer|fr.it.vedph.orthography"
        ]
      }
    }
  ]
}
```

Explanation:

- the item ID collector is used to collect the IDs of the items to render. In this case we are filtering items to select only those with a facet ID equal to `text`.
- rendering filters:
  - `thes-filter` is used to map thesauri IDs to their values.
  - `rep-filter` is used to replace some text. Here we just have a mock replacement to show how it works.
  - `md-filter` is used to render Markdown into some other format; in this case, the target format is plain text.
- JSON renderers are defined for:
  - the token-based text part;
  - the comment layer part;
  - the orthography layer part.
- text part flatteners: a single flattener used for token-based text parts.
- text tree renderers: a single renderer used to generate TEI segmented text for standoff notation.
- item composers: a single composer orchestrating all the components in rendering each item and saving its output into several files. Its options define:
  - the text part flattener to use (for token-based text).
  - the text tree renderer to use (the base text for standoff renderer).
  - the JSON renderers to use (for comment and orthography layers).
