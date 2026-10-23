import { useContext } from 'react'
import { CircleAlert } from 'lucide-react'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { customerErrorFeedback, isUnavailableError } from '../api/customerErrorPresentation'
import { CustomerErrorAudience } from './ErrorAudienceContext'

export function ApiErrorAlert({ error }: { error: ApiErrorPresentation | null }) {
  const customerAudience = useContext(CustomerErrorAudience)
  if (!error) {
    return null
  }

  if (customerAudience && !isUnavailableError(error)) {
    const feedback = customerErrorFeedback(error)
    return <div className="form-error customer-error-alert" role="alert" aria-live="assertive" aria-atomic="true">
      <CircleAlert className="customer-error-icon" size={20} aria-hidden="true" />
      <div className="customer-error-content">
        <strong className="customer-error-title">{feedback.title}</strong>
        <p>{feedback.message}</p>
        {feedback.details.length > 0 && <ul>{feedback.details.map(detail => <li key={detail}>{detail}</li>)}</ul>}
        {error.traceId && <details className="customer-error-reference"><summary>Support reference</summary><p>Share this reference with support if you need help.</p><small>{error.traceId}</small></details>}
      </div>
    </div>
  }

  return (
    <div className="form-error" role="alert" aria-live="assertive">
      <p>{error.message}</p>
      {error.details.length > 0 && (
        <ul>
          {error.details.map((detail) => (
            <li key={detail}>{detail}</li>
          ))}
        </ul>
      )}
      {error.traceId && <small>Trace ID: {error.traceId}</small>}
    </div>
  )
}
