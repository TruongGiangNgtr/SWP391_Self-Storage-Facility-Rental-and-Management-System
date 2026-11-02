// User-supplied reference map, preserved as-is. All positions below are image pixels,
// not geographic coordinates. Facility markers remain illustrative city-level pins.
export const VIETNAM_MAP_IMAGE = {
  src: '/images/home/vietnam-reference-map.png', width: 860, height: 573,
} as const

// Trim only blank outer margins; retain mainland, islands and source labels together.
export const VIETNAM_MAP_VIEWBOX = { x: 160, y: 0, width: 560, height: 573 } as const

export const VIETNAM_MAP_LOCATIONS = [
  { key: 'ha noi', label: 'Hà Nội', x: 302, y: 106 },
  { key: 'hai phong', label: 'Hải Phòng', x: 360, y: 107 },
  { key: 'da nang', label: 'Đà Nẵng', x: 369, y: 289 },
  { key: 'ho chi minh', label: 'Hồ Chí Minh', x: 369, y: 482 },
] as const

export function normalizeMapLocation(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd')
    .toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim()
}
