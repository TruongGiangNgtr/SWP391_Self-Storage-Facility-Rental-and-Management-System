import { useEffect, useState } from 'react'
import { listAllAdminFacilities, type AdminFacilityLookup } from '../api/adminApi'

export function useAdminFacilities() {
  const [facilities, setFacilities] = useState<AdminFacilityLookup[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let active = true
    listAllAdminFacilities().then((items) => {
      if (active) setFacilities(items.sort((a, b) => a.name.localeCompare(b.name, 'vi')))
    }).catch(() => {
      if (active) setError(true)
    }).finally(() => {
      if (active) setLoading(false)
    })
    return () => { active = false }
  }, [attempt])

  return {
    facilities, loading, error,
    retry: () => { setLoading(true); setError(false); setAttempt((value) => value + 1) },
    facilityName: (id: string | null | undefined) => !id ? 'Global' : loading
      ? 'Loading facility…'
      : facilities.find((facility) => facility.facilityId.toLowerCase() === id.toLowerCase())?.name
        ?? 'Facility unavailable',
  }
}
