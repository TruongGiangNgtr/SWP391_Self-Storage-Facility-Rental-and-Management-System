import {
  ChevronLeft,
  ChevronRight,
  CircleOff,
  Eye,
  MapPin,
  Pencil,
  ShieldCheck,
  Warehouse,
  Wrench,
} from 'lucide-react'
import { useMemo } from 'react'
import type { StorageUnit, StorageUnitStatus } from '../../api/storageUnitApi'
import { StatusBadge } from '../../components/StatusBadge'
import { formatMoney } from '../../utils/formatters'
import './NeonUnitGrid.css'

export interface UnitTypeInfo {
  unitTypeId: string
  name: string
  mode: string
  size: string
  rentalPrice: number
}

export interface StorageUnitGridProps {
  units: StorageUnit[]
  filteredUnits: StorageUnit[]
  visibleUnits: StorageUnit[]
  page: number
  totalPages: number
  pageSize?: number
  onPageChange: (page: number) => void
  unitTypeCache: Record<string, UnitTypeInfo>
  onOpenDetail: (unit: StorageUnit) => void
  onOpenEdit: (unit: StorageUnit) => void
  onOpenStatus: (unit: StorageUnit) => void
  managerStatusTarget: (status: StorageUnitStatus) => StorageUnitStatus | null
  statusLabel: (status: StorageUnitStatus) => string
}

export type NeonUnitGridProps = StorageUnitGridProps

function getPageNumbers(current: number, total: number): (number | '...')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }
  if (current <= 4) {
    return [1, 2, 3, 4, 5, '...', total]
  }
  if (current >= total - 3) {
    return [1, '...', total - 4, total - 3, total - 2, total - 1, total]
  }
  return [1, '...', current - 1, current, current + 1, '...', total]
}

