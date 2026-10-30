import type { EvidenceType, InspectionEvidenceDetail } from './return.types'

export interface EvidencePreviewRecord {
  inspectionEvidenceId: string
  evidenceType: EvidenceType
  fileName: string
  mimeType: string
  fileSize: number
  dataUrl: string | null
  objectUrl?: string | null
  createdAt: string
}

const STORAGE_KEY = 'frms.inspectionEvidencePreviews.v1'
const MAX_PERSISTED_DATA_URL_BYTES = 1_500_000
const MAX_STORED_RECORDS = 24
const memoryPreviews = new Map<string, EvidencePreviewRecord>()

function readPersistedMap(): Record<string, EvidencePreviewRecord> {
  if (typeof window === 'undefined') return {}
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY) ?? window.sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return {}
    const parsed = JSON.parse(raw) as unknown
    if (!parsed || typeof parsed !== 'object') return {}
    return parsed as Record<string, EvidencePreviewRecord>
  } catch {
    return {}
  }
}

function writePersistedMap(records: Record<string, EvidencePreviewRecord>) {
  if (typeof window === 'undefined') return
  const entries = Object.entries(records)
    .sort(([, left], [, right]) => right.createdAt.localeCompare(left.createdAt))
    .slice(0, MAX_STORED_RECORDS)
  const serialized = JSON.stringify(Object.fromEntries(entries))
  try {
    window.localStorage.setItem(STORAGE_KEY, serialized)
  } catch {
    try {
      window.sessionStorage.setItem(STORAGE_KEY, serialized)
    } catch {
      // Storage quota reached; in-memory preview remains available for the active tab.
    }
  }
}

function readFileAsDataUrl(file: File): Promise<string | null> {
  if (file.size > MAX_PERSISTED_DATA_URL_BYTES) return Promise.resolve(null)
  return new Promise((resolve) => {
    const reader = new FileReader()
    reader.onload = () => resolve(typeof reader.result === 'string' ? reader.result : null)
    reader.onerror = () => resolve(null)
    reader.readAsDataURL(file)
  })
}

export async function saveEvidencePreview(
  evidence: InspectionEvidenceDetail,
  file: File,
): Promise<EvidencePreviewRecord> {
  const dataUrl = await readFileAsDataUrl(file)
  const objectUrl = typeof URL !== 'undefined' && typeof URL.createObjectURL === 'function'
    ? URL.createObjectURL(file)
    : null

  const record: EvidencePreviewRecord = {
    inspectionEvidenceId: evidence.inspectionEvidenceId,
    evidenceType: evidence.evidenceType,
    fileName: file.name,
    mimeType: file.type || (evidence.evidenceType === 'IMAGE' ? 'image/jpeg' : 'application/octet-stream'),
    fileSize: file.size,
    dataUrl,
    objectUrl,
    createdAt: evidence.createdAt,
  }

  memoryPreviews.set(evidence.inspectionEvidenceId, record)
  const persisted = readPersistedMap()
  persisted[evidence.inspectionEvidenceId] = {
    ...record,
    objectUrl: null,
  }
  writePersistedMap(persisted)
  return record
}

export function getEvidencePreview(inspectionEvidenceId: string): EvidencePreviewRecord | null {
  const fromMemory = memoryPreviews.get(inspectionEvidenceId)
  if (fromMemory) return fromMemory
  const persisted = readPersistedMap()
  return persisted[inspectionEvidenceId] ?? null
}

export function formatEvidenceSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`
}
