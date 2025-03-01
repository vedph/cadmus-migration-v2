# Text Tree Filters

>Linear filters are those filters working on "linear" trees, i.e. those trees having a single branch.

## Block (Linear)

👉 Split nodes at every occurrence of a LF character. Whenever a node is split, the resulting nodes have the same payload except for the text; the original node text is copied only up to the LF excluded; a new node with text past LF is added if this text is not empty, and this new right-half node becomes the child of the left-half node and the parent of what was the child of the original node.

- ID: `it.vedph.text-tree-filter.block-linear`
