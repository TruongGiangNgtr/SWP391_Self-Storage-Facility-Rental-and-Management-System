import { Navigate } from 'react-router-dom'

// Preserve old AWP-05 bookmarks while keeping a single management page.
export default function AccessManagementPage() {
  return <Navigate to="/admin/assignment?tab=permissions" replace />
}
