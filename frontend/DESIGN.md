---
name: FStoRent / FRMS
description: A clear, calm storage rental interface anchored in confident blue.
colors:
  primary: "#2563EB"
  primary-hover: "#1D4ED8"
  primary-soft: "#EFF6FF"
  primary-border: "#DBEAFE"
  canvas: "#F8FAFC"
  surface: "#FFFFFF"
  text: "#0F172A"
  text-secondary: "#475569"
  text-muted: "#64748B"
  border: "#E2E8F0"
  control-border: "#CBD5E1"
  success: "#166534"
  success-soft: "#F0FDF4"
  warning: "#92400E"
  warning-soft: "#FFFBEB"
  danger: "#B91C1C"
  danger-soft: "#FEF2F2"
typography:
  headline:
    fontFamily: "Plus Jakarta Sans, Inter, sans-serif"
    fontSize: "30px"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.025em"
  title:
    fontFamily: "Plus Jakarta Sans, Inter, sans-serif"
    fontSize: "20px"
    fontWeight: 700
    lineHeight: 1.35
  body:
    fontFamily: "Inter, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Inter, sans-serif"
    fontSize: "14px"
    fontWeight: 600
    lineHeight: 1.5
rounded:
  small: "8px"
  control: "12px"
  panel: "16px"
  pill: "999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
  section: "32px"
  spacious: "48px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "10px 16px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
    textColor: "{colors.surface}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.primary-hover}"
    rounded: "{rounded.control}"
    padding: "10px 16px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "10px 12px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.panel}"
    padding: "24px"
---

# Design System: FStoRent / FRMS

## Overview

**Creative North Star: "A clear place for every task."**

FStoRent should feel dependable, organized and approachable. Blue identifies the next useful action; quiet surfaces let storage details, dates, amounts and operational work lead. The visual signature is confident blue, dark slate headings, softly bordered white panels and generous, deliberate spacing.

This is the intended design baseline, established with the owner around the existing customer palette. It is not a claim that every current screen already conforms. Existing employee styles include other blues; this document does not change those styles or authorize a portal migration. Apply convergence only within a separately authorized implementation scope.

Preserve the FStoRent wordmark and FRMS product identity. PRODUCT.md and approved requirements govern product meaning; this file governs visual presentation and introduces no business states, permissions, charges or workflows.

**Key Characteristics:**

- Clear hierarchy before decoration.
- One coherent blue identity across public and authenticated surfaces.
- Comfortable customer journeys and efficient employee workspaces.
- Visible labels, readable numbers and explicit feedback.

## Colors

The frontmatter is the normative palette. Use semantic roles rather than introducing a new hex value for each page.

### Primary

- **Confident Blue** (`primary`): primary actions, selected navigation, links and focus outlines.
- **Deep Blue** (`primary-hover`): hover and pressed emphasis on primary actions.
- **Blue Tint** (`primary-soft`, `primary-border`): selected surfaces and informational emphasis. Avoid large saturated blue containers around ordinary content.

### Neutral

- **Cool Canvas** (`canvas`): application background and quiet supporting areas.
- **White Surface** (`surface`): cards, tables, forms, menus and dialogs.
- **Slate Ink** (`text`): headings, values and essential body content.
- **Supporting Slate** (`text-secondary`): descriptions and supporting instructions.
- **Muted Slate** (`text-muted`): metadata and nonessential secondary text; never make required instructions faint.
- **Surface Border** (`border`): panel boundaries and dividers. **Control Border** (`control-border`): input boundaries where stronger definition is needed.

### Semantic feedback

Use success, warning and danger foregrounds on their matching soft backgrounds. They communicate feedback, not additional brand identities. Map existing domain statuses explicitly; a visually green badge must never invent or imply a successful business transition.

**The Meaning Before Color Rule.** Every status has a visible label. Use an icon or another cue where helpful; color alone never carries meaning.

**The One Blue Rule.** New work uses the primary token consistently. Do not copy Genesis indigo or its green brand accent into FRMS. Existing outliers are migration work, not additional approved primary colors.

## Typography

Use the existing locally hosted fonts: **Plus Jakarta Sans** for headings and the wordmark, **Inter** for body text, controls, tables and metadata. Keep sans-serif fallbacks. Do not add a font provider or download another family for a single page.

| Role | Size | Weight | Line height | Use |
| --- | --- | --- | --- | --- |
| Public display | 32–48px, responsive | 700 | 1.18 | Homepage lead heading only |
| Page heading | 26–30px, responsive | 700 | 1.25 | Main task or page title |
| Section heading | 20px | 700 | 1.35 | Major content groups |
| Subheading | 18px | 600–700 | 1.4 | Subsections and card titles |
| Body / table / control | 14px | 400–500 | 1.5 | Operational reading and input |
| Public body | 16px | 400 | 1.625 | Explanatory public content |
| Label | 14px | 600 | 1.5 | Field labels and actions |
| Metadata | 12–13px | 400–500 | 1.5 | Dates and secondary context |

Keep paragraphs around 60–75 characters wide where possible. Use sentence case and descriptive headings. Reserve uppercase for short, occasional overlines. Use tabular numerals for financial columns and align numbers consistently; do not change existing currency, date or month semantics.

## Layout

