import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { profileApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { ErrorBox, Loading } from '../components/Status'

const PHONE = /^[6-9]\d{9}$/

export default function AccountProfilePage() {
  const { t } = useTranslation()
  const { updateUser } = useAuth()

  const [email, setEmail] = useState('')
  const [fullName, setFullName] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [reload, setReload] = useState(0)

  const [profileMsg, setProfileMsg] = useState({ type: '', text: '' })
  const [savingProfile, setSavingProfile] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [passwordMsg, setPasswordMsg] = useState({ type: '', text: '' })
  const [savingPassword, setSavingPassword] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setLoadError('')

    profileApi
      .get()
      .then((p) => {
        if (cancelled) return
        setEmail(p.email)
        setFullName(p.fullName)
        setPhone(p.phoneNumber || '')
      })
      .catch((e) => !cancelled && setLoadError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [reload])

  const saveProfile = async (e) => {
    e.preventDefault()
    setProfileMsg({ type: '', text: '' })

    if (phone.trim() && !PHONE.test(phone.trim())) {
      setProfileMsg({ type: 'error', text: t('addresses.phoneError') })
      return
    }

    setSavingProfile(true)
    try {
      const res = await profileApi.update({ fullName: fullName.trim(), phoneNumber: phone.trim() || null })
      updateUser({ fullName: res.fullName })
      setProfileMsg({ type: 'success', text: t('account.saved') })
    } catch (err) {
      setProfileMsg({ type: 'error', text: getErrorMessage(err) })
    } finally {
      setSavingProfile(false)
    }
  }

  const savePassword = async (e) => {
    e.preventDefault()
    setPasswordMsg({ type: '', text: '' })
    setSavingPassword(true)
    try {
      await profileApi.changePassword({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setPasswordMsg({ type: 'success', text: t('account.passwordChanged') })
    } catch (err) {
      setPasswordMsg({ type: 'error', text: getErrorMessage(err) })
    } finally {
      setSavingPassword(false)
    }
  }

  if (loading) return <Loading />
  if (loadError) return <ErrorBox message={loadError} onRetry={() => setReload((n) => n + 1)} />

  return (
    <div className="two-col">
      <section className="card panel">
        <h2>{t('account.profile')}</h2>
        {profileMsg.text && <div className={`alert alert-${profileMsg.type}`}>{profileMsg.text}</div>}
        <form className="form" onSubmit={saveProfile}>
          <label>
            {t('account.email')}
            <input type="email" value={email} disabled />
          </label>
          <label>
            {t('account.fullName')}
            <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} required maxLength={150} />
          </label>
          <label>
            {t('account.phone')}
            <input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} maxLength={10} placeholder={t('account.phoneHint')} />
          </label>
          <button type="submit" className="btn btn-primary" disabled={savingProfile}>{t('account.save')}</button>
        </form>
      </section>

      <section className="card panel">
        <h2>{t('account.changePassword')}</h2>
        {passwordMsg.text && <div className={`alert alert-${passwordMsg.type}`}>{passwordMsg.text}</div>}
        <form className="form" onSubmit={savePassword}>
          <label>
            {t('account.currentPassword')}
            <input type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} required autoComplete="current-password" />
          </label>
          <label>
            {t('account.newPassword')}
            <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required minLength={8} autoComplete="new-password" />
            <small className="muted">{t('auth.passwordHint')}</small>
          </label>
          <button type="submit" className="btn btn-primary" disabled={savingPassword}>{t('account.changePassword')}</button>
        </form>
      </section>
    </div>
  )
}
