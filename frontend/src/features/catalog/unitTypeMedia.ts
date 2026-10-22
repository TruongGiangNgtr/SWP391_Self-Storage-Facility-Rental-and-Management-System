export type UnitTypeVideo = { kind: 'file' | 'embed'; src: string; sourceUrl: string }

// Public media is read from UnitType, never inferred from a Facility or name.
// Reject executable/data URLs, credentials and insecure mixed content. Missing
// or unsupported media must not prevent manual sizing or reservation.
export function publicMediaUrl(value: unknown): string | null {
  if (typeof value !== 'string' || !value.trim() || value.length > 2048) return null
  try {
    const url = new URL(value.trim())
    return url.protocol === 'https:' && !url.username && !url.password ? url.href : null
  } catch { return null }
}

export function unitTypeVideo(value: unknown): UnitTypeVideo | null {
  const sourceUrl = publicMediaUrl(value)
  if (!sourceUrl) return null
  const url = new URL(sourceUrl)
  const host = url.hostname.toLowerCase()
  const parts = url.pathname.split('/').filter(Boolean)
  if (['youtube.com', 'www.youtube.com', 'm.youtube.com', 'youtu.be', 'www.youtu.be', 'youtube-nocookie.com', 'www.youtube-nocookie.com'].includes(host)) {
    const id = host.endsWith('youtu.be') ? parts[0] : url.pathname === '/watch' ? url.searchParams.get('v') :
      ['embed', 'shorts', 'live'].includes(parts[0]) ? parts[1] : null
    return id && /^[A-Za-z0-9_-]{11}$/.test(id)
      ? { kind: 'embed', src: `https://www.youtube-nocookie.com/embed/${id}`, sourceUrl }
      : null
  }
  if (['vimeo.com', 'www.vimeo.com', 'player.vimeo.com'].includes(host)) {
    const id = host === 'player.vimeo.com' && parts[0] === 'video' ? parts[1] : parts[0]
    if (!id || !/^\d+$/.test(id)) return null
    const embed = new URL(`https://player.vimeo.com/video/${id}`)
    // Preserve the access hash of an unlisted video, never arbitrary parameters.
    const hash = url.searchParams.get('h') ?? (host === 'player.vimeo.com' ? undefined : parts[1])
    if (hash && /^[A-Za-z0-9]+$/.test(hash)) embed.searchParams.set('h', hash)
    return { kind: 'embed', src: embed.href, sourceUrl }
  }
  // Other HTTPS URLs use the browser's video decoder, not an arbitrary iframe.
  // A non-video page/unsupported codec produces a safe player error instead.
  return { kind: 'file', src: sourceUrl, sourceUrl }
}
