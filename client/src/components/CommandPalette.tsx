import { Add, CalendarMonth, PeopleOutlined, Person, Search, Sell, Tag } from '@mui/icons-material'
import {
  Box, Chip, Dialog, InputAdornment, List, ListItemButton, ListItemIcon, ListItemText, ListSubheader, TextField, Typography,
} from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { useAuth } from '../auth'
import { place, shortRange } from '../format'
import EventIcon from './EventIcon'

type Results = {
  events: { id: number; title: string; startDate: string; city?: string | null; country?: string | null; iconUpdatedAt?: string | null }[]
  categories: { id: number; name: string }[]
  topics: { id: number; name: string }[]
  users: { id: string; firstName: string; lastName: string; email: string }[]
}

type Item = { key: string; group: string; label: string; secondary?: string; icon: ReactNode; to: string }

const none: Results = { events: [], categories: [], topics: [], users: [] }

export default function CommandPalette({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate()
  const { canEdit, isAdmin } = useAuth()
  const [q, setQ] = useState('')
  const [found, setFound] = useState<Results>(none)
  const [loading, setLoading] = useState(false)
  const [active, setActive] = useState(0)
  const listRef = useRef<HTMLUListElement>(null)
  const term = q.trim()

  useEffect(() => {
    if (!open) return
    setQ('')
    setFound(none)
  }, [open])

  useEffect(() => {
    if (term.length < 2) { setFound(none); setLoading(false); return }
    let stale = false
    setLoading(true)
    const t = setTimeout(() => {
      api<Results>(`/api/search?q=${encodeURIComponent(term)}`).then(
        r => { if (!stale) { setFound(r); setLoading(false) } },
        () => { if (!stale) { setFound(none); setLoading(false) } },
      )
    }, 200)
    return () => { stale = true; clearTimeout(t) }
  }, [term])

  const items = useMemo<Item[]>(() => {
    const pages: Item[] = [
      { key: 'p-events', group: 'Go to', label: 'Events', icon: <CalendarMonth fontSize="small" />, to: '/events' },
      ...(canEdit ? [{ key: 'p-new', group: 'Go to', label: 'New event', icon: <Add fontSize="small" />, to: '/events/new' }] : []),
      { key: 'p-cat', group: 'Go to', label: 'Categories', icon: <Sell fontSize="small" />, to: '/categories' },
      { key: 'p-top', group: 'Go to', label: 'Topics', icon: <Tag fontSize="small" />, to: '/topics' },
      ...(isAdmin ? [{ key: 'p-users', group: 'Go to', label: 'Users', icon: <PeopleOutlined fontSize="small" />, to: '/users' }] : []),
    ].filter(p => p.label.toLowerCase().includes(term.toLowerCase()))
    return [
      ...found.events.map(e => ({
        key: `e${e.id}`, group: 'Events', label: e.title, secondary: [place(e), shortRange(e.startDate, null)].filter(Boolean).join(' · '),
        icon: <EventIcon id={e.id} title={e.title} iconUpdatedAt={e.iconUpdatedAt} />, to: `/events/${e.id}`,
      })),
      ...found.categories.map(c => ({ key: `c${c.id}`, group: 'Categories', label: c.name, secondary: 'Show its events', icon: <Sell fontSize="small" />, to: `/events?categoryId=${c.id}` })),
      ...found.topics.map(t => ({ key: `t${t.id}`, group: 'Topics', label: t.name, secondary: 'Show its events', icon: <Tag fontSize="small" />, to: `/events?topicId=${t.id}` })),
      ...found.users.map(u => ({
        key: `u${u.id}`, group: 'Users', label: [u.firstName, u.lastName].filter(Boolean).join(' ') || u.email,
        secondary: u.email, icon: <Person fontSize="small" />, to: '/users',
      })),
      ...pages,
    ]
  }, [found, term, canEdit, isAdmin])

  useEffect(() => { setActive(0) }, [items])
  useEffect(() => { listRef.current?.querySelector('[data-active="true"]')?.scrollIntoView({ block: 'nearest' }) }, [active])

  const go = (item: Item | undefined) => {
    if (!item) return
    onClose()
    navigate(item.to)
  }

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') { e.preventDefault(); setActive(a => (items.length ? (a + 1) % items.length : 0)) }
    else if (e.key === 'ArrowUp') { e.preventDefault(); setActive(a => (items.length ? (a - 1 + items.length) % items.length : 0)) }
    else if (e.key === 'Enter') { e.preventDefault(); go(items[active]) }
  }

  return (
    <Dialog
      open={open} onClose={onClose} fullWidth maxWidth="sm"
      sx={{ '& .MuiDialog-container': { alignItems: 'flex-start' } }}
      slotProps={{ paper: { sx: { mt: '12vh', borderRadius: '16px', background: 'var(--surface-menu)', backdropFilter: 'var(--blur-material)', boxShadow: 'var(--shadow-dialog)', overflow: 'hidden' } } }}
    >
      <TextField
        autoFocus fullWidth variant="standard" placeholder="Search events, categories, topics..." value={q}
        onChange={e => setQ(e.target.value)} onKeyDown={onKeyDown}
        slotProps={{
          htmlInput: { 'aria-label': 'Search', role: 'combobox', 'aria-expanded': true, 'aria-controls': 'palette-list' },
          input: {
            disableUnderline: true,
            startAdornment: <InputAdornment position="start"><Search /></InputAdornment>,
            endAdornment: <Chip size="small" variant="outlined" label="esc" sx={{ height: 22, fontSize: 11 }} />,
            sx: { px: 2, py: 1.5, fontSize: 16 },
          },
        }}
      />
      <Box sx={{ borderTop: '1px solid var(--divider)', maxHeight: '50vh', overflowY: 'auto' }}>
        <List id="palette-list" ref={listRef} role="listbox" dense disablePadding sx={{ py: 0.5 }}>
          {items.map((item, i) => (
            <Box key={item.key} component="li" sx={{ listStyle: 'none' }}>
              {(i === 0 || items[i - 1].group !== item.group) && (
                <ListSubheader disableSticky sx={{ lineHeight: '28px', background: 'transparent', fontSize: 12 }}>{item.group}</ListSubheader>
              )}
              <ListItemButton
                role="option" aria-selected={i === active} data-active={i === active} selected={i === active}
                onClick={() => go(item)} onMouseMove={() => setActive(i)} sx={{ mx: 0.75, borderRadius: '10px', minHeight: 44 }}
              >
                <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} secondary={item.secondary} slotProps={{ primary: { noWrap: true }, secondary: { noWrap: true } }} />
              </ListItemButton>
            </Box>
          ))}
          {items.length === 0 && (
            <Typography color="text.secondary" sx={{ px: 2.5, py: 2 }}>
              {loading ? 'Searching...' : term.length < 2 ? 'Type at least 2 characters.' : `Nothing matches "${term}".`}
            </Typography>
          )}
        </List>
      </Box>
    </Dialog>
  )
}
