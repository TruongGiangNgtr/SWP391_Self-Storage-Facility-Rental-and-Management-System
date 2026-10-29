# Public policy article catalogue

`published.json` supplies the four Help Center topics through the existing
PUB-POL-001/002 read APIs. It contains only `policyKey`, `title`, `summary` and
plain-text `sections` (`heading` / `body`), in the existing display order.

## Current content and review scope

On 2026-10-29 the owner approved consolidating the six earlier articles into
four focused topics for local manual testing and feedback (SRS §0.0.11). Content is copied from
[the policy copy document](../../../../docs/FRMS_Public_Policy_Copy_Draft.md).
Internal review notes and requirement IDs are not included in public bodies.
This authorizes the local content preview, not production/legal certification
or changes to business logic, account registration, database rules or formulas.

The former `identity-verification` and `account-privacy` topics are included
within `reservation-cancellation`, not published as separate keys. The
item-acceptance article uses the existing
`Content/AiStorageRules/storage-items.vi-VN.json` catalogue. That source still
has version `0.1.0-draft`, approval status
`DRAFT_REQUIRES_LEGAL_AND_BUSINESS_APPROVAL` and no effective date. This update
does not mark it APPROVED or alter AI screening. Keep those review facts visible
in the internal copy document; resolve formal publication separately.

## Runtime data

PolicyId, policy version and effective timestamps come from the current ACTIVE
persisted Policy parameters, not a separate article-copy version. Do not hard-code them in article content. Existing rentals keep
their captured PolicyId and financial snapshots.

No configurable time limit, due-day, fee divisor or handover window is embedded
as a fixed number. The current service attaches Policy metadata; it does not
interpolate placeholders or insert numerical thresholds into section bodies.
The CCCD format and first-month/later-month distinctions follow existing SRS
requirements and are not sampled configuration values.

## Local manual check

1. Build and restart the API from this checkout. The project copies the JSON to
   build/publish output; the singleton provider does not watch for content edits.
2. Use an existing database with a current ACTIVE Policy. No database or migration
   change is needed for these articles. Without an ACTIVE Policy, public list and
   detail return the existing 404 RESOURCE_NOT_FOUND response.
3. Refresh `/help`: all four topic cards should link to Read Policy in a 2×2 desktop grid.
4. Open each `/help/policies/{policyKey}` and check title, summary, sections,
   current Policy metadata, plain-text paragraphs and table-of-contents links.

Unknown/unpublished topic keys still return 404. The catalogue loader continues
to reject unknown/duplicate keys, empty required text/sections and HTML markup.
This content change does not claim a live PostgreSQL or production acceptance
pass; record actual runtime/manual feedback separately.