Use a 4px spacing rhythm, with the frontmatter scale covering common gaps and padding. Customer content uses the existing centered 1240px container, 24px desktop gutters and 24px gaps. Keep the customer sidebar near its existing 248px width where space permits. Operational pages may retain their established widths to support dense tables.

Separate page identity, primary action, filters and results into a predictable reading order. Group related fields and information through spacing first. Use cards for meaningful groups, not around every label or value. Keep primary actions close to the task they complete.

On narrow screens, reduce gutters to 16px, stack columns and allow headings and actions to wrap. Collapse navigation through the established mobile pattern. Preserve labels and essential context. Tables may scroll within a labeled region; the document itself must not overflow horizontally. Do not shrink table text to make every column fit a phone.

Public pages can use larger headings, imagery and more breathing room. Authenticated workspaces prioritize scanability and efficient task completion. Do not apply a marketing hero layout to operational pages.

## Elevation & Depth

Use tonal backgrounds and subtle borders to separate surfaces. Standard panels use a quiet shadow (`0 1px 3px rgb(15 23 42 / 4%)`), consistent with the customer stylesheet. Floating menus may use the existing stronger shadow (`0 12px 24px rgb(15 23 42 / 12%)`). Dialogs need clear separation from their backdrop.

Focus uses a visible 3px primary outline with 3px offset; reserve adequate space so containers do not clip it. Validate focus contrast on the actual surrounding surface.

Keep color and border transitions brief, around 160ms. Avoid moving buttons, rows or financial cards on hover. Honor reduced-motion preferences. Use motion only to explain a change of state or location.

## Shapes

Use the radius scale consistently: small corners for compact elements, control corners for fields and buttons, panel corners for major surfaces. Pills belong to short status badges and avatars; they are not the default shape for every control.

Prefer thin, quiet borders. Input boundaries, selected states and focus indicators must remain distinguishable; a decorative divider's color is not automatically sufficient for an interactive boundary. Keep icons consistent with the existing icon system and do not substitute emoji for navigation or status icons.

## Components

### Buttons

Primary buttons use white text on primary blue, a minimum height of 44px and the frontmatter padding. Give each task area one dominant action; use secondary or text actions for alternatives. Destructive actions use danger styling rather than brand blue.

Keep action text visible during submission and communicate progress. Prevent repeat submission where needed by the existing flow. Disabled styling must remain understandable and must not depend on opacity alone. Icon-only actions need accessible names and at least a 44px interaction target.

### Inputs and validation

Use persistent visible labels, white surfaces and clear control borders. Place short instructions near the relevant field. Place specific validation feedback next to its source and connect it programmatically. Preserve entered values after a recoverable error. Placeholders provide examples; they do not replace labels.

Reuse existing date, password and error components. Show user-friendly recovery guidance while preserving stable API error-code handling. Technical references may be secondary details, never the main explanation.

### Cards, summaries and tables

Cards have one clear heading and a meaningful information group. Highlight the most useful value rather than making every metric large or colored. Avoid nested card stacks and repeated decorative icons.

Tables have clear column headers, consistent row spacing, aligned numeric columns and nearby filters. Preserve identity and relevant context when presenting details. Empty and filtered-empty results explain what happened and offer an applicable next step. Do not invent bulk operations or workflows to satisfy a visual pattern.

### Navigation and status badges

Selected navigation combines blue text, a pale blue surface and an explicit selected/current indicator. Pair unfamiliar icons with labels. Preserve role and facility context in employee portals.

Status badges use concise labels and semantic colors based on approved domain meanings. Reuse existing badge components; do not introduce alternate mappings in individual pages.

### Dialogs and page states

Dialogs have a clear title, understandable action labels, managed keyboard focus and a visible exit where the existing workflow permits one. Return focus to the trigger when closing. Never discard entered work silently.

Cover loading, empty, validation, forbidden, not found, conflict and unexpected-error states with the existing state components. Announce relevant asynchronous changes accessibly. Use skeletons only when they clarify the structure being loaded. A provider failure must not look like a confirmed successful transaction.

### Imagery and help

Use existing storage imagery and illustrations where they explain the offering or help selection. Keep images proportional and use purposeful alternative text. Do not present an unverified image as a real facility, or add unsupported testimonials, statistics or guarantees.

## Do's and Don'ts

### Do

- Reuse BrandLogo, existing controls, PageStates, error alerts and status badges before adding alternatives.
- Keep blue reserved for identity, interaction and meaningful selection.
- Make keyboard focus, labels, errors and current navigation visible.
- Verify contrast on rendered surfaces: target at least 4.5:1 for normal text, 3:1 for large text and essential non-text indicators.
- Check desktop, narrow mobile and 200% zoom when implementing changes; preserve reading order and task context.
- Preserve Vietnamese diacritics, long names, amounts, dates and multi-line labels without clipping.

### Don't

- Treat this document as proof that existing screens meet these rules or pass accessibility checks.
- Replace approved business terminology, financial meaning or server-side decisions through visual design.
- Add a second competing brand palette, arbitrary gradients or decorative glass effects to routine workspaces.
- Hide essential actions behind hover, remove visible labels or communicate status through color alone.
- Add claims, features, new business statuses or extra approval steps for visual completeness.
- Rewrite employee portals or backend code as a side effect of documenting this baseline.
