---
target: "BOM Reports: quá nhiều nội dung và kéo dài xuống"
total_score: 26
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:D:\\SWP391_Self-Storage-Facility-Rental-and-Management-System\\frontend\\src\\features\\business-reports\\BusinessReportsPage.tsx"
target_fingerprint: "sha256:44b0870cf67ff9159630f5682e9c7749a52c4aa8915cf1db8115c587d18023ed"
target_path: "D:\\SWP391_Self-Storage-Facility-Rental-and-Management-System\\frontend\\src\\features\\business-reports\\BusinessReportsPage.tsx"
timestamp: 2026-10-28T16-57-33Z
slug: business-reports-businessreportspage-tsx-17c6408f
closed: true
---
Method: dual-agent (A: /root/report_design_review · B: /root/report_evidence_review)

BOM Reports design critique — Operate mode

Verdict: domain-aware reporting content in a generic, repetitive dashboard composition. The page is too vertically long because the same figures recur across KPI cards, charts and the detail table, and comparison charts grow with every row. Tightening spacing alone is insufficient.

Nielsen design health: 26/40.

| Heuristic | Score | Key issue |
|---|---:|---|
| System status | 3/4 | Loading/applied scope/export feedback present; unapplied edits lack a cue |
| Real-world match | 3/4 | Relevant domain terminology; Rental Status mixes current inventory and revenue period |
| User control | 3/4 | Reset, tabs, retry and non-destructive exploration |
| Consistency | 2/4 | Many 10–12px labels and indigo variants depart from operational baseline |
| Error prevention | 3/4 | Validation and export guards; draft scope can be confused with applied scope |
| Recognition | 3/4 | Explicit labels/chips; long separation between controls and exact results |
| Efficiency | 2/4 | Mandatory chart stacks delay table/pagination |
| Minimalist design | 1/4 | Repeated totals, panels and explanatory prose |
| Error recovery | 3/4 | Reusable error presentation and retries |
| Help/documentation | 3/4 | Accurate collapsed definitions; repeated help elsewhere |

Strengths: current-inventory versus period-revenue distinction is accurate; exact values remain in a semantic table and scope-wide CSV; labeled controls and reusable loading/error states provide a sound base.

Priority issues:
1. [P1] Charts scale with rows and duplicate the report. GroupChart renders up to 20 rows; Unit Type renders two such charts before its table. At <=1100px panels stack. Make the table primary and grouped comparison charts optional. Preserve all rows in table/CSV and clearly label page-local comparisons. Commands: impeccable distill, layout.
2. [P1] Control/context stack delays results. Tabs, filter form, actions, applied chips, timestamp and export note each occupy their own band. Merge desktop filters/actions into a compact toolbar; combine result scope/time/export. On mobile collapse filters after applying, retaining visible applied scope. Commands: impeccable layout, adapt.
3. [P2] Secondary totals add repetition rather than insight. Revenue components repeats all three KPI values. Status cards repeat the current-inventory caption. Replace large KPI cards with a compact summary strip and remove the redundant components panel. Keep help in one disclosure. Commands: impeccable distill, quieter.
4. [P2] Mobile exact-value reading requires long vertical travel then horizontal scrolling. Retain semantic table, provide visible scroll guidance and readable labels, preserve row identity where feasible. Avoid converting all rows into tall cards. Commands: impeccable adapt, typeset.

Cognitive load: high, 4/8 checklist failures (single focus, hierarchy, sequencing, progressive disclosure). Four tabs/four filter fields are manageable; competing representations are the main problem.
Emotional journey: orderly first impression, then a slow search for exact values after filtering; export feedback provides useful reassurance.
Personas: power users must scroll past mandatory charts to inspect details; distracted mobile users encounter the table after several screens; accessibility-dependent users face small labels and a horizontal-scroll region whose keyboard/zoom behavior remains unverified.
Minor observations: Rental Status should emphasize current physical snapshot and reduce revenue-period noise without changing API semantics. Chart scales reset per page. Shared footer adds length but is outside the proposed BWP-07 scope.

Evidence: current source/CSS plus immediately preceding mocked-data screenshots. Desktop 1440x1667, mobile 390x2867, status view with ONE rendered group row. Approximate table-card start desktop y1015, mobile y1807. A 20-row chart can add roughly900px over this fixture; 12 monthly rows roughly550–600px, derived from CSS, not a stress-render measurement. Other tabs assessed from source.
Detector: successful run, 5 advisory design-system-color findings at TSX21,22,23,212 (two at212); colors #168365, #4b61ed, #a66508. No hard findings. These are palette drift, not proof of contrast defects.

Questions to guide action:
1. Default result: compact summary plus table, charts on demand (recommended), or one visible compact chart above the table?
2. Scope: top three structural issues only, or all four including mobile table usability (recommended)?
