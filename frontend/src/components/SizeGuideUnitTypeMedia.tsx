import { useState } from 'react'
import { VideoOff } from 'lucide-react'
import { unitTypeVideo, type UnitTypeVideo } from '../features/catalog/unitTypeMedia'
import { SizeGuideVideoPreview } from './SizeGuideVideoPreview'

function EmbeddedUnitTypeVideo({ video, name }: { video: UnitTypeVideo; name: string }) {
  const [failed, setFailed] = useState(false)
  return failed ? <p className="size-guide-media-fallback" role="status"><VideoOff size={24} aria-hidden="true" />This video could not be played. You can continue comparing storage types.</p>
    : <iframe className="size-guide-video-player" src={video.src} title={`Storage illustration for ${name}`} allow="encrypted-media; fullscreen; picture-in-picture" sandbox="allow-scripts allow-same-origin allow-presentation" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen loading="lazy" onError={() => setFailed(true)} />
}

export function SizeGuideUnitTypeMedia({ name, videoUrl }: { name: string; videoUrl?: string | null }) {
  const video = unitTypeVideo(videoUrl)
  return <div className="size-guide-card-media">
    {video?.kind === 'file' ? <SizeGuideVideoPreview key={video.src} src={video.src} />
      : video?.kind === 'embed' ? <EmbeddedUnitTypeVideo key={video.src} video={video} name={name} />
        : <div className="size-guide-media-fallback"><VideoOff size={28} aria-hidden="true" /><span>{videoUrl ? 'Video unavailable' : 'No video available yet'}</span></div>}
  </div>
}
