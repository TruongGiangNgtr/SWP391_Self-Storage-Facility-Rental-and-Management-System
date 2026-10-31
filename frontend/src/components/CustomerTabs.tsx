import { useId, useRef, type ReactNode } from 'react'

export function CustomerTabs({ labels, counts, countScope = 'on this page', ariaLabel = 'Contract tracking sections', selected, onChange, children }: {
  labels: readonly string[]; counts?: readonly number[]; countScope?: string; ariaLabel?: string; selected: number; onChange(index: number): void; children(ids: { tabId: (index: number) => string; panelId: (index: number) => string }): ReactNode
}) {
  const id = useId()
  const buttons = useRef<(HTMLButtonElement | null)[]>([])
  const tabId = (index: number) => `${id}-tab-${index}`
  const panelId = (index: number) => `${id}-panel-${index}`
  return <>
    <div className="customer-tabs" role="tablist" aria-label={ariaLabel}>
      {labels.map((label, index) => {
        const count = counts?.[index]
        const hasCount = typeof count === 'number'
        const countTitle = hasCount && countScope ? `${count} ${countScope}` : undefined
        const buttonAria = hasCount && countScope ? `${label} (${count} ${countScope})` : label
        return (
          <button
            key={label}
            ref={element => { buttons.current[index] = element }}
            type="button"
            role="tab"
            id={tabId(index)}
            aria-controls={panelId(index)}
            aria-selected={selected === index}
            aria-label={buttonAria}
            title={countTitle}
            tabIndex={selected === index ? 0 : -1}
            onClick={() => onChange(index)}
            onKeyDown={event => {
              let next = index
              if (event.key === 'ArrowRight') next = (index + 1) % labels.length
              else if (event.key === 'ArrowLeft') next = (index - 1 + labels.length) % labels.length
              else if (event.key === 'Home') next = 0
              else if (event.key === 'End') next = labels.length - 1
              else return
              event.preventDefault()
              onChange(next)
              buttons.current[next]?.focus()
            }}
          >
            {hasCount ? <><span>{label}</span><span className="customer-tab-count" title={countTitle}>{count}</span></> : label}
          </button>
        )
      })}
    </div>
    {children({ tabId, panelId })}
  </>
}
