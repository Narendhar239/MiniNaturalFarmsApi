import { useState } from 'react'
import { categoryApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import useAsync from '../hooks/useAsync'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { validateImages } from '../utils/images'

const EMPTY = { nameEn: '', nameTe: '', isActive: true }

export default function CategoriesPage() {
  const { data, loading, error, reload } = useAsync(() => categoryApi.list(), [])

  const [editing, setEditing] = useState(null) // null | 'new' | category id
  const [form, setForm] = useState(EMPTY)
  const [saving, setSaving] = useState(false)
  const [busyId, setBusyId] = useState(null)
  const [message, setMessage] = useState(null)

  const openNew = () => {
    setForm(EMPTY)
    setEditing('new')
    setMessage(null)
  }

  const openEdit = (c) => {
    setForm({ nameEn: c.nameEn, nameTe: c.nameTe, isActive: c.isActive })
    setEditing(c.id)
    setMessage(null)
  }

  const submit = async (e) => {
    e.preventDefault()
    setMessage(null)
    setSaving(true)
    try {
      const payload = { nameEn: form.nameEn.trim(), nameTe: form.nameTe.trim(), isActive: form.isActive }
      if (editing === 'new') await categoryApi.create(payload)
      else await categoryApi.update(editing, payload)
      setEditing(null)
      reload()
    } catch (err) {
      setMessage({ type: 'error', text: getErrorMessage(err) })
    } finally {
      setSaving(false)
    }
  }

  const onImage = async (c, e) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return

    const problem = validateImages([file], 0, 1)
    if (problem) return setMessage({ type: 'error', text: problem })

    setMessage(null)
    setBusyId(c.id)
    try {
      await categoryApi.uploadImage(c.id, file)
      reload()
    } catch (err) {
      setMessage({ type: 'error', text: getErrorMessage(err) })
    } finally {
      setBusyId(null)
    }
  }

  const remove = async (c) => {
    if (!window.confirm(`Delete category "${c.nameEn}"?`)) return
    setMessage(null)
    setBusyId(c.id)
    try {
      await categoryApi.remove(c.id)
      reload()
    } catch (err) {
      setMessage({ type: 'error', text: getErrorMessage(err) })
    } finally {
      setBusyId(null)
    }
  }

  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (loading && !data) return <Loading />

  return (
    <>
      <div className="section-head">
        <h1>Categories</h1>
        {editing === null && <button type="button" className="btn btn-primary" onClick={openNew}>+ New category</button>}
      </div>

      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      {editing !== null && (
        <form className="card panel form" onSubmit={submit}>
          <h2>{editing === 'new' ? 'New category' : 'Edit category'}</h2>
          <div className="form-grid">
            <label>
              Name (English)
              <input type="text" value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} required maxLength={150} />
            </label>
            <label>
              Name (Telugu)
              <input type="text" value={form.nameTe} onChange={(e) => setForm({ ...form, nameTe: e.target.value })} required maxLength={150} />
            </label>
          </div>
          <label className="check">
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
            Visible in the shop
          </label>
          <div className="actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
            <button type="button" className="btn btn-outline" onClick={() => setEditing(null)}>Cancel</button>
          </div>
          {editing === 'new' && <p className="muted small">Save first, then upload the category image from the list.</p>}
        </form>
      )}

      {data && data.length === 0 ? (
        <p className="muted">No categories yet.</p>
      ) : (
        <div className="card table-wrap">
          <table className="table">
            <thead><tr><th>Category</th><th className="right">Products</th><th>Status</th><th /></tr></thead>
            <tbody>
              {data.map((c) => (
                <tr key={c.id}>
                  <td>
                    <div className="cell-product">
                      {c.imageUrl ? <img src={c.imageUrl} alt="" /> : <span className="thumb-ph">🌿</span>}
                      <div>
                        <strong>{c.nameEn}</strong>
                        <div className="muted small">{c.nameTe}</div>
                      </div>
                    </div>
                  </td>
                  <td className="right">{c.productCount}</td>
                  <td><StatusPill value={c.isActive ? 'Active' : 'Inactive'} /></td>
                  <td className="right nowrap">
                    <button type="button" className="btn btn-outline btn-sm" onClick={() => openEdit(c)}>Edit</button>{' '}
                    <label className={`btn btn-outline btn-sm ${busyId === c.id ? 'disabled' : ''}`}>
                      {busyId === c.id ? 'Working…' : 'Upload image'}
                      <input type="file" accept="image/jpeg,image/png,image/webp" hidden disabled={busyId === c.id} onChange={(e) => onImage(c, e)} />
                    </label>{' '}
                    <button type="button" className="btn btn-danger btn-sm" disabled={busyId === c.id} onClick={() => remove(c)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
