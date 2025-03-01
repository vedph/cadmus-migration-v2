# Renderer Context Suppliers

## Flag

👉 Inspect the item's flags from the received context, and for each flag bitvalue on/off and mapped in its configuration it supplies a name=value pair to the context data.

- ID: `it.vedph.renderer-context-supplier.flag`
- options:
  - `On`: the flag to pair mappings for "on" states of flags. Keys are flags (decimal values, or hexadecimal values prefixed by H or h), values are the corresponding name=value pair as a string with `=` as separator. If the value is only the name (without = and the value), the pair is removed from the context data when present.
  - `Off`: the flag to pair mappings for "off" states of flags. Keys are flags (decimal values, or hexadecimal values prefixed by H or h), values are the corresponding name=value pair as a string with `=` as separator. If the value is only the name (without = and the value), the pair is removed from the context data when present.