export function StorageUnitGrid({
  units,
  filteredUnits,
  visibleUnits,
  page,
  totalPages,
  pageSize = 12,
  onPageChange,
  unitTypeCache,
  onOpenDetail,
  onOpenEdit,
  onOpenStatus,
  managerStatusTarget,
  statusLabel,
}: StorageUnitGridProps) {
  const stats = useMemo(() => {
    const total = units.length
    const available = units.filter((u) => u.status === 'AVAILABLE').length
    const inUse = units.filter((u) => u.status === 'IN_USE').length
    const inspection = units.filter((u) => u.status === 'INSPECTION').length
    const maintenance = units.filter((u) => u.status === 'MAINTENANCE').length
    const occupancyRate = total > 0 ? Math.round((inUse / total) * 100) : 0
    return { total, available, inUse, inspection, maintenance, occupancyRate }
  }, [units])

  const startIdx = Math.min((page - 1) * pageSize + 1, filteredUnits.length)
  const endIdx = Math.min(page * pageSize, filteredUnits.length)

  return (
    <div className="storage-grid-wrapper">
      {/* Facility Capacity & Occupancy Overview Card */}
      <section className="storage-telemetry-card" aria-label="Facility capacity overview">
        <div className="storage-telemetry-header">
          <div className="storage-telemetry-title">
            <span className="storage-title-icon">
              <Warehouse size={18} aria-hidden="true" />
            </span>
            <div>
              <h3>Storage Unit Capacity Overview</h3>
              <p>Real-time physical unit allocation and status across facility zones</p>
            </div>
          </div>
          <div className="storage-occupancy-badge">
            <span>Occupancy: <strong>{stats.occupancyRate}%</strong></span>
          </div>
        </div>

        <div className="storage-telemetry-metrics">
          <div className="storage-metric-pill">
            <span>Total Units</span>
            <strong>{stats.total}</strong>
          </div>
          <div className="storage-metric-pill">
            <span className="storage-pill-dot dot-available" aria-hidden="true" />
            <span>Available</span>
            <strong>{stats.available}</strong>
          </div>
          <div className="storage-metric-pill">
            <span className="storage-pill-dot dot-occupied" aria-hidden="true" />
            <span>In Use</span>
            <strong>{stats.inUse}</strong>
          </div>
          <div className="storage-metric-pill">
            <span className="storage-pill-dot dot-inspection" aria-hidden="true" />
            <span>Under Inspection</span>
            <strong>{stats.inspection}</strong>
          </div>
          <div className="storage-metric-pill">
            <span className="storage-pill-dot dot-maintenance" aria-hidden="true" />
            <span>Maintenance</span>
            <strong>{stats.maintenance}</strong>
          </div>
        </div>

        {/* Multi-segmented Capacity Progress Bar */}
        <div className="storage-capacity-bar" aria-hidden="true">
          {stats.total > 0 && (
            <>
              <div
                className="capacity-segment seg-occupied"
                style={{ width: `${(stats.inUse / stats.total) * 100}%` }}
                title={`In Use: ${stats.inUse}`}
              />
              <div
                className="capacity-segment seg-available"
                style={{ width: `${(stats.available / stats.total) * 100}%` }}
                title={`Available: ${stats.available}`}
              />
              <div
                className="capacity-segment seg-inspection"
                style={{ width: `${(stats.inspection / stats.total) * 100}%` }}
                title={`Under Inspection: ${stats.inspection}`}
              />
              <div
                className="capacity-segment seg-maintenance"
                style={{ width: `${(stats.maintenance / stats.total) * 100}%` }}
                title={`Maintenance: ${stats.maintenance}`}
              />
            </>
          )}
        </div>
      </section>

      {/* Storage Units Cards Grid */}
      <div className="storage-units-grid">
        {visibleUnits.map((unit) => {
          const unitType = unitTypeCache[unit.unitTypeId]
          const target = managerStatusTarget(unit.status)

          return (
            <article
              key={unit.storageUnitId}
              className="storage-unit-card"
              onClick={() => void onOpenDetail(unit)}
              tabIndex={0}
              role="button"
              aria-label={`Storage unit ${unit.unitCode}, status ${statusLabel(unit.status)}`}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault()
                  void onOpenDetail(unit)
                }
              }}
            >
              <div className="storage-card-header">
                <div className="unit-card-code-wrap">
                  <Warehouse size={16} className="unit-card-code-icon" aria-hidden="true" />
                  <strong className="unit-card-code">{unit.unitCode}</strong>
                </div>
                <StatusBadge status={unit.status} label={statusLabel(unit.status)} />
              </div>

              <div className="storage-card-body">
                <div className="unit-card-type-name">
                  {unitType?.name ?? 'Unit type name unavailable'}
                </div>
                <div className="unit-card-type-meta">
                  {unitType
                    ? `${unitType.mode} · ${unitType.size} · ${formatMoney(unitType.rentalPrice)}`
                    : 'Unit type details unavailable'}
                </div>
                <div className="unit-card-location">
                  <MapPin size={13} aria-hidden="true" />
                  <span>{unit.locationInfo || 'Location not assigned'}</span>
                </div>
              </div>

              <div className="storage-card-actions" onClick={(e) => e.stopPropagation()}>
                <button
                  type="button"
                  className="unit-action-btn"
                  onClick={() => void onOpenDetail(unit)}
                  title="View unit details"
                >
                  <Eye size={14} aria-hidden="true" />
                  <span>View</span>
                </button>

                <button
                  type="button"
                  className="unit-action-btn"
                  onClick={() => void onOpenEdit(unit)}
                  title="Edit unit type or location"
                >
                  <Pencil size={14} aria-hidden="true" />
                  <span>Edit</span>
                </button>

                <button
                  type="button"
                  className="unit-action-btn"
                  onClick={() => void onOpenStatus(unit)}
                  disabled={!target}
                  title={
                    target
                      ? `Change status to ${statusLabel(target)}`
                      : 'Status is controlled by handover/return lifecycle'
                  }
                >
                  {target === 'MAINTENANCE' ? (
                    <Wrench size={14} aria-hidden="true" />
                  ) : target === 'AVAILABLE' ? (
                    <ShieldCheck size={14} aria-hidden="true" />
                  ) : (
                    <CircleOff size={14} aria-hidden="true" />
                  )}
                  <span>
                    {target === 'MAINTENANCE'
                      ? 'Maintain'
                      : target === 'AVAILABLE'
                      ? 'Restore'
                      : 'Lifecycle'}
                  </span>
                </button>
              </div>
            </article>
          )
        })}
      </div>

      {/* Enterprise Pagination Bar */}
      {filteredUnits.length > 0 && (
        <footer className="storage-pagination-bar" aria-label="Storage unit grid pagination">
          <div className="storage-pagination-info">
            <span>
              Showing <strong>{startIdx}</strong>–<strong>{endIdx}</strong> of{' '}
              <strong>{filteredUnits.length}</strong> storage units (Page {page} of {totalPages})
            </span>
          </div>

          {totalPages > 1 && (
            <nav className="storage-page-nav" aria-label="Storage unit pagination navigation">
              <button
                type="button"
                className="storage-nav-btn"
                disabled={page <= 1}
                onClick={() => onPageChange(page - 1)}
                title="Previous page"
              >
                <ChevronLeft size={16} aria-hidden="true" />
                <span>Previous</span>
              </button>

              <div className="storage-page-numbers">
                {getPageNumbers(page, totalPages).map((p, idx) =>
                  p === '...' ? (
                    <span key={`dots-${idx}`} className="storage-page-dots">…</span>
                  ) : (
                    <button
                      key={`page-${p}`}
                      type="button"
                      className={`storage-page-btn ${page === p ? 'active' : ''}`}
                      onClick={() => onPageChange(Number(p))}
                    >
                      {p}
                    </button>
                  )
                )}
              </div>

              <button
                type="button"
                className="storage-nav-btn"
                disabled={page >= totalPages}
                onClick={() => onPageChange(page + 1)}
                title="Next page"
              >
                <span>Next</span>
                <ChevronRight size={16} aria-hidden="true" />
              </button>
            </nav>
          )}
        </footer>
      )}
    </div>
  )
}

export const NeonUnitGrid = StorageUnitGrid
export default StorageUnitGrid
