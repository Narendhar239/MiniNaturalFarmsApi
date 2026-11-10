import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { addressApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { ErrorBox, Loading } from '../components/Status'

const EMPTY = { fullName: '', phone: '', line1: '', line2: '', city: '', state: '', pincode: '', isDefault: false }
const PHONE = /^[6-9]\d{9}$/
const PINCODE = /^\d{6}$/

export default function AddressesPage() {
  const { t } = useTranslation()

  const [addresses, setAddresses] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  const [editing, setEditing] = useState(null)
  const [form, setForm] = useState(EMPTY)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')

    addressApi
      .list()
      .then((list) => !cancelled && setAddresses(list))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [reload])

  const setField = (name, value) => setForm((f) => ({ ...f, [name]: value }))

  const openNew = () => {
    setForm({ ...EMPTY, isDefault: addresses.length === 0 })
    setFormError('')
    setEditing('new')
  }

  const openEdit = (a) => {
    setForm({
      fullName: a.fullName,
      phone: a.phone,
      line1: a.line1,
      line2: a.line2 || '',
      city: a.city,
      state: a.state,
      pincode: a.pincode,
      isDefault: a.isDefault,
    })
    setFormError('')
    setEditing(a.id)
  }

  const submit = async (e) => {
    e.preventDefault()
    setFormError('')

    if (!PHONE.test(form.phone.trim())) return setFormError(t('addresses.phoneError'))
    if (!PINCODE.test(form.pincode.trim())) return setFormError(t('addresses.pincodeError'))

    const payload = {
      fullName: form.fullName.trim(),
      phone: form.phone.trim(),
      line1: form.line1.trim(),
      line2: form.line2.trim() || null,
      city: form.city.trim(),
      state: form.state.trim(),
      pincode: form.pincode.trim(),
      isDefault: form.isDefault,
    }

    setSaving(true)
    try {
      if (editing === 'new') await addressApi.create(payload)
      else await addressApi.update(editing, payload)
      setEditing(null)
      setReload((n) => n + 1)
    } catch (err) {
      setFormError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const act = async (action) => {
    setActionError('')
    try {
      await action()
      setReload((n) => n + 1)
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  const remove = (a) => {
    if (!window.confirm(t('addresses.confirmDelete'))) return
    act(() => addressApi.remove(a.id))
  }

  if (loading) return <Loading />
  if (error) return <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />

  return (
    <>
      {actionError && <div className="alert alert-error">{actionError}</div>}

      {editing === null && (
        <div className="section-head">
          <h2>{t('addresses.title')}</h2>
          <button type="button" className="btn btn-primary btn-sm" onClick={openNew}>{t('addresses.add')}</button>
        </div>
      )}

      {editing !== null && (
        <section className="card panel">
          <h2>{editing === 'new' ? t('addresses.add') : t('addresses.edit')}</h2>
          {formError && <div className="alert alert-error">{formError}</div>}

          <form className="form" onSubmit={submit}>
            <div className="form-grid">
              <label>
                {t('addresses.fullName')}
                <input type="text" value={form.fullName} onChange={(e) => setField('fullName', e.target.value)} required maxLength={150} />
              </label>
              <label>
                {t('addresses.phone')}
                <input type="tel" value={form.phone} onChange={(e) => setField('phone', e.target.value)} required maxLength={10} />
              </label>
              <label className="full">
                {t('addresses.line1')}
                <input type="text" value={form.line1} onChange={(e) => setField('line1', e.target.value)} required maxLength={250} />
              </label>
              <label className="full">
                {t('addresses.line2')}
                <input type="text" value={form.line2} onChange={(e) => setField('line2', e.target.value)} maxLength={250} />
              </label>
              <label>
                {t('addresses.city')}
                <input type="text" value={form.city} onChange={(e) => setField('city', e.target.value)} required maxLength={100} />
              </label>
              <label>
                {t('addresses.state')}
                <input type="text" value={form.state} onChange={(e) => setField('state', e.target.value)} required maxLength={100} />
              </label>
              <label>
                {t('addresses.pincode')}
                <input type="text" inputMode="numeric" value={form.pincode} onChange={(e) => setField('pincode', e.target.value)} required maxLength={6} />
              </label>
            </div>

            <label className="check">
              <input type="checkbox" checked={form.isDefault} onChange={(e) => setField('isDefault', e.target.checked)} />
              {t('addresses.makeDefault')}
            </label>

            <div className="actions">
              <button type="submit" className="btn btn-primary" disabled={saving}>{t('addresses.save')}</button>
              <button type="button" className="btn btn-outline" onClick={() => setEditing(null)}>{t('addresses.cancel')}</button>
            </div>
          </form>
        </section>
      )}

      {editing === null && (
        addresses.length === 0 ? (
          <p className="muted">{t('addresses.empty')}</p>
        ) : (
          <div className="address-grid">
            {addresses.map((a) => (
              <div key={a.id} className={`card address-card ${a.isDefault ? 'default' : ''}`}>
                {a.isDefault && <span className="pill pill-delivered">{t('addresses.default')}</span>}
                <p>
                  <strong>{a.fullName}</strong><br />
                  {a.line1}{a.line2 ? `, ${a.line2}` : ''}<br />
                  {a.city}, {a.state} - {a.pincode}<br />
                  {a.phone}
                </p>
                <div className="actions">
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => openEdit(a)}>{t('addresses.edit')}</button>
                  {!a.isDefault && (
                    <button type="button" className="btn btn-outline btn-sm" onClick={() => act(() => addressApi.setDefault(a.id))}>{t('addresses.setDefault')}</button>
                  )}
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => remove(a)}>{t('addresses.delete')}</button>
                </div>
              </div>
            ))}
          </div>
        )
      )}
    </>
  )
}
