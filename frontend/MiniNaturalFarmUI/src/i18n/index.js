import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from './en'
import te from './te'
import enShop from './enShop'
import teShop from './teShop'

const merge = (base, extra) => {
  const out = { ...base }
  for (const [key, value] of Object.entries(extra)) {
    out[key] = value && typeof value === 'object' ? merge(base[key] || {}, value) : value
  }
  return out
}

const saved = localStorage.getItem('mnf_lang')

i18n.use(initReactI18next).init({
  resources: {
    en: { translation: merge(en, enShop) },
    te: { translation: merge(te, teShop) },
  },
  lng: saved === 'te' ? 'te' : 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
})

document.documentElement.lang = i18n.language

i18n.on('languageChanged', (lng) => {
  localStorage.setItem('mnf_lang', lng)
  document.documentElement.lang = lng
})

export default i18n
