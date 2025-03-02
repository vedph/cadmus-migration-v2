# Text Tree Renderers

## Payload (Linear)

👉 Render a string representing a JSON array, using as items the nodes payloads from a linear tree. The array may directly include payloads as items, or (default) arrays which in turn include payloads, where each inner array represents a line of the original text. This can be used for single branch trees and the output is typically targeted to frontend UI components which should render rows of blocks (or just blocks) of text with links to fragments.

- ID: `it.vedph.text-tree-renderer.payload-linear`
- options:
  - `FlattenLines`: true to flatten the original lines, i.e. do not render an array of arrays, where each node is inside the inner array up to the end of the line, but rather an array of nodes, discarding line breaks.

### TEI (Linear)

👉 Render TEI with a single apparatus layer.

- ID: `it.vedph.text-tree-renderer.tei-app-linear`

### TEI Standoff (Linear)

👉 Render the base text from a linear tree into a TEI segmented text using `seg`, each with its mapped ID, so that it can be targeted by annotations.

- ID: `it.vedph.text-tree-renderer.tei-off-linear`

## TXT (Linear)

👉 Render a plain text from a linear tree. The text is obtained by concatenating the text of each node in the tree, optionally adding a newline after each node having the `IsBeforeEol` flag set to true.

- ID: `it.vedph.text-tree-renderer.txt-linear`
