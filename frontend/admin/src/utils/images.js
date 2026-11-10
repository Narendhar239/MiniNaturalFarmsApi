export const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
export const MAX_BYTES = 5 * 1024 * 1024

export function validateImages(files, existingCount = 0, max = 8) {
  if (existingCount + files.length > max) return `You can have at most ${max} images.`
  for (const f of files) {
    if (!ALLOWED_TYPES.includes(f.type)) return `${f.name} must be a JPEG, PNG or WebP image.`
    if (f.size > MAX_BYTES) return `${f.name} is larger than 5 MB.`
  }
  return null
}
