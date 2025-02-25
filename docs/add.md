# Addenda

The rendering process is designed to be modular and fully customizable, so that we will be able to reuse software components to fit different encoding strategies. So, even for the simplest cases a full-blown customizable rendering pipeline is used. For trivial renderings this might seem as an overkill, but its purpose is right to make the system capable of any type of output, from the most simple to the most complex, using a unified modular approach.

## Overview

The general flow for text rendition is:

1. open the output via the item composer.
2. use an item collector to get the ID of one item at a time from a data source, applying the required filters.
3. for each ID, get the item and use the item composer to:
   1. get the text part and collect the desired layers parts from the received item.
   2. flatten layers (via an instance of `ITextPartFlattener`) and merge the resulting text ranges.
   3. build a linear tree from these ranges.
   4. apply optional tree filters (`ITextTreeFilter`).
   5. render the tree into a string (`ITextTreeRenderer`) and variously use the result to build a specific output.

The factory used to configure the process is based on a JSON configuration file whose template is like this:

```json
{
  "ItemIdCollector": {
    "Id": "...",
    "Options": {}
  },
  "TextTreeFilters": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {}
    }
  ],
  "RendererFilters": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {}
    }
  ],
  "JsonRenderers": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {}
    }
  ],
  "TextPartFlatteners": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {}
    }
  ],
  "TextTreeRenderers": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {
        "FilterKeys": ["..."]
      }
    }
  ],
  "ItemComposers": [
    {
      "Keys": ["..."],
      "Id": "...",
      "Options": {
        "TextPartFlattenerKey": "...",
        "TextTreeFilterKeys": ["..."],
        "TextTreeRendererKey": "...",
        "JsonRendererKeys": ["..."]
      }
    }
  ]
}
```

