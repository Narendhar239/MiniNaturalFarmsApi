import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { categoryApi, productApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import useAsync from '../hooks/useAsync'
import { ErrorBox, Loading } from '../components/Status'
import { validateImages } from '../utils/images'

const EMPTY = {
  nameEn: '', nameTe: '', descriptionEn: '', descriptionTe: '',
  price: '', discountPrice: '', stockQuantity: '0', categoryId: '', isActive: true,
}

const toForm = (p) => ({
  nameEn: p.nameEn,
  nameTe: p.nameTe,
  descriptionEn: p.descriptionEn || '',
  descriptionTe: p.descriptionTe || '',
  price: String(p.price),
  discountPrice: p.discountPrice == null ? '' : String(p.discountPrice),
  stockQuantity: String(p.stockQuantity),
  categoryId: String(p.categoryId),
  isActive: p.isActive,
})

const ok = (text) => ({ type: 'success', text })
const bad = (text) => ({ type: 'error', text })

// The key makes React start fresh when switching between "new" and an existing product
export default function ProductFormPage() {
  const { id } = useParams()
  return <ProductEditor key={id ?? 'new'} id={id} />
}

function ProductEditor({ id }) {
  const isNew = !id
  const navigate = useNavigate()
  const location = useLocation()

  const cats = useAsync(() => categoryApi.list(), [])
  const prod = useAsync(() => (isNew ? Promise.resolve(null) : productApi.get(id)), [id])

  const [form, setForm] = useState(EMPTY)
  const [images, setImages] = useState([])
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState(location.state?.created ? ok('Product created. You can now add images below.') : null)
  const [imageMessage, setImageMessage] = useState(null)

  useEffect(() => {
    if (prod.data) {
      setForm(toForm(prod.data))
      setImages(prod.data.images || [])
    }
  }, [prod.data])

  if (cats.error) return <ErrorBox message={cats.error} onRetry={cats.reload} />
  if (prod.error) return <><ErrorBox message={prod.error} onRetry={prod.reload} /><Link to="/products" className="link">← Back to products</Link></>
  if ((cats.loading && !cats.data) || (!isNew && prod.loading && !prod.data)) return <Loading />

  const set = (name, value) => setForm((f) => ({ ...f, [name]: value }))

  const submit = async (e) => {
    e.preventDefault()
    setMessage(null)

    const price = Number(form.price)
    const discount = form.discountPrice === '' ? null : Number(form.discountPrice)
    const stock = Number(form.stockQuantity)

    if (!(price > 0)) return setMessage(bad('Price must be greater than 0.'))
    if (discount !== null && !(discount > 0 && discount < price)) return setMessage(bad('Discount price must be more than 0 and less than the price.'))
    if (!Number.isInteger(stock) || stock < 0) return setMessage(bad('Stock must be a whole number, 0 or more.'))
    if (!form.categoryId) return setMessage(bad('Choose a category.'))

    const payload = {
      nameEn: form.nameEn.trim(),
      nameTe: form.nameTe.trim(),
      descriptionEn: form.descriptionEn.trim(),
      descriptionTe: form.descriptionTe.trim(),
      price,
      discountPrice: discount,
      stockQuantity: stock,
      categoryId: Number(form.categoryId),
      isActive: form.isActive,
    }

    setSaving(true)
    try {
      if (isNew) {
        const created = await productApi.create(payload)
        navigate(`/products/${created.id}`, { replace: true, state: { created: true } })
        return
      }
      const updated = await productApi.update(id, payload)
      setForm(toForm(updated))
      setImages(updated.images)
      setMessage(ok('Saved.'))
    } catch (err) {
      setMessage(bad(getErrorMessage(err)))
    } finally {
      setSaving(false)
    }
  }

  const onFiles = async (e) => {
    const files = Array.from(e.target.files || [])
    e.target.value = ''
    if (files.length === 0) return

    setImageMessage(null)
    const problem = validateImages(files, images.length)
    if (problem) return setImageMessage(bad(problem))

    setUploading(true)
    try {
      const updated = await productApi.addImages(id, files)
      setImages(updated.images)
      setImageMessage(ok(`${files.length} image(s) uploaded.`))
    } catch (err) {
      setImageMessage(bad(getErrorMessage(err)))
    } finally {
      setUploading(false)
    }
  }

  const imageAction = async (action) => {
    setImageMessage(null)
    try {
      const updated = await action()
      setImages(updated.images)
    } catch (err) {
      setImageMessage(bad(getErrorMessage(err)))
    }
  }

  const removeImage = (img) => {
    if (!window.confirm('Delete this image?')) return
    imageAction(() => productApi.deleteImage(id, img.id))
  }

  return (
    <>
      <Link to="/products" className="link">← Back to products</Link>
      <h1>{isNew ? 'New product' : form.nameEn || 'Edit product'}</h1>

      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      <form className="card panel form" onSubmit={submit}>
        <div className="form-grid">
          <label>
            Name (English)
            <input type="text" value={form.nameEn} onChange={(e) => set('nameEn', e.target.value)} required maxLength={250} />
          </label>
          <label>
            Name (Telugu)
            <input type="text" value={form.nameTe} onChange={(e) => set('nameTe', e.target.value)} required maxLength={250} />
          </label>
          <label>
            Description (English)
            <textarea rows="4" value={form.descriptionEn} onChange={(e) => set('descriptionEn', e.target.value)} maxLength={4000} />
          </label>
          <label>
            Description (Telugu)
            <textarea rows="4" value={form.descriptionTe} onChange={(e) => set('descriptionTe', e.target.value)} maxLength={4000} />
          </label>
          <label>
            Category
            <select value={form.categoryId} onChange={(e) => set('categoryId', e.target.value)} required>
              <option value="">Select category…</option>
              {(cats.data || []).map((c) => (
                <option key={c.id} value={c.id}>{c.nameEn}{c.isActive ? '' : ' (inactive)'}</option>
              ))}
            </select>
          </label>
          <label>
            Stock quantity
            <input type="number" min="0" step="1" value={form.stockQuantity} onChange={(e) => set('stockQuantity', e.target.value)} required />
          </label>
          <label>
            Price (₹)
            <input type="number" min="0.01" step="0.01" value={form.price} onChange={(e) => set('price', e.target.value)} required />
          </label>
          <label>
            Discount price (₹, optional)
            <input type="number" min="0.01" step="0.01" value={form.discountPrice} onChange={(e) => set('discountPrice', e.target.value)} />
          </label>
        </div>

        <label className="check">
          <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} />
          Visible in the shop
        </label>

        <div className="actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : isNew ? 'Create product' : 'Save changes'}</button>
          <Link to="/products" className="btn btn-outline">Cancel</Link>
        </div>
      </form>

      {!isNew && (
        <section className="card panel">
          <div className="section-head">
            <h2>Images ({images.length}/8)</h2>
            <label className={`btn btn-primary btn-sm ${uploading || images.length >= 8 ? 'disabled' : ''}`}>
              {uploading ? 'Uploading…' : '+ Upload images'}
              <input type="file" accept="image/jpeg,image/png,image/webp" multiple hidden disabled={uploading || images.length >= 8} onChange={onFiles} />
            </label>
          </div>
          <p className="muted small">JPEG, PNG or WebP, up to 5 MB each. The first image, or the one marked Primary, is shown on product cards.</p>

          {imageMessage && <div className={`alert alert-${imageMessage.type}`}>{imageMessage.text}</div>}

          {images.length === 0 ? (
            <p className="muted">No images yet.</p>
          ) : (
            <div className="image-grid">
              {images.map((img) => (
                <div key={img.id} className={`image-tile ${img.isPrimary ? 'primary' : ''}`}>
                  <img src={img.url} alt="" />
                  {img.isPrimary && <span className="pill pill-paid">Primary</span>}
                  <div className="image-actions">
                    {!img.isPrimary && (
                      <button type="button" className="btn btn-outline btn-sm" onClick={() => imageAction(() => productApi.setPrimaryImage(id, img.id))}>Make primary</button>
                    )}
                    <button type="button" className="btn btn-danger btn-sm" onClick={() => removeImage(img)}>Delete</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}
    </>
  )
}
