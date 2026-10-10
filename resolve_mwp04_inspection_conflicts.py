#!/usr/bin/env python3
"""Local-only resolution for duplicate Inspection model/response names on MWP-04.
No GitHub API calls; does not modify SQL; default is dry-run.
Expected input: be/MWP-04/Return-&-Inspection-Monitoring at c09d7afb.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

BASE = Path('backend')

CHANGES = {
    'Frms.DataAccess/Repositories/Models/InspectionRecords.cs': [
        ('public sealed record InspectionDetailRecord(',
         'public sealed record InspectionMonitoringDetailRecord('),
    ],
    'Frms.DataAccess/Repositories/Interfaces/IInspectionRepository.cs': [
        ('Task<InspectionDetailRecord?> GetDetailAsync(',
         'Task<InspectionMonitoringDetailRecord?> GetDetailAsync('),
    ],
    'Frms.DataAccess/Repositories/Implementations/InspectionRepository.cs': [
        ('Task<InspectionDetailRecord?> GetDetailAsync(',
         'Task<InspectionMonitoringDetailRecord?> GetDetailAsync('),
        ('return new InspectionDetailRecord(summary, damages, extraFees, evidence);',
         'return new InspectionMonitoringDetailRecord(summary, damages, extraFees, evidence);'),
    ],
    'Frms.Business/Services/Interfaces/IInspectionService.cs': [
        ('Task<InspectionDetailRecord> GetAccessibleAsync(',
         'Task<InspectionMonitoringDetailRecord> GetAccessibleAsync('),
    ],
    'Frms.Business/Services/Implementations/InspectionService.cs': [
        ('Task<InspectionDetailRecord> GetAccessibleAsync(',
         'Task<InspectionMonitoringDetailRecord> GetAccessibleAsync('),
    ],
    'Frms.Api/DTOs/Responses/InspectionMonitoringResponses.cs': [
        ('public sealed record InspectionSummaryResponse(',
         'public sealed record InspectionMonitoringSummaryResponse('),
        ('public sealed record InspectionDamageResponse(',
         'public sealed record InspectionMonitoringDamageResponse('),
        ('public sealed record InspectionExtraFeeResponse(',
         'public sealed record InspectionMonitoringExtraFeeResponse('),
        ('public sealed record InspectionDetailResponse(',
         'public sealed record InspectionMonitoringDetailResponse('),
        ('    InspectionSummaryResponse Inspection,',
         '    InspectionMonitoringSummaryResponse Inspection,'),
        ('IReadOnlyList<InspectionDamageResponse> Damages,',
         'IReadOnlyList<InspectionMonitoringDamageResponse> Damages,'),
        ('IReadOnlyList<InspectionExtraFeeResponse> ExtraFees,',
         'IReadOnlyList<InspectionMonitoringExtraFeeResponse> ExtraFees,'),
    ],
    'Frms.Api/Controllers/InspectionsController.cs': [
        ('using Frms.Business.Models.Results;\nusing Frms.Business.Services.Interfaces;\n',
         'using Frms.Business.Models.Results;\n'),
        ('public sealed class InspectionsController(IInspectionService inspectionService) : ScaffoldControllerBase',
         'public sealed class InspectionsController(\n    IInspectionService inspectionService,\n    IInspectionWorkflowService workflow) : ScaffoldControllerBase'),
        ('Task<ActionResult<ApiResponse<InspectionDetailResponse>>> GetInspection(',
         'Task<ActionResult<ApiResponse<InspectionMonitoringDetailResponse>>> GetInspection('),
        ('new ApiResponse<InspectionDetailResponse>(new InspectionDetailResponse(',
         'new ApiResponse<InspectionMonitoringDetailResponse>(new InspectionMonitoringDetailResponse('),
        ('new InspectionDamageResponse(', 'new InspectionMonitoringDamageResponse('),
        ('new InspectionExtraFeeResponse(', 'new InspectionMonitoringExtraFeeResponse('),
        ('ToSummaryResponse(', 'ToMonitoringSummaryResponse('),
        ('private static InspectionSummaryResponse ToMonitoringSummaryResponse(',
         'private static InspectionMonitoringSummaryResponse ToMonitoringSummaryResponse('),
    ],
}


def validate(root: Path, content_by_path: dict[Path, str]) -> None:
    def get(rel: str) -> str:
        p = root / BASE / rel
        return content_by_path.get(p, p.read_text(encoding='utf-8-sig'))

    model_a = get('Frms.DataAccess/Repositories/Models/InspectionRecords.cs')
    model_b = get('Frms.DataAccess/Repositories/Models/ReturnInspectionRecords.cs')
    dto_a = get('Frms.Api/DTOs/Responses/InspectionMonitoringResponses.cs')
    dto_b = get('Frms.Api/DTOs/Responses/InspectionResponses.cs')

    def record_names(s: str) -> set[str]:
        return set(re.findall(r'\bpublic\s+sealed\s+record\s+(\w+)\s*\(', s))

    for label, a, b in [('DataAccess records', model_a, model_b),
                        ('API response records', dto_a, dto_b)]:
        names = record_names(a) & record_names(b)
        if names:
            raise RuntimeError(f'{label} still duplicate: {sorted(names)}')
    ctrl = get('Frms.Api/Controllers/InspectionsController.cs')
    if not ('IInspectionService inspectionService' in ctrl
            and 'IInspectionWorkflowService workflow' in ctrl):
        raise RuntimeError('InspectionsController requires both inspectionService and workflow.')
    if 'InspectionMonitoringDetailRecord' not in model_a or 'InspectionDetailRecord' not in model_b:
        raise RuntimeError('Both independent detail records must be preserved.')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path('.'),
                        help='Repository root (contains backend/)')
    g = parser.add_mutually_exclusive_group()
    g.add_argument('--apply', action='store_true', help='Apply local changes')
    g.add_argument('--check', action='store_true', help='Validate already-patched files')
    args = parser.parse_args()
    root = args.root.resolve()
    if args.check:
        validate(root, {})
        print('PASS: distinct DataAccess and API DTO record names; controller has both services.')
        return 0

    candidate = {}
    errors = []
    changed_paths = []
    for path, replacements in CHANGES.items():
        full = root / BASE / path
        if not full.is_file():
            errors.append(f'Missing file: {full}')
            continue
        old_text = full.read_text(encoding='utf-8-sig')
        new_text = old_text
        for old, new in replacements:
            count = new_text.count(old)
            if count == 0:
                errors.append(f'Cannot match target in {path}: {old[:90]!r}')
                continue
            if count > 1 and old != 'ToSummaryResponse(':
                errors.append(f'Ambiguous replacement count {count} in {path}: {old[:90]!r}')
                continue
            new_text = new_text.replace(old, new)
        candidate[full] = new_text
        if new_text != old_text:
            changed_paths.append(path)

    if errors:
        print('FAIL: Source differs from checked branch; no files changed.', file=sys.stderr)
        for e in errors:
            print(' - ' + e, file=sys.stderr)
        return 2
    try:
        validate(root, candidate)
    except (OSError, RuntimeError) as e:
        print('FAIL: ' + str(e) + ' (no files changed)', file=sys.stderr)
        return 2

    print(f'PASS: replacement preflight ({len(changed_paths)} files)')
    for p in changed_paths:
        print('  ' + str(BASE / p))
    if not args.apply:
        print('\nDRY RUN — nothing changed. Rerun with --apply to modify local files.')
        return 0
    for file, content in candidate.items():
        file.write_text(content, encoding='utf-8')
    validate(root, {})
    print('\nPASS: local patch applied and record-name collisions checked.')
    print('NEXT: dotnet build backend/Frms.slnx')
    return 0


if __name__ == '__main__':
    sys.exit(main())
