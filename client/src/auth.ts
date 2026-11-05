import { createContext, useContext } from 'react'
import type { Role } from './api'

export const AuthContext = createContext<{ email: string; name: string; role: Role }>({ email: '', name: '', role: 'Viewer' })

export function useAuth() {
  const { email, name, role } = useContext(AuthContext)
  return { email, name, role, isAdmin: role === 'Admin', canEdit: role === 'Admin' || role === 'Editor' }
}
