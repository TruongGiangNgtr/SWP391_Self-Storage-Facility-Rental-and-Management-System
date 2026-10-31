import {
  ChevronLeft,
  ChevronRight,
  CircleOff,
  Eye,
  MapPin,
  Pencil,
  ShieldCheck,
  Sparkles,
  Warehouse,
  Wrench,
} from 'lucide-react'
import { useMemo } from 'react'
import type { StorageUnit, StorageUnitStatus } from '../../api/storageUnitApi'
import { ClickSpark } from '../../components/react-bits/ClickSpark'
import { Squares } from '../../components/react-bits/Squares'
import { StatusBadge } from '../../components/StatusBadge'
import { StorageUnitCode } from '../../components/StorageUnitCode'
import './NeonUnitGrid.css'

export interface UnitTypeInfo {
  unitTypeId: string
  name: string
  mode: string
  size: string
  rentalPrice: number
}

export interface NeonUnitGridProps {
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

export function NeonUnitGrid({
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
}: NeonUnitGridProps) {
  const stats = useMemo(() => {
    const total = units.length
    const available = units.filter((u) => u.status === 'AVAILABLE').length
    const inUse = units.filter((u) => u.status === 'IN_USE').length
    const inspection = units.filter((u) => u.status === 'INSPECTION').length
    const maintenance = units.filter((u) => u.status === 'MAINTENANCE').length
    const occupancyRate = total > 0 ? Math.round((inUse / total) * 100) : 0
    return { total, available, inUse, inspection, maintenance, occupancyRate }
  }, [units])

  const getLockerThemeClass = (status: StorageUnitStatus) => {
    switch (status) {
      case 'AVAILABLE':
        return 'neon-locker-available'
      case 'IN_USE':
        return 'neon-locker-occupied'
      case 'INSPECTION':
        return 'neon-locker-inspection'
      case 'MAINTENANCE':
        return 'neon-locker-maintenance'
      default:
        return 'neon-locker-unavailable'
    }
  }

  const startIdx = Math.min((page - 1) * pageSize + 1, filteredUnits.length)
  const endIdx = Math.min(page * pageSize, filteredUnits.length)

  return (
    <div className="neon-grid-wrapper">
      {/* Facility Capacity & Occupancy Telemetry Bar */}
      <section className="neon-telemetry-card" aria-label="Facility capacity overview">
        <div className="neon-telemetry-header">
          <div className="neon-telemetry-title">
            <span className="neon-title-icon"><Warehouse size={18} /></span>
            <div>
              <h3>Sơ Đồ Ô Kho Holographic</h3>
              <p>Mạng lưới ô kho vật lý thời gian thực theo cấu trúc tầng &amp; phân khu</p>
            </div>
          </div>
          <div className="neon-occupancy-badge">
            <Sparkles size={14} className="neon-sparkle" />
            <span>Tỷ lệ lấp đầy: <strong>{stats.occupancyRate}%</strong></span>
          </div>
        </div>

        <div className="neon-telemetry-metrics">
          <div className="neon-metric-pill metric-total">
            <span>Tổng số ô kho</span>
            <strong>{stats.total}</strong>
          </div>
          <div className="neon-metric-pill metric-available">
            <span className="neon-pill-dot dot-emerald" />
            <span>Khả dụng</span>
            <strong>{stats.available}</strong>
          </div>
          <div className="neon-metric-pill metric-occupied">
            <span className="neon-pill-dot dot-blue" />
            <span>Đang sử dụng</span>
            <strong>{stats.inUse}</strong>
          </div>
          <div className="neon-metric-pill metric-inspection">
            <span className="neon-pill-dot dot-cyan" />
            <span>Đang kiểm tra</span>
            <strong>{stats.inspection}</strong>
          </div>
          <div className="neon-metric-pill metric-maintenance">
            <span className="neon-pill-dot dot-amber" />
            <span>Bảo trì</span>
            <strong>{stats.maintenance}</strong>
          </div>
        </div>

        {/* Multi-segmented Capacity Progress Bar */}
        <div className="neon-capacity-bar" aria-hidden="true">
          {stats.total > 0 && (
            <>
              <div
                className="capacity-segment seg-occupied"
                style={{ width: `${(stats.inUse / stats.total) * 100}%` }}
                title={`Đang sử dụng: ${stats.inUse}`}
              />
              <div
                className="capacity-segment seg-available"
                style={{ width: `${(stats.available / stats.total) * 100}%` }}
                title={`Khả dụng: ${stats.available}`}
              />
              <div
                className="capacity-segment seg-inspection"
                style={{ width: `${(stats.inspection / stats.total) * 100}%` }}
                title={`Đang kiểm tra: ${stats.inspection}`}
              />
              <div
                className="capacity-segment seg-maintenance"
                style={{ width: `${(stats.maintenance / stats.total) * 100}%` }}
                title={`Bảo trì: ${stats.maintenance}`}
              />
            </>
          )}
        </div>
      </section>

      {/* Interactive Blueprint Canvas with Squares Background */}
      <div className="neon-canvas-container">
        <Squares
          direction="diagonal"
          speed={0.35}
          squareSize={38}
          borderColor="rgba(56, 189, 248, 0.08)"
          hoverFillColor="rgba(56, 189, 248, 0.16)"
        />

        <div className="neon-lockers-grid">
          {visibleUnits.map((unit) => {
            const unitType = unitTypeCache[unit.unitTypeId]
            const target = managerStatusTarget(unit.status)
            const themeClass = getLockerThemeClass(unit.status)

            return (
              <article
                key={unit.storageUnitId}
                className={`neon-locker-card ${themeClass}`}
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
                <div className="locker-card-header">
                  <StorageUnitCode code={unit.unitCode} />
                  <StatusBadge status={unit.status} label={statusLabel(unit.status)} />
                </div>

                <div className="locker-card-body">
                  <div className="locker-type-info">
                    <strong className="locker-type-name">
                      {unitType?.name ?? 'Unit type name unavailable'}
                    </strong>
                    <span className="locker-type-mode">
                      {unitType ? `${unitType.mode} · ${unitType.size}` : 'Type details unavailable'}
                    </span>
                  </div>

                  <div className="locker-location-tag">
                    <MapPin size={13} className="location-icon" aria-hidden="true" />
                    <span>{unit.locationInfo || 'Vị trí chưa thiết lập'}</span>
                  </div>
                </div>

                <div className="locker-card-actions" onClick={(e) => e.stopPropagation()}>
                  <ClickSpark
                    sparkColor="#38bdf8"
                    sparkCount={8}
                    sparkRadius={18}
                    duration={350}
                    className="locker-action-spark"
                  >
                    <button
                      type="button"
                      className="locker-action-btn btn-view"
                      onClick={() => void onOpenDetail(unit)}
                      title="Xem chi tiết ô kho"
                    >
                      <Eye size={14} />
                      <span>Xem</span>
                    </button>
                  </ClickSpark>

                  <ClickSpark
                    sparkColor="#818cf8"
                    sparkCount={8}
                    sparkRadius={18}
                    duration={350}
                    className="locker-action-spark"
                  >
                    <button
                      type="button"
                      className="locker-action-btn btn-edit"
                      onClick={() => void onOpenEdit(unit)}
                      title="Chỉnh sửa ô kho"
                    >
                      <Pencil size={14} />
                      <span>Sửa</span>
                    </button>
                  </ClickSpark>

                  <ClickSpark
                    sparkColor={target === 'MAINTENANCE' ? '#f59e0b' : target === 'AVAILABLE' ? '#10b981' : '#64748b'}
                    sparkCount={8}
                    sparkRadius={18}
                    duration={350}
                    className="locker-action-spark"
                  >
                    <button
                      type="button"
                      className="locker-action-btn btn-status"
                      onClick={() => void onOpenStatus(unit)}
                      disabled={!target}
                      title={
                        target
                          ? `Chuyển trạng thái sang ${statusLabel(target)}`
                          : 'Trạng thái được điều khiển theo chu kỳ bàn giao/trả kho'
                      }
                    >
                      {target === 'MAINTENANCE' ? (
                        <Wrench size={14} />
                      ) : target === 'AVAILABLE' ? (
                        <ShieldCheck size={14} />
                      ) : (
                        <CircleOff size={14} />
                      )}
                      <span>
                        {target === 'MAINTENANCE'
                          ? 'Bảo trì'
                          : target === 'AVAILABLE'
                          ? 'Mở lại'
                          : 'Khóa'}
                      </span>
                    </button>
                  </ClickSpark>
                </div>
              </article>
            )
          })}
        </div>
      </div>

      {/* Neon Holographic Pagination Bar */}
      {filteredUnits.length > 0 && (
        <footer className="neon-pagination-bar" aria-label="Locker grid pagination">
          <div className="neon-pagination-info">
            <span>
              Hiển thị <strong>{startIdx}</strong>–<strong>{endIdx}</strong> trong{' '}
              <strong>{filteredUnits.length}</strong> ô kho (Trang {page}/{totalPages})
            </span>
          </div>

          {totalPages > 1 && (
            <nav className="neon-page-nav" aria-label="Điều hướng phân trang ô kho">
              <ClickSpark sparkColor="#38bdf8" sparkCount={6} sparkRadius={16} duration={300} className="neon-nav-spark">
                <button
                  type="button"
                  className="neon-nav-btn"
                  disabled={page <= 1}
                  onClick={() => onPageChange(page - 1)}
                  title="Trang trước"
                >
                  <ChevronLeft size={16} />
                  <span>Trước</span>
                </button>
              </ClickSpark>

              <div className="neon-page-numbers">
                {getPageNumbers(page, totalPages).map((p, idx) =>
                  p === '...' ? (
                    <span key={`dots-${idx}`} className="neon-page-dots">…</span>
                  ) : (
                    <ClickSpark
                      key={`spark-page-${p}`}
                      sparkColor={page === p ? '#00f5ff' : '#94a3b8'}
                      sparkCount={6}
                      sparkRadius={14}
                      duration={300}
                      className="neon-page-spark"
                    >
                      <button
                        type="button"
                        className={`neon-page-btn ${page === p ? 'active' : ''}`}
                        onClick={() => onPageChange(Number(p))}
                      >
                        {p}
                      </button>
                    </ClickSpark>
                  )
                )}
              </div>

              <ClickSpark sparkColor="#38bdf8" sparkCount={6} sparkRadius={16} duration={300} className="neon-nav-spark">
                <button
                  type="button"
                  className="neon-nav-btn"
                  disabled={page >= totalPages}
                  onClick={() => onPageChange(page + 1)}
                  title="Trang kế tiếp"
                >
                  <span>Sau</span>
                  <ChevronRight size={16} />
                </button>
              </ClickSpark>
            </nav>
          )}
        </footer>
      )}
    </div>
  )
}

export default NeonUnitGrid
