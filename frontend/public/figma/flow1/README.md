# Flow 1 Figma assets

Source: https://www.figma.com/design/vsAJIrNF7AxPMS3j8rJqFl/Storage-Prototype?node-id=0-1

SVG exports are downloaded from the Figma design context without editing paths,
colors, or root width/height attributes. The code uses these local files through
`FlowIcon`; no temporary Figma asset URL is shipped in the frontend.

Design nodes:

- `214:134482`: logo, footer logo, location, unit, size, info.
- `214:136720`: close.
- `315:8019`: overview, visits, reservations, invoices, logout, add, clock,
  paid, calendar, complete, cancel, payment-start, calendar-action.
- `315:6277`: visit-confirm, visit-type, calendar-prev, calendar-next.

Fonts used by the design are stored separately in `public/fonts` with their
original license files. Sources:

- https://github.com/google/fonts/tree/main/ofl/inter
- https://github.com/google/fonts/tree/main/ofl/plusjakartasans