As an example, consider this configuration, targeting a [project](https://github.com/vedph/cadmus-sidon-app) using just a single apparatus layer to be rendered into TEI with embedded `app` elements:

```json
{
  "ItemIdCollector": {
    "Id": "it.vedph.item-id-collector.mongo",
    "Options": {
      "FacetId": "text"
    }
  },
  "ContextSuppliers": [
    {
      "Keys": "flags",
      "Id": "renderer-context-supplier.flag",
      "Options": {
        "On": {
          "8": "block-type=poetry"
        },
        "Off": {
          "8": "block-type=prose"
        }
      }
    }
  ],
  "TextTreeFilters": [
    {
      "Keys": "block-linear",
      "Id": "text-tree-filter.block-linear"
    },
    {
      "Keys": "app-linear",
      "Id": "text-tree-filter.apparatus-linear"
    }
  ],
  "RendererFilters": [
    {
      "Keys": "nl-appender",
      "Id": "it.vedph.renderer-filter.appender",
      "Options": {
        "Text": "\r\n"
      }
    },
    {
      "Keys": "ns-remover",
      "Id": "it.vedph.renderer-filter.replace",
      "Options": {
        "Replacements": [
          {
            "Source": " xmlns=\"http://www.tei-c.org/ns/1.0\"",
            "Target": "",
            "Repetitions": 1
          }
        ]
      }
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
      "Keys": "tei",
      "Id": "text-tree-renderer.tei-app-linear",
      "Options": {
        "FilterKeys": ["nl-appender", "ns-remover"],
        "ZeroVariantType": "omissio",
        "BlockElements": {
          "default": "tei:p",
          "poetry": "tei:l",
          "prose": "tei:p"
        }
      }
    }
  ],
  "ItemComposers": [
    {
      "Keys": "default",
      "Id": "it.vedph.item-composer.tei.fs",
      "Options": {
        "ContextSupplierKeys": ["flags"],
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextTreeFilterKeys": ["block-linear", "app-linear"],
        "TextTreeRendererKey": "tei",
        "TextHead": "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">\n  <teiHeader>\n    <fileDesc>\n      <titleStmt>\n        <title>Sidonius</title>\n      </titleStmt>\n      <publicationStmt>\n        <p>Not published.</p>\n      </publicationStmt>\n      <sourceDesc>\n        <p>Undisclosed.</p>\n      </sourceDesc>\n    </fileDesc>\n  </teiHeader>\n  <text>\n    <body>\n      <div>\n",
        "TextTail": "      </div>\n    </body>\n  </text>\n</TEI>",
        "OutputDirectory": "c:\\users\\dfusi\\Desktop\\sidon"
      }
    }
  ]
}
```

In this configuration, from top to bottom (the order of sections is free):

- the **item ID collector** is the component used to collect the identifiers of all the items which will be rendered, in their order. This applies some filters to get only text items.
- the **context suppliers** contains a renderer context supplier component, which updates the current block type to poetry or prose according to the item's flag. When the flag 8 is on, it is poetry; else it's prose. This block type parameter is then used by another component, the tree renderer, to decide whether to output TEI `p` or `l` as blocks.
- two **text tree filters**, applied after the text has been transformed into a tree:
  - one named `block-linear` is used to split nodes at every occurrence of a newline character. This ensures that TEI block elements will be rendered correctly.
  - another named `app-linear` is used inject apparatus metadata into tree nodes representing the text during the rendering process.
- there are 2 **renderer filters** applied after TEI rendering has completed:
  - the one named `nl-appender` just appends a newline to each rendered item, so the text is more readable.
  - the one named `ns-remover` removes the redundant namespace attribute from the rendered blocks. When rendering a TEI fragment with namespaces, like in TEI, its top level element(s) always get the default namespace; otherwise, an incorrect XML fragment would be emitted. As we are going to provide the default TEI namespace once at the document's root, these namespaces can be removed, because they would be redundant.
- the **text part flattener** named `it.vedph.token-text` is used to flatten the layers on a text. In this case it will just use the apparatus layer, which is the only one. Once text is flattened, it will be converted into an at least initially linear tree, where each portion of text is represented by a node, and is child of the previous portion.
- the **text tree renderer** with name `tei` is used to render the tree into TEI according to the desired format. In this case we use a renderer designed to handle a linear tree with apparatus, and we add a couple of filters after it completes (`nl-appender` to add a newline, `ns-remover` to remove redundant namespace attributes). Also, we specify that in case the variant is zero (=omission), there should be a `@type` attribute on the `rdg` element with value equal to `omissio`.
- the **item composer**, here named `default`, is the component orchestrating the rendering process. It uses a component designed to render TEI with inline apparatus elements. Besides its components (flattener, tree filters, tree renderer) it has some parameters which define:
  - the portion of markup to prepend to the generated document (`TextHead`). This is a simple markup including the opening root tag and a TEI header skeleton.
  - the portion of markup to append to the generated document (`TextTail`), which closes the tags opened in the head.
  - the output directory for saving the generated document(s).

## Building Trees

An item is rendered via an item composer, which implements the `IItemComposer` interface. Text items are the most complex for rendering, so let us consider them first.

Let us start from a 2-lines token-based text like this:

```txt
que bixit
annos XX
```

Let us say that there are the following layer fragments:

- orthography fragment 1 on `qu[e]` (`1.1@3`).
- orthography fragment 2 on `[b]ixit` (`1.2@1`).
- paleography fragment 1 on `qu[e b]ixit` (a ligature: `1.1@3-1.2@1`).
- comment fragment 1 on `bixit annos` (`1.2-2.1`).

In this example we want to render all these layers, but you are free to select only the ones you want.

▶️ (1) **flatten layers**: use a text part flattener (`ITextPartFlattener`) to get the whole text into a multiline string, plus one range for each fragment in each of the picked layer parts.

- text: `que bixit|annos XX` (where `|` stands for a LF character, used as the line delimiter).
- ranges:
  1. 2-2 for `qu[e]`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.orthography@0`;
  2. 4-4 for `[b]ixit`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.orthography@1`;
  3. 2-4 for `qu[e b]ixit`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.apparatus@0`;
  4. 4-14 for `bixit|annos`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.comment@0`.

Each of the ranges has a model including:

- the start and end indexes referred to the whole text as output by the same function.
- the global ID of the corresponding fragment(s). After flattening, each range has just a single fragment ID, because by definition one fragment produces one range. Later, when ranges are merged, they may carry more than a single fragment ID. Each fragment ID is built by concatenating the part type ID, followed by `:` and its role ID (which is always defined for a layer part), followed by `_` and the index of the fragment in its layer part.
- the text corresponding to the range. This is assigned after flattening and merging, for performance reasons (it would be pointless to assign text to all the ranges when many of them are going to be merged into new ones).

At this stage we have a string with the text, and a bunch of freely overlapping ranges referring to it. The next step is merging these ranges into a single linear, contiguous sequence.

▶️ (2) **merge ranges** (via `FragmentTextRange.MergeRanges`) into a set of non-overlapping and contiguous ranges, covering the whole text from start to end. So, starting from this state, where each line below the text represents a range with its fragment ID:

```txt
012345678901234567
que bixit|annos XX
..O............... fr1
....O............. fr2
..PPP............. fr3
....CCCCCCCCCCC... fr4
```

we get these ranges:

1. 0-1 for `qu` = no fragments;
2. 2-2 for `e` = fr1, fr3;
3. 3-3 for space = fr3;
4. 4-4 for `b` = fr2, fr3, fr4;
5. 5-14 for `ixit|annos` = fr4;
6. 15-17 for space + `XX` = no fragments.

```txt
012345678901234567
que bixit|annos XX
112345555555555666
```

▶️ (3) **assign text values** to each merged range. This is trivial as it just means getting substrings from the whole text, as delimited by each range.

▶️ (4) **build a text tree**: this tree is built from a blank root node, having in a single branch descendant nodes corresponding to the merged ranges. The first range is child of the blank root node, and each following range is child of the previous one.

Each node has _payload_ data with this model:

- range: the source merged range with its fragment ID(s).
- type: an optional string representing a node type when required.
- before EOL: true if node is appeared before a line end marker (LF) in the original text.
- text: the text corresponding to this node. Initially this is equal to the source range's text, but it might be changed by filters.
- features: a set of generic name=value pairs, where both are strings, plus a source identifier (equal to or derived from the fragment ID). Duplicate names are allowed and represent arrays. Initially these are empty, but they are going to be used later.

So the tree is:

```mermaid
graph LR;

root --> 1[qu]
1 --> 2[e]
2 --> 3[_]
3 --> 4[b]
4 --> 5[ixit/annos]
5 --> 6[_XX]
```

>The tree structure may seem an overcomplication when dealing with a single linear branch, but it is really useful when rendering more complex data. For instance, we might be able to transform a linear tree into a binary branching tree, and adopt a parallel segmentation strategy. See below for more.

Note that here a node contains text with a LF character, which is used to mark the end of the original line. Typically this is adjusted in the next step so that such nodes are split.

▶️ (5) **apply text tree filters**: optionally, apply filters to the tree nodes. Each of the filters takes the input of the previous one and generates a new tree. Almost always you will be using the _block linear tree text filter_, which splits nodes wherever they include newlines. This ensures that each node has at most 1 newline, and that it appears at the end of its text. This is required to ensure that text blocks will be correctly rendered. The result is:

```mermaid
graph LR;

root --> 1[qu]
1 --> 2[e]
2 --> 3[_]
3 --> 4[b]
4 --> 5[ixit]
5 --> 6[annos]
6 --> 7[_XX]
```

▶️ (6) **render the text tree** (via an `ITextTreeRenderer`). A text tree renderer traverses the tree and renders it into some specific format.

### Critical apparatus

When an apparatus is involved, this can potentially modify the text by selecting specific variants or normalized forms. In this case, special filters can be applied to modify the text and features of nodes before further processing.

The approach depends on the complexity of the source data. Let us consider various scenarios, from the simplest to the most complex ones.

The standard [apparatus](https://github.com/vedph/cadmus-philology/blob/master/docs/fr.apparatus.md) fragment model is (I add the feature name after each property converted to a feature during processing):

- location
- tag (`app-tag`)
- entries:
  - subrange
  - tag (`app.e.tag`)
  - value  (`app.e.variant`, when type is not note)
  - normValue
  - isAccepted
  - groupId
  - witnesses:
    - value (`app.e.witness`)
    - note (`app.e.witness.note`)
  - authors:
    - tag
    - value (`app.e.author`)
    - location (`app.e.author.loc`)
    - note (`app.e.author.note`)
  - note (`app.e.note`)

#### Linear Single Layer

In this approach we just have a _single_ layer with _apparatus_. So, merging just projects the apparatus ranges on the whole text; the text is segmented only according to the apparatus fragments.

>Given that we deal with a single layer, we can be sure there is no overlap: this is a constraint imposed to the Cadmus text layers model. This constraint, somewhat artificial for the Cadmus model itself, was designed for compatibility reasons, to make it simpler to deal with third-party models in exports or visualizations.

For instance, say we have this simple text:

```txt
012345678901234567890123456789012
illuc unde negant redire quemquam
AAAAA....................BBBBBBBB
```

with 2 fragments in the apparatus, one (A) with 3 entries, and another (B) with 2:

- A0: note: witnesses=`O1`, accepted.
- A1: replacement: value=`illud`, witnesses=`O G R`.
- A2: replacement: value=`illic`, authors=`Fruterius` with note=`(†1566) 1605a 388`.
- B0: note: witnesses=`O G`, accepted.
- B1: replacement: value=`umquam`, witnesses=`R`, note=`some note`.

The merged ranges would be:

1. 0-4 for `illuc`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.apparatus@0`;
2. 25-32 for `quemquam`: fragment ID=`it.vedph.token-text-layer:fr.it.vedph.apparatus@1`.

```mermaid
graph LR;

classDef em stroke:yellow,stroke-width:2px;

root --> 1[illuc]
class 1 em;
1 --> 2[-unde-negant-redire-]
2 --> 3[quemquam]
class 3 em;
```

>In this diagram, yellow borders mark nodes linked to apparatus fragments and dashes represent spaces.

Given that we have a single layer, we won't need to add or delete nodes, but just to change their payload data adding an **apparatus linear tree text filter** to ▶️ step (5). So, traversing our nodes this layer generates features for nodes linked to apparatus fragments:

1. `illuc` is linked to fragment 0:
    - from entry 0 (source `it.vedph.token-text-layer:fr.it.vedph.apparatus@0.0`):
      - `app.e.witness`=`O1`
    - from entry 1 (source `it.vedph.token-text-layer:fr.it.vedph.apparatus@0.1`):
      - `app.e.variant`=`illud`
      - `app.e.witness`=`O`
      - `app.e.witness`=`G`
      - `app.e.witness`=`R`
    - from entry 2 (source `it.vedph.token-text-layer:fr.it.vedph.apparatus@0.2`):
      - `app.e.variant`=`illic`
      - `app.e.author`=`Fruterius`
      - `app.e.author.note`=`(†1566) 1605a 388`
2. `quemquam` is linked to fragment 1:
    - from entry 0 (source `it.vedph.token-text-layer:fr.it.vedph.apparatus@1.0`):
      - `app.e.witness`=`O`
      - `app.e.witness`=`G`
    - from entry 1 (source `it.vedph.token-text-layer:fr.it.vedph.apparatus@1.1`):
      - `app.e.variant`=`umquam`
      - `app.e.witness`=`R`
      - `app.e.note`=`some note`

At this stage, we're done with the tree and we can move to ▶️ step (6) for rendering it. Rendition depends on the desired output format; for this example, let's keep things simple and say that we want a TEI text fragment like this (witnesses and other attributes are fake data assumed to be in the fragments, and text is indented for more readability):

```xml
<p>
    <app>
      <lem wit="#O1">illuc</lem>
      <rdg wit="#O #G #R">illud</rdg>
      <rdg id="seg1" resp="#Fruterius">illic</rdg>
      <witDetail target="#seg1" resp="#Fruterius">(†1566) 1605a 388</witDetail>
    </app>
    unde negant redire
    <app>
      <lem wit="#O #G">quemquam</lem>
      <rdg wit="#R">
        umquam
        <note>some note</note>
      </rdg>
    </app>
</p>
```

We can easily build this TEI code by just traversing our tree:

1. at root, start with a block element (`p` in this case);
2. `illuc`: as the node is linked to a fragment, add an `app` element and inside it add a `lem` element with the node's text as text, and as many `rdg` elements as variants with the variant value as text;
3. `unde negant redire` (surrounded by spaces) is not linked to fragments, so just output it as text;
4. `quemquam`: linked to fragment, so process as for 2 above;
5. close the block.

So the rules for this simple renderer would be:

- use a specific element for blocks (e.g. `p` for prose, `l` for verses):
  - open a block at root;
  - close and reopen the block after each node before a newline;
  - close the block at end.

- if the node has apparatus feature(s):
  - add an `app` element with content:
    - `lem` = node text with `@wit` for witnesses, `@resp` for authors, a child `note` for note. Also, for each witness/author having its own note, add a `witDetail` sibling with `@target` pointing to the witness/author element, `@wit` or `@resp` with the value of the author/witness, and content=note's value.
    - `rdg` = variant text, with attributes and children as above.
- else just output the node's text.
