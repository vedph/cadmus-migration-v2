---
title: "Rendition Filters" 
layout: default
parent: Rendition
nav_order: 4
---

Cadmus provides some builtin filters which can be used by JSON renderers or text block renderers; as for any other component type, you are free to add your own filters.

The filters to be used are typically specified in a JSON-based [configuration](config), where each filter type has its own ID. You can concatenate as many filters as you want, even when they are of the same type. All the filters will be applied in the order they are defined. Here I list the builtin filters.

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

## Thesaurus Lookup Filter

Lookup any thesaurus entry by its ID, replacing it with its value when found, or with the entry ID when not found.

- ID: `it.vedph.renderer-filter.mongo-thesaurus`
- options:
  - `ConnectionString`: connection string to the Mongo DB. This is usually omitted and supplied by the client code from its own application settings.
  - `Pattern`: the regular expression pattern representing a thesaurus ID to lookup: it is assumed that this expression has two named captures, `t` for the thesaurus ID, and `e` for its entry ID. The default pattern is a `$` followed by the thesaurus ID, `:`, and the entry ID.


