---
title: "Rendition Filters" 
layout: default
parent: Rendition
nav_order: 4
---

# Rendition Filters

- [Rendition Filters](#rendition-filters)
  - [Appender Filter](#appender-filter)
  - [Fragment Link Filter](#fragment-link-filter)
  - [ISO 639 Lookup Filter](#iso-639-lookup-filter)
  - [Markdown Conversion Filter](#markdown-conversion-filter)
  - [Text Replacements Filter](#text-replacements-filter)
  - [Thesaurus Lookup Filter](#thesaurus-lookup-filter)
  - [Token-based Text Extractor Filter](#token-based-text-extractor-filter)
  - [Sentence Split Filter](#sentence-split-filter)

Cadmus provides some builtin filters which can be used by JSON renderers or text block renderers; as for any other component type, you are free to add your own filters.

The filters to be used are typically specified in a JSON-based [configuration](config), where each filter type has its own ID. You can concatenate as many filters as you want, even when they are of the same type. All the filters will be applied in the order they are defined. Here I list the builtin filters.

## Appender Filter

Append the specified text to the input.

- ID: `it.vedph.renderer-filter.appender`
- options:
  - `Text`: the text to append.

## Fragment Link Filter

Map layer keys into target IDs, leveraging the metadata built by the text renderer run before this filter. This is used to link fragments renditions to their base text (see about [building TEI](markup) for more).

- ID: `it.vedph.renderer-filter.fr-link`
- options:
  - `TagOpen`: the opening tag for fragment key.
  - `TagClose`: the closing tag for fragment key.

## ISO 639 Lookup Filter

Language codes ISO639-3 or ISO639-2 filter. This is a simple lookup filter replacing these ISO codes with the corresponding English language names, e.g. `eng` with `English`. Often, when dealing with such codes they rather belong to a thesaurus (e.g. the list of languages used in a manuscript); in this case, you will rather use a thesaurus filter to resolve the codes, as this ensures that you get the desired name and locale. This filter instead is used as a quick way of resolving language codes when you just deal with ISO639 without recurring to a thesaurus, nor requiring localization.

- ID: `it.vedph.renderer-filter.iso639`
- options:
  - `Pattern`: the pattern used to identify ISO codes. It is assumed that the code is the first captured group in a match. Default is `^^` followed by 3 lowercase letters for ISO 639-3. For all the matches, the filter will extract the code, lookup it, and replace the matched expression with either the result or the code itself, when it was not found.
  - `TwoLetters`: true to use 2-letters codes instead of 3-letters codes.

## Markdown Conversion Filter

Convert Markdown text into HTML or plain text.

- ID: `it.vedph.renderer-filter.markdown`
- options:
  - `MarkdownOpen`: the markdown region opening tag. When not set, it is assumed that the whole text is Markdown.
  - `MarkdownClose`: the markdown region closing tag. When not set, it is assumed that the whole text is Markdown.
  - `Format`: the Markdown regions target format: if not specified, nothing is done; if `txt`, any Markdown region is converted into plain text; if `html`, any Markdown region is converted into HTML.

## Text Replacements Filter

Perform any text replacements, either literals or based on regular expressions.

- ID: `it.vedph.renderer-filter.replace`
- options:
  - `Replacements`: an array of objects with these properties:
    - `Source`: the text or pattern to find.
    - `Target`: the replacement.
    - `Repetitions`: the max repetitions count, or 0 for no limit (=keep replacing until no more changes).
    - `IsPattern`: `true` if `Source` is a regular expression pattern rather than a literal.

>Note: unless you need to effectively repeat the replacement, always set the `Repetitions` property to 1 to optimize the performance. Otherwise, the default value being 0 (=no limit), all the replacements which need to be executed just once will be repeated a second time just to discover that no repetition is required.

## Thesaurus Lookup Filter

Lookup any thesaurus entry by its ID, replacing it with its value when found, or with the entry ID when not found.

- ID: `it.vedph.renderer-filter.mongo-thesaurus`
- options:
  - `ConnectionString`: connection string to the Mongo DB. This is usually omitted and supplied by the client code from its own application settings.
  - `Pattern`: the regular expression pattern representing a thesaurus ID to lookup: it is assumed that this expression has two named captures, `t` for the thesaurus ID, and `e` for its entry ID. The default pattern is a `$` followed by the thesaurus ID, `:`, and the entry ID.

## Token-based Text Extractor Filter

Replace all the text locations matched via a specified regular expression pattern with the corresponding text from the base text part.

- ID: `it.vedph.renderer-filter.mongo-token-extractor`
- options:
  - `ConnectionString`: connection string to the Mongo DB. This is usually omitted and supplied by the client code from its own application settings.
  - `LocationPattern`: the regular expression pattern representing a text location expression. It is assumed that the first capture group in it is the text location.
  - `WholeToken`: a value indicating whether to extract the whole token from the base text, even when the oordinates refer to a portion of it.
  - `StartMarker` (when `WholeToken` is true): the start marker to insert at the beginning of the token portion when extracting the whole token. Default is `[`.
  - `EndMarker` (when `WholeToken` is true): the end marker to insert at the end of the token portion when extracting the whole token. Default is `]`.
  - `TextCutting`: true to enable text cutting.

When text cutting is enabled, you can specify these additional options:

- `Mode:`: the operational mode for the cutter: 0=cut tail (like `ABC...`); 1=cut head (like `...ABC`); 2=cut both tail and head (like `...ABC...`); 3=cut body (like `ABC...DEF`).
- `Limit`:  the limit to the resulting text extent. This is either the maximum desired text length, or the maximum desired text extent expressed as a percentage of the total text's length. Default=100.
- `MinusLimit`: the limit by which the maximum length can be reduced. Default=10.
- `PlusLimit`: the limit by which the maximum length can be increased. Default=10.
- `LimitAsPercents`: true if limits are expressed as percentages.
- `Ellipsis`: the text to append as ellipsis indicator when text is cut. Default=`...`.
- `StopChars`: characters marking a stop, in descending order of importance. Default=`.?!;:,/ -`. These are used to locate the preferred locations for a cut.

## Sentence Split Filter

- ID: `it.vedph.renderer-filter.sentence-split`
- options:
  - `EndMarkers`: the end-of-sentence marker characters. Each character in this string is treated as a sentence end marker. Any sequence of such end marker characters is treated as a single end. Default characters are `.`, `?`, `!`, Greek question mark (U+037E), and ellipsis (U+2026).
  - `BlackOpeners`: the "black" section openers characters. Each character in this string has a corresponding closing character in `BlackClosers`, and marks the beginning of a section which may contain end markers which will not count as sentence separators. This is typically used for parentheses, e.g. "hoc tibi dico (cui enim?) ut sapias", where we do not want the sentence to stop after the question mark. These sections cannot be nested, so you are free to use the same character both as an opener and as a closer, e.g. an EM dash. The default value is `(`. If no such sections must be detected, just leave this empty.
  - `BlackClosers`: the "black" section closers. Each character in this string has a corresponding opening character in `BlackOpeners`. The default value is `)`. If no such sections must be detected, just leave this null.
  - `NewLine`: the newline marker to use. The default value is the/ newline sequence of the host OS.
  - `Trimming`: a value indicating whether trimming spaces/tabs at both sides of any inserted newline is enabled.
  - `CrLfRemoval`: a value indicating whether CR/LF should be removed when filtering. When this is true, any CR or CR+LF or LF is replaced with a space.
