# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

FRMS supports exactly five business roles:

- `CUSTOMER`: find suitable storage, reserve capacity, arrange visits, manage rentals and invoices, renew or return storage, and request support.
- `FACILITY_STAFF`: handle facility visits, handover, returns, inspections and assigned support work.
- `FACILITY_MANAGER`: manage physical units and oversee handover, inspections and operations within the assigned facility.
- `BUSINESS_OPERATIONS_MANAGER`: manage business configuration and monitor performance across facilities.
- `SYSTEM_ADMINISTRATOR`: manage accounts, employee roles and facility assignments, and inspect security/activity history within approved permissions.

Anonymous visitors can browse public facilities, unit types, requested-period capacity, sizing guidance and public help before authentication. Guest access is an access mode, not a sixth business role.

Primary-audience priority across these roles remains undecided; do not assume that one portal takes precedence.

## Product Purpose

FRMS means Self-Storage Facility Rental and Management System. It connects customer storage rental with the facility work needed to operate those rentals. The approved scope covers seven end-to-end flows (SRS sections 2–4):

1. Storage Unit Reservation.
2. Storage Check-in and Handover.
3. Rented Storage Unit Management.
4. Business Rules, Fee Management, and Revenue Monitoring.
5. Facility Storage and Staff Management.
6. Storage Renewal and Overdue Handling.
7. Support Request and Issue Handling.

Success requires usable journeys backed by approved lifecycle, authorization and persistence behavior. Rendered UI or source code alone is not evidence of release acceptance.

## Positioning

The documented operating mechanism is month-based capacity reservation by facility, unit type and month range, followed by physical-unit selection during the handover process. A reservation does not allocate a physical unit. This is a product mechanism, not a verified competitive-differentiation claim.

## Operating Context

Customers use the public site and their own rental portal; employees use role-specific operational portals. Facility staff and managers operate within their assigned facility, while business operations has system-wide business scope. Administrator access does not imply business-data mutation authority.

Customer authentication uses a phone number; employee authentication uses email. Private operations require an active account and authoritative server checks for role, facility, ownership and current state.

Business calendar rules and timestamp display use `Asia/Ho_Chi_Minh` (GMT+7). API timestamps are UTC; month values are `YYYY-MM` and must not shift with timezones. The existing formatter presents currency as VND (`src/utils/formatters.ts`).

## Capabilities and Constraints

- Existing frontend: React + TypeScript + Vite, with React Router and typed API consumption. Use existing components and API abstractions before adding alternatives.
- The frontend is never authoritative for capacity, money, payment status, authorization or lifecycle transitions. REST calls use `/api/v1`, camelCase JSON and string enums; errors are handled through stable `error.code` values.
- Preserve customer ownership, facility scope and the distinction between public information and private rental/operational records.
- Support multiple customer contracts where approved rules permit. Preserve immutable financial snapshots and protected history; do not invent new financial obligations, states, entities, permissions or approval workflows.
- Preserve the approved distinction between first-month offline collection and invoice-based payments for eligible other obligations. Consult the governing SRS/API contracts before changing any payment or handover behavior; this record does not define financial formulas.
- Optional AI sizing assistance must not block manual storage selection or reservation when unavailable.
- Cover loading, empty, validation, forbidden, not-found, conflict and unexpected-error states.
- Local development uses the existing `pnpm --dir frontend dev` command and Vite port 5173. Its proxy requires the owner's local `VITE_API_PROXY_TARGET`; never invent environment values or expose secrets in frontend configuration.

### Authority and open decisions

The owner approved using `../docs/FRMS_SRS_V11.md` for this product record on 2026-10-27. Relevant product sections are 2 (purpose/scope), 3 (actors/access) and 4 (flows); section 0.0.9 identifies scoped owner decisions. The API and data-dictionary companions are `../docs/FRMS_API_V11.md` and `../docs/FRMS_Data_Dictionary_V11.md`.

This file summarizes product context and does not replace requirements or grant approval for implementation changes. Repository instructions name a V11 FINAL source under `project_sources/`, while the available SRS labels itself a V11 REVIEW CANDIDATE with scoped approvals and pending acceptance. The repository instructions and available documents also disagree on database architecture and payment-provider identity. These authority/status and technical conflicts remain unresolved here; obtain an approved clarification before an affected implementation. Earlier-version references in README/TECH_BASELINE are not new approval.

Primary-audience priority, competitive positioning, a binding copy voice and a product-specific accessibility standard remain undecided.

## Brand Commitments

FRMS is the repository/product-system name. The incumbent web interface uses the FStoRent wordmark in `src/components/BrandLogo.tsx` and the document title in `index.html`. Preserve these existing names unless the owner requests a change. Existing code and assets provide evidence of the incumbent interface; init does not establish a replacement visual identity.

## Evidence on Hand

- Requirements: the V11 SRS and companion API/data-dictionary files under `../docs/`, with approval and release-status qualifications retained.
- Implementation: role-specific pages, layouts, authentication guards and API modules under `src/`; implementation presence is distinct from runtime acceptance.
- Existing brand and media assets: `src/components/BrandLogo.tsx`, `public/favicon.svg`, `src/assets/hero.png`, `public/images/` and `public/figma/`. Presence does not establish provenance or prove that depicted facilities/customers are real.
- Existing shared formatting: `src/utils/formatters.ts`.

No customer testimonial, facility count, service guarantee, benchmark, competitive superiority or production-readiness claim was verified during init. Do not fabricate these as interface content.

## Product Principles

1. Keep customer choices and operational actions aligned with the approved rental lifecycle.
2. Make role, facility and ownership context clear while enforcing authority on the server.
3. Represent money, capacity and historical records faithfully; distinguish pending work from confirmed outcomes.
4. Keep manual rental journeys usable when optional AI assistance fails.
5. Reuse established terminology and components, and surface missing decisions rather than inventing requirements.
