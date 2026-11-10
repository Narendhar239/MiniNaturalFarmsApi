import { Outlet } from 'react-router-dom'
import Header from './Header'
import Footer from './Footer'

export default function Layout() {
  return (
    <div className="app">
      <Header />
      <main className="container main">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
