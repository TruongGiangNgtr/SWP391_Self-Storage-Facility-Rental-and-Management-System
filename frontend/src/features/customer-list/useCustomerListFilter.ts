import { useState } from 'react'

export function useCustomerListFilter() {
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('')
  return {
    controls: { query, status, onQueryChange: setQuery, onStatusChange: setStatus },
    matches: (text: string, itemStatus: string) =>
      (!status || status === itemStatus) && text.toLocaleLowerCase('en').includes(query.trim().toLocaleLowerCase('en')),
  }
}
