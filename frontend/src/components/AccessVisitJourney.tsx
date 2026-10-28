import { Check } from 'lucide-react'
import type { VisitStatus } from '../features/visits/visit.types'

const accessStages = [
  { title: 'Schedule visit', actor: 'Customer' },
  { title: 'Check in', actor: 'Facility Staff' },
  { title: 'Check out', actor: 'Facility Staff' },
]

export function AccessVisitJourney({ status }: { status?: VisitStatus }) {
  return <VisitJourney status={status} visitType="ACCESS" />
}

export function VisitJourney({ status, visitType }: { status?: VisitStatus; visitType: 'ACCESS' | 'RESERVATION' }) {
  if (status === 'CANCELLED') return null
  const handover = visitType === 'RESERVATION'
  const stages = handover ? [accessStages[0], accessStages[1], { title: 'Complete handover', actor: 'Facility Staff' }] : accessStages
  const completed = status === 'CHECKED_OUT' ? 3 : status === 'CHECKED_IN' ? 2 : status === 'SCHEDULED' ? 1 : 0
  return <ol className="access-journey" aria-label={handover ? 'Handover visit progress' : 'Access visit progress'}>
    {stages.map((stage, index) => <li key={stage.title} className={index < completed ? 'access-stage-complete' : index === completed ? 'access-stage-current' : undefined} aria-current={index === completed ? 'step' : undefined}>
      <span className="access-stage-number">{index < completed ? <Check size={16} aria-label="Completed" /> : index + 1}</span>
      <span><strong>{stage.title}</strong><small>{stage.actor}</small></span>
    </li>)}
  </ol>
}
