import { useEffect, useRef, useState } from 'react'
import { PlayCircle, VideoOff } from 'lucide-react'

// Keep the video's own final frame on screen; no separate image or new API field.
export function SizeGuideVideoPreview({ src, suspended }: { src: string; suspended: boolean }) {
  const containerRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [status, setStatus] = useState<'loading' | 'ready' | 'playing' | 'blocked' | 'failed'>('loading')

  useEffect(() => {
    const container = containerRef.current
    const video = videoRef.current
    if (!container || !video) return
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)')
    const mousePointer = window.matchMedia('(hover: hover) and (pointer: fine)')
    let disposed = false, visible = false, wantsPlayback = false, attempt = 0
    let blocked = reducedMotion.matches || !mousePointer.matches

    function showStill() {
      if (disposed) return
      wantsPlayback = false
      attempt++
      video!.pause()
      if (video!.error || !Number.isFinite(video!.duration) || video!.duration <= 0) return
      // Seeking to the exact duration can display a blank ended frame.
      const endFrame = Math.max(0, video!.duration - Math.min(0.08, video!.duration / 2))
      if (Math.abs(video!.currentTime - endFrame) > 0.01) video!.currentTime = endFrame
      else if (video!.readyState >= 2) setStatus(blocked ? 'blocked' : 'ready')
    }
    function startPlayback() {
      if (disposed || !wantsPlayback || !visible || suspended || document.hidden) return
      if (video!.readyState < 1) return // loadedmetadata resumes the requested preview.
      const currentAttempt = ++attempt
      video!.muted = true
      video!.currentTime = 0
      void video!.play().then(() => {
        if (disposed || currentAttempt !== attempt) return
        if (!wantsPlayback || !visible || document.hidden) { showStill(); return }
        setStatus('playing')
      }).catch(() => {
        if (disposed || currentAttempt !== attempt) return
        blocked = true
        showStill()
        setStatus(video!.error ? 'failed' : 'blocked')
      })
    }
    function loadPreview() {
      if (!video!.getAttribute('src')) video!.src = src
      else if (video!.readyState >= 1) showStill()
    }
    function enter(event: PointerEvent) {
      if (event.pointerType !== 'mouse' || !mousePointer.matches || reducedMotion.matches || suspended || !visible || video!.error) return
      blocked = false
      wantsPlayback = true
      loadPreview()
      wantsPlayback = true
      startPlayback()
    }
    function metadata() {
      if (!Number.isFinite(video!.duration) || video!.duration <= 0) {
        blocked = true
        setStatus('blocked')
      } else if (wantsPlayback) startPlayback()
      else showStill()
    }
    function frameReady() {
      if (!wantsPlayback && !video!.seeking && video!.readyState >= 2 && !video!.error) setStatus(blocked ? 'blocked' : 'ready')
    }
    function playing() {
      if (!wantsPlayback || !visible || suspended || document.hidden || reducedMotion.matches) showStill()
    }
    function failed() { wantsPlayback = false; attempt++; setStatus('failed') }
    function ended() { wantsPlayback = false; setStatus('ready') }
    function motionChanged() {
      blocked = reducedMotion.matches || !mousePointer.matches
      if (blocked) showStill()
    }
    function visibilityChanged() { if (document.hidden) showStill() }

    container.addEventListener('pointerenter', enter)
    container.addEventListener('pointerleave', showStill)
    video.addEventListener('loadedmetadata', metadata)
    video.addEventListener('loadeddata', frameReady)
    video.addEventListener('seeked', frameReady)
    video.addEventListener('playing', playing)
    video.addEventListener('ended', ended)
    video.addEventListener('error', failed)
    reducedMotion.addEventListener('change', motionChanged)
    mousePointer.addEventListener('change', motionChanged)
    document.addEventListener('visibilitychange', visibilityChanged)
    const observer = typeof IntersectionObserver === 'undefined' ? null : new IntersectionObserver(entries => {
      if (disposed) return
      visible = entries.some(entry => entry.isIntersecting)
      if (visible) loadPreview()
      else showStill()
    })
    if (observer) observer.observe(container)
    else { visible = true; loadPreview() }
    if (suspended) showStill()

    return () => {
      disposed = true
      wantsPlayback = false
      attempt++
      observer?.disconnect()
      container.removeEventListener('pointerenter', enter)
      container.removeEventListener('pointerleave', showStill)
      video.removeEventListener('loadedmetadata', metadata)
      video.removeEventListener('loadeddata', frameReady)
      video.removeEventListener('seeked', frameReady)
      video.removeEventListener('playing', playing)
      video.removeEventListener('ended', ended)
      video.removeEventListener('error', failed)
      reducedMotion.removeEventListener('change', motionChanged)
      mousePointer.removeEventListener('change', motionChanged)
      document.removeEventListener('visibilitychange', visibilityChanged)
      video.pause()
    }
  }, [src, suspended])

  return <div className="size-guide-video-preview" ref={containerRef}>
    <video ref={videoRef} muted playsInline preload="metadata" aria-hidden="true" tabIndex={-1} />
    {status === 'loading' && <div className="size-guide-media-fallback size-guide-preview-feedback" role="status">Loading video preview…</div>}
    {status === 'failed' && <div className="size-guide-media-fallback size-guide-preview-feedback" role="status"><VideoOff size={28} aria-hidden="true" /><span>Video preview unavailable. Try opening the video.</span></div>}
    {status !== 'loading' && status !== 'failed' && <span className="size-guide-preview-hint"><PlayCircle size={16} aria-hidden="true" />{status === 'playing' ? 'Muted preview' : status === 'blocked' ? 'Select View Video to play' : 'Hover to preview · Select View Video to watch'}</span>}
  </div>
}
