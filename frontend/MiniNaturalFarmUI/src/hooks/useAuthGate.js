import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function useAuthGate() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  return () => {
    if (isAuthenticated) return true
    navigate('/login', { state: { from: location.pathname + location.search } })
    return false
  }
}
