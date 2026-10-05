import type { ApiErrorPresentation } from '../api/apiErrorPresentation'

export function ApiErrorAlert({ error }: { error: ApiErrorPresentation | null }) {
  if (!error) {
    return null
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
      {error.traceId && <small>Mã đối chiếu: {error.traceId}</small>}
    </div>
  )
}
