import { Add, ChevronRight, Download, EventBusy, Search, Upload } from '@mui/icons-material'
import {
  Alert, Autocomplete, Box, Button, Chip, InputAdornment, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom'
import { api, errText, type Category, type EventItem, type Topic } from '../api'
import EventIcon from '../components/EventIcon'
import { useAuth } from '../auth'
import { useRefs } from '../refs'
import { place, shortRange, ticket } from '../format'

const PAGE = 25
const COLS = 'minmax(0, 2.4fr) 140px 150px minmax(0, 1.4fr) 120px'

export default function Events({ compact }: { compact: boolean }) {
  const [items, setItems] = useState<EventItem[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [params] = useSearchParams()
  const [q, setQ] = useState(params.get('q') ?? '')
  const [categoryId, setCategoryId] = useState(params.get('categoryId') ?? '')
  const [topicId, setTopicId] = useState(params.get('topicId') ?? '')
  const [topics, setTopics] = useState<Topic[]>([])
  const [country, setCountry] = useState('')
  const { canEdit } = useAuth()
  const countries = useRefs('countries')
  const [page, setPage] = useState(0)
  const [exported, setExported] = useState(false)
  const [imported, setImported] = useState('')
  const [rev, setRev] = useState(0)
  const fileInput = useRef<HTMLInputElement>(null)
  const [error, setError] = useState('')
  const navigate = useNavigate()

  const query = new URLSearchParams()
  if (q) query.set('q', q)
  if (categoryId) query.set('categoryId', categoryId)
  if (topicId) query.set('topicId', topicId)
  if (country) query.set('country', country)
  const qs = query.toString() ? `?${query}` : ''
  const filtered = qs !== ''

  useEffect(() => { api<Category[]>('/api/categories').then(setCategories, () => {}) }, [rev])
  useEffect(() => { api<Topic[]>('/api/topics').then(setTopics, () => {}) }, [rev])
  useEffect(() => {
    api<EventItem[]>(`/api/events${qs}`).then(r => { setItems(r); setPage(0) }, e => setError(errText(e)))
  }, [qs, rev])

  const clear = () => { setQ(''); setCategoryId(''); setTopicId(''); setCountry('') }
  const count = `${items.length} ${items.length === 1 ? 'event' : 'events'}`
  const pages = Math.max(1, Math.ceil(items.length / PAGE))
  const rows = items.slice(page * PAGE, (page + 1) * PAGE)
  const exportCsv = () => setExported(true)
  const importCsv = async (file?: File) => {
    if (!file) return
    setError(''); setImported(''); setExported(false)
    try {
      const r = await api<{ imported: number; skipped: number }>('/api/events/import', 'POST', file, { 'Content-Type': 'text/csv' })
      setImported(`Imported ${r.imported} ${r.imported === 1 ? 'event' : 'events'}${r.skipped ? `, skipped ${r.skipped} already in the list` : ''}.`)
      setRev(v => v + 1)
    } catch (e) {
      setError(errText(e))
    } finally {
      if (fileInput.current) fileInput.current.value = ''
    }
  }

  const filters = (
    <>
      <TextField
        size="small" placeholder="Search by title or city" value={q} onChange={e => setQ(e.target.value)}
        slotProps={{ htmlInput: { 'aria-label': 'Search events' }, input: { startAdornment: <InputAdornment position="start"><Search fontSize="small" /></InputAdornment> } }}
        sx={{ width: compact ? '100%' : 300 }}
      />
      <TextField
        size="small" select value={categoryId} onChange={e => setCategoryId(e.target.value)}
        slotProps={{ select: { displayEmpty: true }, htmlInput: { 'aria-label': 'Category' } }} sx={{ width: compact ? undefined : 180, flex: compact ? 1 : undefined }}
      >
        <MenuItem value="">All categories</MenuItem>
        {categories.map(c => <MenuItem key={c.id} value={String(c.id)}>{c.name}</MenuItem>)}
      </TextField>
      <TextField
        size="small" select value={topicId} onChange={e => setTopicId(e.target.value)}
        slotProps={{ select: { displayEmpty: true }, htmlInput: { 'aria-label': 'Topic' } }} sx={{ width: compact ? undefined : 160, flex: compact ? 1 : undefined }}
      >
        <MenuItem value="">All topics</MenuItem>
        {topics.map(t => <MenuItem key={t.id} value={String(t.id)}>{t.name}</MenuItem>)}
      </TextField>
      <Autocomplete
        size="small" options={countries} value={countries.find(c => c.code === country) ?? null}
        getOptionLabel={c => c.name} isOptionEqualToValue={(a, b) => a.code === b.code}
        onChange={(_, c) => setCountry(c?.code ?? '')}
        renderInput={p => <TextField {...p} placeholder="Country" slotProps={{ ...p.slotProps, htmlInput: { ...p.slotProps.htmlInput, 'aria-label': 'Country' } }} />}
        sx={{ width: compact ? 160 : 200 }}
      />
    </>
  )

  const empty = (
    <Stack sx={{ alignItems: 'center', textAlign: 'center', py: 6, px: 2 }} spacing={1}>
      <EventBusy sx={{ fontSize: 32, color: 'text.disabled' }} />
      <Typography variant="h6">No events found.</Typography>
      <Typography color="text.secondary">
        {filtered ? 'Nothing matches these filters. Try another search, category or country.' : canEdit ? 'Add the first event to start tracking.' : 'No events have been added yet.'}
      </Typography>
      {filtered && <Button variant="outlined" size="small" onClick={clear}>Clear filters</Button>}
    </Stack>
  )

  return (
    <Box sx={{ maxWidth: 1180 }}>
      <Stack direction="row" sx={{ flexWrap: 'wrap', alignItems: 'flex-end', justifyContent: 'space-between', gap: 1.5, mb: compact ? 1.75 : 3 }}>
        <Box>
          <Typography variant={compact ? 'h3' : 'h2'}>Events</Typography>
          <Typography color="text.secondary" sx={{ mt: compact ? 0.25 : 0.75, fontSize: compact ? 13 : 14 }}>
            {compact ? count : 'Conferences, meetups and trade shows the team may attend.'}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          {canEdit && (
            <>
              <input ref={fileInput} type="file" accept=".csv,text/csv" hidden onChange={e => importCsv(e.target.files?.[0])} />
              <Button variant="outlined" size={compact ? 'small' : 'medium'} startIcon={<Upload />} onClick={() => fileInput.current?.click()}>Import CSV</Button>
            </>
          )}
          <Button component="a" href={`/api/events/export.csv${qs}`} onClick={exportCsv} variant="outlined" size={compact ? 'small' : 'medium'} startIcon={<Download />}>Export CSV</Button>
          {!compact && canEdit && <Button component={RouterLink} to="/events/new" variant="contained" startIcon={<Add />}>New event</Button>}
        </Stack>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {imported && <Alert severity="success" onClose={() => setImported('')} sx={{ mb: 2 }}>{imported}</Alert>}
      {exported && (
        <Alert severity="success" onClose={() => setExported(false)} sx={{ mb: 2 }}>
          Exported {count} to events-{new Date().toISOString().slice(0, 10)}.csv.
        </Alert>
      )}

      {compact && <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, mb: 1.75 }}>{filters}</Stack>}

      <Paper sx={{ overflow: 'hidden' }}>
        {!compact && (
          <Stack direction="row" sx={{ flexWrap: 'wrap', alignItems: 'center', gap: 1, px: 2.5, py: 1.75, borderBottom: '1px solid var(--divider)' }}>
            {filters}
            {filtered && <Button size="small" onClick={clear}>Clear filters</Button>}
            <Box sx={{ flex: 1 }} />
            <Typography variant="body2" color="text.secondary" sx={{ fontSize: 13 }}>{count}</Typography>
          </Stack>
        )}

        {compact ? rows.map(e => (
          <Box
            key={e.id} component={RouterLink} to={`/events/${e.id}`}
            sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 1.5, pl: 2, pr: 1.5, borderBottom: '1px solid var(--divider)', textDecoration: 'none', color: 'inherit', minHeight: 44 }}
          >
            <Stack spacing={0.6} sx={{ flex: 1, minWidth: 0 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
                <EventIcon id={e.id} title={e.title} iconUpdatedAt={e.iconUpdatedAt} />
                <Typography noWrap sx={{ fontWeight: 600 }}>{e.title}</Typography>
              </Stack>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', fontSize: 13, color: 'text.secondary' }}>
                <Chip variant="outlined" size="small" label={e.category?.name} />
                <span>{shortRange(e.startDate, e.endDate)}</span>
              </Stack>
              <Typography sx={{ fontSize: 13 }} color="text.secondary">
                {place(e.location)}{e.ticketCost != null && <> · <b style={{ color: 'var(--ink)' }}>{ticket(e.ticketCost, e.currency)}</b></>}
              </Typography>
            </Stack>
            <ChevronRight sx={{ color: 'text.disabled' }} />
          </Box>
        )) : (
          <Box sx={{ overflowX: 'auto' }}>
            <Box sx={{ minWidth: 780 }}>
              <Box sx={{ display: 'grid', gridTemplateColumns: COLS, columnGap: 2, alignItems: 'center', px: 2.5, py: 1.25, borderBottom: '1px solid var(--divider)', fontSize: 12, fontWeight: 600, color: 'text.secondary' }}>
                <span>Title</span><span>Category</span><span>Dates</span><span>Location</span><span style={{ textAlign: 'right' }}>Ticket</span>
              </Box>
              {rows.map(e => (
                <Box
                  key={e.id} component="a" href={`/events/${e.id}`}
                  onClick={ev => { if (!ev.metaKey && !ev.ctrlKey) { ev.preventDefault(); navigate(`/events/${e.id}`) } }}
                  sx={{
                    display: 'grid', gridTemplateColumns: COLS, columnGap: 2, alignItems: 'center', minHeight: 52, px: 2.5, py: 0.75,
                    borderBottom: '1px solid var(--divider)', textDecoration: 'none', color: 'inherit', '&:hover': { bgcolor: 'action.hover' },
                    '&:focus-visible': { outline: 'var(--focus-ring)', outlineOffset: '-3px', borderRadius: '12px' },
                  }}
                >
                  <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', minWidth: 0 }}>
                    <EventIcon id={e.id} title={e.title} iconUpdatedAt={e.iconUpdatedAt} />
                    <Typography noWrap sx={{ fontWeight: 600 }}>{e.title}</Typography>
                  </Stack>
                  <Box><Chip variant="outlined" size="small" label={e.category?.name} /></Box>
                  <Typography noWrap color="text.secondary">{shortRange(e.startDate, e.endDate)}</Typography>
                  <Typography noWrap>
                    {e.location.city}
                    {e.location.city && e.location.country && <span style={{ color: 'var(--ink-2)' }}>, </span>}
                    <span style={{ color: e.location.city ? 'var(--ink-2)' : undefined }}>{e.location.country}</span>
                  </Typography>
                  <Typography noWrap sx={{ textAlign: 'right', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{ticket(e.ticketCost, e.currency)}</Typography>
                </Box>
              ))}
            </Box>
          </Box>
        )}

        {items.length === 0 && empty}

        {items.length > 0 && !compact && (
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', px: 2.5, py: 1.25 }}>
            <Typography color="text.secondary">{page * PAGE + 1}&ndash;{page * PAGE + rows.length} of {items.length}</Typography>
            <Stack direction="row" spacing={0.75}>
              <Button variant="outlined" size="small" disabled={page === 0} onClick={() => setPage(page - 1)}>Previous</Button>
              <Button variant="outlined" size="small" disabled={page >= pages - 1} onClick={() => setPage(page + 1)}>Next</Button>
            </Stack>
          </Stack>
        )}
      </Paper>
    </Box>
  )
}
