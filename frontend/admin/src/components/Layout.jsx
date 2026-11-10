import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const LINKS = [
  ['/', 'Dashboard', true],
  ['/orders', 'Orders'],
  ['/products', 'Products'],
  ['/categories', 'Categories'],
  ['/inventory', 'Inventory'],
  ['/customers', 'Customers'],
  ['/reports', 'Reports'],
]

export default function Layout() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)

  return (
    <div className="shell">
      <aside className={`sidebar ${open ? 'open' : ''}`}>
        <div className="sidebar-brand">🌿 MNF Admin</div>
        <nav>
          {LINKS.map(([to, label, end]) => (
            <NavLink key={to} to={to} end={end} onClick={() => setOpen(false)}>{label}</NavLink>
          ))}
        </nav>
      </aside>

      <div className="main">
        <header className="topbar">
          <button type="button" className="menu-btn" onClick={() => setOpen((o) => !o)} aria-label="Menu">☰</button>
          <span className="spacer" />
          <span className="muted">{user.fullName}</span>
          <button type="button" className="btn btn-outline btn-sm" onClick={logout}>Log out</button>
        </header>
        <main className="content"><Outlet /></main>
      </div>
    </div>
  )
}
