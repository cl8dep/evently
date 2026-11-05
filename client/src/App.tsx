import { CalendarMonth, Add, Close, Logout, Menu, Sell, People, Search, Tag } from '@mui/icons-material'
import {
  Box, Button, ButtonBase, CircularProgress, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, Typography, useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { useCallback, useEffect, useState } from 'react'
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { api, type Me } from './api'
import { AuthContext } from './auth'
import CommandPalette from './components/CommandPalette'
import Logo from './components/Logo'
import UserMenu from './components/UserMenu'
import Taxonomy from './pages/Taxonomy'
import EventDetail from './pages/EventDetail'
import EventForm from './pages/EventForm'
import Events from './pages/Events'
import Login from './pages/Login'
import Users from './pages/Users'

const WIDTH = 220
const isMac = /Mac|iPhone|iPad/.test(navigator.platform)

export default function App() {
  const [me, setMe] = useState<Me | null | undefined>(undefined)
  const [menu, setMenu] = useState(false)
  const [searching, setSearching] = useState(false)
  const { pathname, search } = useLocation()
  const navigate = useNavigate()
  const compact = useMediaQuery(useTheme().breakpoints.down('md'))

  const refresh = useCallback(
    () => api<Me>('/api/me').then(setMe, () => setMe({ email: null, firstName: null, lastName: null, role: null, setup: false })),
    [],
  )
  useEffect(() => { void refresh() }, [refresh])

  useEffect(() => { setMenu(false) }, [pathname])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); setSearching(s => !s) }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  if (me === undefined) return <Box sx={{ p: 4 }}><CircularProgress /></Box>
  if (me === null || me.email === null) return <Login setup={me?.setup ?? false} onDone={refresh} />
  const { email } = me
  const role = me.role ?? 'Viewer'
  const name = [me.firstName, me.lastName].filter(Boolean).join(' ')
  const canEdit = role === 'Admin' || role === 'Editor'

  const logout = async () => {
    await api('/api/auth/logout', 'POST')
    setMe({ email: null, firstName: null, lastName: null, role: null, setup: false })
    navigate('/')
  }

  const nav = (
    <List disablePadding sx={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
      <ListItemButton selected={pathname.startsWith('/events')} onClick={() => navigate('/events')} sx={{ minHeight: compact ? 44 : 36 }}>
        <ListItemIcon><CalendarMonth fontSize="small" /></ListItemIcon>
        <ListItemText primary="Events" />
      </ListItemButton>
      <ListItemButton selected={pathname.startsWith('/categories')} onClick={() => navigate('/categories')} sx={{ minHeight: compact ? 44 : 36 }}>
        <ListItemIcon><Sell fontSize="small" /></ListItemIcon>
        <ListItemText primary="Categories" />
      </ListItemButton>
      <ListItemButton selected={pathname.startsWith('/topics')} onClick={() => navigate('/topics')} sx={{ minHeight: compact ? 44 : 36 }}>
        <ListItemIcon><Tag fontSize="small" /></ListItemIcon>
        <ListItemText primary="Topics" />
      </ListItemButton>
      {role === 'Admin' && (
        <ListItemButton selected={pathname.startsWith('/users')} onClick={() => navigate('/users')} sx={{ minHeight: compact ? 44 : 36 }}>
          <ListItemIcon><People fontSize="small" /></ListItemIcon>
          <ListItemText primary="Users" />
        </ListItemButton>
      )}
    </List>
  )

  const glass = { background: 'var(--surface-glass)', backdropFilter: 'var(--blur-material)' }

  return (
    <AuthContext value={{ email, name, role }}>
    <Box sx={{ minHeight: '100vh', display: 'flex' }}>
      {!compact && (
        <Box component="aside" sx={{ width: WIDTH + 20, flexShrink: 0, position: 'sticky', top: 0, height: '100vh', p: 1.25 }}>
          <Box sx={{ ...glass, height: '100%', p: 1.25, borderRadius: '16px', boxShadow: 'var(--shadow-float)', display: 'flex', flexDirection: 'column', overflowY: 'auto' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, pl: 0.75, py: 1, pb: 2 }}>
              <Logo size={28} />
              <Typography sx={{ fontSize: 16, fontWeight: 650, letterSpacing: '-0.02em' }}>Evently</Typography>
            </Box>
            {nav}
          </Box>
        </Box>
      )}

      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <Box component="header" sx={{ position: 'sticky', top: 0, zIndex: 1100, flexShrink: 0, p: compact ? '8px 8px 0' : '10px 10px 0' }}>
          <Box
            sx={{
              ...glass, height: 56, px: compact ? 1 : 2, borderRadius: '16px', boxShadow: 'var(--shadow-float)',
              display: 'flex', alignItems: 'center', gap: compact ? 0.75 : 2,
            }}
          >
            {compact && <IconButton aria-label="Open menu" onClick={() => setMenu(true)} sx={{ width: 44, height: 44 }}><Menu /></IconButton>}
            {compact ? (
              <>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, flexGrow: 1 }}>
                  <Logo size={26} />
                  <Typography sx={{ fontSize: 16, fontWeight: 650, letterSpacing: '-0.02em' }}>Evently</Typography>
                </Box>
                <IconButton aria-label="Search" onClick={() => setSearching(true)} sx={{ width: 44, height: 44 }}><Search /></IconButton>
                {canEdit && <IconButton aria-label="New event" onClick={() => navigate('/events/new')} sx={{ width: 44, height: 44, bgcolor: 'primary.main', color: '#fff', '&:hover': { bgcolor: 'primary.dark' } }}>
                  <Add />
                </IconButton>}
              </>
            ) : (
              <>
                <ButtonBase
                  onClick={() => setSearching(true)} aria-label="Search"
                  sx={{ flex: '0 1 420px', height: 36, px: 1.5, gap: 1, borderRadius: '999px', justifyContent: 'flex-start', bgcolor: 'var(--fill)', color: 'text.secondary', fontSize: 14 }}
                >
                  <Search fontSize="small" />
                  <Box component="span" sx={{ flexGrow: 1, textAlign: 'left' }}>Search</Box>
                  <Box component="kbd" sx={{ fontFamily: 'inherit', fontSize: 12, px: 0.75, py: '1px', borderRadius: '6px', border: '1px solid var(--divider)' }}>{isMac ? '⌘K' : 'Ctrl K'}</Box>
                </ButtonBase>
                <Box sx={{ flexGrow: 1 }} />
                <UserMenu email={email} name={name} role={role} onLogout={logout} />
              </>
            )}
          </Box>
        </Box>

        <Box component="main" sx={{ flexGrow: 1, minWidth: 0, px: compact ? 2 : 2.5, pt: compact ? 1 : 2.5, pb: 4 }}>
          <Routes>
            <Route path="/events" element={<Events key={search} compact={compact} />} />
            <Route path="/events/new" element={canEdit ? <EventForm /> : <Navigate to="/events" replace />} />
            <Route path="/events/:id" element={<EventDetail />} />
            <Route path="/events/:id/edit" element={canEdit ? <EventForm /> : <Navigate to="/events" replace />} />
            <Route path="/categories" element={<Taxonomy kind="categories" />} />
            <Route path="/topics" element={<Taxonomy kind="topics" />} />
            <Route path="/users" element={role === 'Admin' ? <Users /> : <Navigate to="/events" replace />} />
            <Route path="*" element={<Navigate to="/events" replace />} />
          </Routes>
        </Box>
      </Box>

      <CommandPalette open={searching} onClose={() => setSearching(false)} />

      <Drawer
        open={compact && menu}
        onClose={() => setMenu(false)}
        slotProps={{ paper: { sx: { m: 1.25, height: 'calc(100% - 20px)', width: 280, borderRadius: '16px', p: 1.25, background: 'var(--surface-menu)', backdropFilter: 'var(--blur-material)', boxShadow: 'var(--shadow-dialog)' } } }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, pl: 0.75, pb: 1.5 }}>
          <Logo />
          <Typography sx={{ fontSize: 16, fontWeight: 650, flexGrow: 1 }}>Evently</Typography>
          <IconButton aria-label="Close menu" onClick={() => setMenu(false)} sx={{ width: 44, height: 44 }}><Close /></IconButton>
        </Box>
        {nav}
        <Box sx={{ flexGrow: 1 }} />
        <Box sx={{ borderTop: '1px solid var(--divider)', pt: 1.5, px: 0.75, display: 'flex', flexDirection: 'column', gap: 1.25 }}>
          <Typography variant="body2" color="text.secondary">Signed in as <b style={{ color: 'var(--ink)' }}>{name || email}</b></Typography>
          <Button variant="outlined" fullWidth startIcon={<Logout />} onClick={logout} sx={{ height: 44 }}>Log out</Button>
        </Box>
      </Drawer>
    </Box>
    </AuthContext>
  )
}
