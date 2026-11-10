import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../context/AuthContext'
import { getErrorMessage } from '../api/client'

export default function RegisterPage() {
  const { t } = useTranslation()
  const { register, isAuthenticated } = useAuth()
  const navigate = useNavigate()

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  if (isAuthenticated) return <Navigate to="/" replace />

  const onSubmit = async (e) => {
    e.preventDefault()
    setError('')

    if (password.length < 8) {
      setError(t('auth.passwordHint'))
      return
    }

    setSubmitting(true)
    try {
      await register({ fullName: fullName.trim(), email: email.trim(), password })
      navigate('/', { replace: true })
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-card card">
      <h1>{t('auth.registerTitle')}</h1>
      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={onSubmit} className="form">
        <label>
          {t('auth.fullName')}
          <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} required maxLength={150} autoComplete="name" />
        </label>
        <label>
          {t('auth.email')}
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
        </label>
        <label>
          {t('auth.password')}
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="new-password" />
          <small className="muted">{t('auth.passwordHint')}</small>
        </label>
        <button type="submit" className="btn btn-primary btn-block" disabled={submitting}>
          {submitting ? t('auth.submitting') : t('nav.register')}
        </button>
      </form>

      <p className="muted center">
        {t('auth.haveAccount')} <Link to="/login" className="link">{t('nav.login')}</Link>
      </p>
    </div>
  )
}
