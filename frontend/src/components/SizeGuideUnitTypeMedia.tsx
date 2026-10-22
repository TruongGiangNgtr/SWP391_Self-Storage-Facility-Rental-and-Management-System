import { useState } from 'react'
import { createPortal } from 'react-dom'
import { ImageOff, PlayCircle, VideoOff } from 'lucide-react'
import { publicMediaUrl, unitTypeVideo, type UnitTypeVideo } from '../features/catalog/unitTypeMedia'
import { FlowDialog } from './FlowDialog'

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

export function SizeGuideUnitTypeMedia({ name, imageUrl, videoUrl }: { name: string; imageUrl?: string | null; videoUrl?: string | null }) {
  const image = publicMediaUrl(imageUrl)
  const video = unitTypeVideo(videoUrl)
  const [failedImage, setFailedImage] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  return <>
    <div className="size-guide-card-media">
      {image && failedImage !== image ? <img src={image} alt={`Storage type illustration: ${name}`} loading="lazy" decoding="async" referrerPolicy="no-referrer" onError={() => setFailedImage(image)} />
        : <div className="size-guide-media-fallback"><ImageOff size={28} aria-hidden="true" /><span>{image ? 'Image unavailable' : 'No image available yet'}</span></div>}
      {video ? <button className="size-guide-video-button" type="button" aria-label={`Watch video for ${name}`} onClick={() => setOpen(true)}><PlayCircle size={20} aria-hidden="true" />Watch Video</button>
        : videoUrl ? <p className="size-guide-media-note size-guide-video-unavailable">Video unavailable</p> : null}
    </div>
    {open && video && createPortal(<FlowDialog trapFocus className="size-guide-video-dialog" title={`${name} — Storage Video`} subtitle="View the illustration provided for this storage type." category="Size Guide" onClose={() => setOpen(false)}>
      <UnitTypeVideoPlayer key={video.src} video={video} name={name} />
    </FlowDialog>, document.body)}
  </>
}
