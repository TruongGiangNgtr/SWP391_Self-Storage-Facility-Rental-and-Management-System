import { useState } from 'react'
import { createPortal } from 'react-dom'
import { PlayCircle, VideoOff } from 'lucide-react'
import { unitTypeVideo, type UnitTypeVideo } from '../features/catalog/unitTypeMedia'
import { FlowDialog } from './FlowDialog'
import { SizeGuideVideoPreview } from './SizeGuideVideoPreview'

function UnitTypeVideoPlayer({ video, name }: { video: UnitTypeVideo; name: string }) {
  const [failed, setFailed] = useState(false)
  return <div className="size-guide-video-content">
    {failed ? <p className="size-guide-media-fallback" role="status"><VideoOff size={24} aria-hidden="true" />This video could not be played. You can open the source or continue comparing storage types.</p>
      : video.kind === 'embed' ? <iframe className="size-guide-video-player" src={video.src} title={`Storage illustration for ${name}`} allow="encrypted-media; fullscreen; picture-in-picture" sandbox="allow-scripts allow-same-origin allow-presentation" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen onError={() => setFailed(true)} />
        : <video className="size-guide-video-player" src={video.src} controls playsInline preload="metadata" onError={() => setFailed(true)}>Your browser does not support this video.</video>}
    <p className="size-guide-media-note">An illustration of this storage type, not a unit assignment or confirmation that your belongings will fit.</p>
    <a className="public-text-link" href={video.sourceUrl} target="_blank" rel="noopener noreferrer">Open Video in New Tab</a>
    <p className="size-guide-media-note">If the source restricts embedded playback, open it in a new tab. Availability is checked separately when you search.</p>
  </div>
}

export function SizeGuideUnitTypeMedia({ name, videoUrl }: { name: string; videoUrl?: string | null }) {
  const video = unitTypeVideo(videoUrl)
  const [open, setOpen] = useState(false)
  return <>
    <div className="size-guide-card-media">
      {video?.kind === 'file' ? <SizeGuideVideoPreview key={video.src} src={video.src} suspended={open} />
        : <div className="size-guide-media-fallback"><VideoOff size={28} aria-hidden="true" /><span>{video ? 'Select View Video to watch this storage illustration.' : videoUrl ? 'Video unavailable' : 'No video available yet'}</span></div>}
      {video && <button className="size-guide-video-button" type="button" aria-label={`View video for ${name}`} onClick={() => setOpen(true)}><PlayCircle size={20} aria-hidden="true" />View Video</button>}
    </div>
    {open && video && createPortal(<FlowDialog trapFocus className="size-guide-video-dialog" title={`${name} — Storage Video`} subtitle="View the illustration provided for this storage type." category="Size Guide" onClose={() => setOpen(false)}>
      <UnitTypeVideoPlayer key={video.src} video={video} name={name} />
    </FlowDialog>, document.body)}
  </>
}
