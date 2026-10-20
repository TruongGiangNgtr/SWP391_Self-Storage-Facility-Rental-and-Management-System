# Approved public policy articles

`published.json` is an explicit reviewed-content publication catalogue.
The shipped list is intentionally empty: no approved article copy was available in
SRS V10.2 or repository source. Do not publish the illustrative SRS example.

After legal/business approval, add only the six approved keys, with title,
summary and a nonempty sections array of heading/body plain-text pairs.
No hard-coded policy version, PolicyId, effective timestamps or numerical terms:
those are derived from the active persisted Policy snapshot by PUB-POL-001/002.

Before approval the list endpoint returns 200 with an empty collection when
an ACTIVE Policy exists; detail returns 404. This is not feature acceptance.
