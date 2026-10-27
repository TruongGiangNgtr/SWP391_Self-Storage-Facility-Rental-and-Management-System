import type { FacilitySummary } from './catalog.types'

// Owner-approved illustrative artwork, selected on the FE only; not facility evidence.
const IMAGES = {
  mountain: '/images/facilities/facility-mountain.webp',
  coastal: '/images/facilities/facility-coastal.webp',
  valley: '/images/facilities/facility-valley.webp',
  highland: '/images/facilities/facility-highland.webp',
  urban: '/images/facilities/facility-urban.webp',
} as const
type ImageTheme = keyof typeof IMAGES
const THEMES = Object.keys(IMAGES) as ImageTheme[]

// IDs checked against the public catalogue; Nha Trang/Da Lat are named by province.
const PINNED_IDS: Record<string, ImageTheme> = {
  'e6ba6353-50a3-4f3f-b5f6-4c68034c8676': 'coastal', // Khanh Hoa / Nha Trang, image 2
  '8fc821d5-135f-4727-bafb-ff7f9fc3a8a1': 'highland', // Lam Dong / Da Lat, image 4
  'b8e42f20-df74-4c93-80a2-daafa7b92d92': 'valley', // An Giang, image 3
  'c34f4e04-70a7-4744-848f-f87a338cbc9c': 'urban', // Ho Chi Minh, image 5
}
const PINNED_NAMES: Record<string, ImageTheme> = {
  'nha trang': 'coastal', 'khanh hoa': 'coastal',
  'da lat': 'highland', 'lam dong': 'highland',
  'an giang': 'valley',
  'ho chi minh': 'urban', 'tp ho chi minh': 'urban', 'tp hcm': 'urban', 'tphcm': 'urban',
}

export function facilityIllustration(facility: Pick<FacilitySummary, 'facilityId' | 'name'>): string {
  const id = facility.facilityId.trim().toLowerCase()
  const name = facility.name.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd')
    .toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim().replace(/^(fstorent|fstorage)\s+/, '')
  const pinned = Object.hasOwn(PINNED_IDS, id) ? PINNED_IDS[id] : Object.hasOwn(PINNED_NAMES, name) ? PINNED_NAMES[name] : undefined
  if (pinned) return IMAGES[pinned]

  // Stable across pages, pagination, rerenders and reloads; no random render state.
  let hash = 2166136261
  for (const character of id) hash = Math.imul(hash ^ character.charCodeAt(0), 16777619) >>> 0
  return IMAGES[THEMES[hash % THEMES.length]]
}
