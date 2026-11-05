import { Save } from '@mui/icons-material'
import { Alert, Autocomplete, Box, Button, InputAdornment, Paper, Stack, TextField, Typography } from '@mui/material'
import { DatePicker } from '@mui/x-date-pickers/DatePicker'
import dayjs from 'dayjs'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { api, errText, type Category, type EventItem, type Topic } from '../api'
import Crumbs from '../components/Crumbs'
import PlaceMap from '../components/PlaceMap'
import PlaceSearch from '../components/PlaceSearch'
import { usePlacesEnabled } from '../maps'
import { useRefs, type Ref } from '../refs'

type Form = {
  title: string; description: string; categoryId: string; website: string; ticketCost: string; currency: string
  startDate: string; endDate: string; venue: string; address: string; city: string; region: string; country: string
  latitude: string; longitude: string; topicIds: number[]
}

const empty: Form = {
  title: '', description: '', categoryId: '', website: '', ticketCost: '', currency: 'EUR', startDate: '', endDate: '',
  venue: '', address: '', city: '', region: '', country: '', latitude: '', longitude: '', topicIds: [],
}

const num = (s: string) => (s === '' ? null : Number(s))
const stripScheme = (s: string) => s.trim().replace(/^https?:\/\//i, '')

export default function EventForm() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [f, setF] = useState<Form>(empty)
  const [title, setTitle] = useState('')
  const [categories, setCategories] = useState<Category[]>([])
  const [topics, setTopics] = useState<Topic[]>([])
  const countries = useRefs('countries')
  const currencies = useRefs('currencies')
  const [error, setError] = useState('')
  const [tried, setTried] = useState(false)
  const placesEnabled = usePlacesEnabled()
  const [saving, setSaving] = useState(false)
  // One key per form: a retried or double-clicked create returns the same event instead of a duplicate.
  const idempotencyKey = useRef(crypto.randomUUID())

  useEffect(() => { api<Category[]>('/api/categories').then(setCategories, e => setError(errText(e))) }, [])
  useEffect(() => { api<Topic[]>('/api/topics').then(setTopics, e => setError(errText(e))) }, [])
  useEffect(() => {
    if (!id) return
    api<EventItem>(`/api/events/${id}`).then(e => {
      setTitle(e.title)
      setF({
        title: e.title, description: e.description ?? '', categoryId: String(e.categoryId), website: stripScheme(e.website ?? ''),
        ticketCost: e.ticketCost?.toString() ?? '', currency: e.currency ?? 'EUR', startDate: e.startDate, endDate: e.endDate ?? '',
        venue: e.location.venue ?? '', address: e.location.address ?? '', city: e.location.city ?? '',
        region: e.location.region ?? '', country: e.location.country ?? '',
        latitude: e.location.latitude?.toString() ?? '', longitude: e.location.longitude?.toString() ?? '',
        topicIds: e.topics.map(t => t.id),
      })
    }, e => setError(errText(e)))
  }, [id])

  const titleError = tried && !f.title.trim()
  const categoryError = tried && !f.categoryId
  const dateError = tried && !f.startDate
  const endError = !!f.startDate && !!f.endDate && f.endDate < f.startDate
  const invalid = !f.title.trim() || !f.categoryId || !f.startDate || endError

  const set = (k: keyof Form) => (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, [k]: e.target.value })
  const text = (k: keyof Form, label: string, extra: object = {}) => (
    <TextField label={label} value={f[k]} onChange={set(k)} fullWidth slotProps={{ inputLabel: { shrink: true } }} {...extra} />
  )

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setTried(true)
    if (invalid || saving) return
    const website = f.website.trim()
    const body = {
      title: f.title, description: f.description, categoryId: Number(f.categoryId), topicIds: f.topicIds,
      website: website && !/^https?:\/\//i.test(website) ? `https://${website}` : website,
      ticketCost: num(f.ticketCost), currency: f.currency, startDate: f.startDate, endDate: f.endDate || null,
      location: {
        venue: f.venue, address: f.address, city: f.city, region: f.region, country: f.country,
        latitude: num(f.latitude), longitude: num(f.longitude),
      },
    }
    setSaving(true)
    try {
      const saved = id
        ? await api<EventItem>(`/api/events/${id}`, 'PUT', body)
        : await api<EventItem>('/api/events', 'POST', body, { 'Idempotency-Key': idempotencyKey.current })
      navigate(`/events/${saved.id}`)
    } catch (err) {
      setError(errText(err))
      setSaving(false)
    }
  }

  const refPicker = (label: string, options: Ref[], value: string, onChange: (code: string) => void, text: (r: Ref) => string) => (
    <Autocomplete
      options={options} value={options.find(o => o.code === value) ?? null}
      getOptionLabel={text} isOptionEqualToValue={(a, b) => a.code === b.code}
      filterOptions={(opts, { inputValue }) => opts.filter(o => `${o.code} ${o.name}`.toLowerCase().includes(inputValue.toLowerCase()))}
      onChange={(_, o) => onChange(o?.code ?? '')}
      renderInput={p => <TextField {...p} label={label} slotProps={{ ...p.slotProps, inputLabel: { ...p.slotProps.inputLabel, shrink: true } }} />}
    />
  )

  const datePicker = (k: 'startDate' | 'endDate', label: string, { minDate, ...field }: { required?: boolean; error?: boolean; helperText?: string; minDate?: dayjs.Dayjs }) => (
    <DatePicker
      label={label} format="MMM D, YYYY" minDate={minDate} value={f[k] ? dayjs(f[k]) : null}
      onChange={d => setF(p => ({ ...p, [k]: d?.isValid() ? d.format('YYYY-MM-DD') : '' }))}
      slotProps={{ textField: { ...field, fullWidth: true, slotProps: { inputLabel: { shrink: true } } }, field: { clearable: true } }}
    />
  )

  const grid = { display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: '18px 16px' }
  const full = { gridColumn: '1 / -1' }
  const section = (heading: string, hint: string, children: React.ReactNode) => (
    <Box component="section" sx={{ p: 3, borderBottom: '1px solid var(--divider)' }}>
      <Typography variant="subtitle1">{heading}</Typography>
      <Typography color="text.secondary" sx={{ fontSize: 13, mt: 0.5, mb: 2.25 }}>{hint}</Typography>
      <Box sx={grid}>{children}</Box>
    </Box>
  )

  return (
    <Box sx={{ maxWidth: 760 }}>
      <Crumbs
        items={id ? [{ label: 'Events', to: '/events' }, { label: title, to: `/events/${id}` }] : [{ label: 'Events', to: '/events' }]}
        current={id ? 'Edit' : 'New event'}
      />
      <Typography variant="h2" sx={{ mb: 3 }}>{id ? 'Edit event' : 'New event'}</Typography>

      <Paper component="form" onSubmit={submit} noValidate>
        {section('Details', 'What the event is and what it costs to attend.', <>
          {text('title', 'Title', { placeholder: 'e.g. Atlantic Web Summit', required: true, error: titleError, helperText: titleError ? 'Enter a title.' : undefined, sx: full })}
          {text('description', 'Description', { placeholder: 'Why it is worth attending, who will be there. Leave empty to use the website description.', multiline: true, minRows: 4, sx: full, slotProps: { inputLabel: { shrink: true }, htmlInput: { maxLength: 600 } } })}
          <Autocomplete
            options={categories} value={categories.find(c => String(c.id) === f.categoryId) ?? null}
            getOptionLabel={c => c.name} isOptionEqualToValue={(a, b) => a.id === b.id}
            filterOptions={(opts, { inputValue }) => {
              const q = inputValue.trim()
              const shown = opts.filter(c => c.name.toLowerCase().includes(q.toLowerCase()))
              const exact = opts.some(c => c.name.toLowerCase() === q.toLowerCase())
              return q && !exact ? [...shown, { id: -1, name: q }] : shown
            }}
            renderOption={(props, c) => <li {...props} key={c.id}>{c.id === -1 ? <>Add &quot;<b>{c.name}</b>&quot;</> : c.name}</li>}
            onChange={async (_, c) => {
              if (!c) return setF({ ...f, categoryId: '' })
              if (c.id !== -1) return setF({ ...f, categoryId: String(c.id) })
              try {
                const created = await api<Category>('/api/categories', 'POST', { name: c.name })
                setCategories(prev => [...prev, created].sort((x, y) => x.name.localeCompare(y.name)))
                setF(p => ({ ...p, categoryId: String(created.id) }))
              } catch (err) {
                setError(errText(err))
              }
            }}
            renderInput={p => (
              <TextField {...p} label="Category" required placeholder="Choose or type to add" error={categoryError}
                helperText={categoryError ? 'Choose a category.' : undefined}
                slotProps={{ ...p.slotProps, inputLabel: { ...p.slotProps.inputLabel, shrink: true } }} />
            )}
          />
          {text('website', 'Website', {
            placeholder: 'example.com', onChange: (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, website: stripScheme(e.target.value) }), helperText: "The site's favicon becomes the event image.",
            slotProps: { inputLabel: { shrink: true }, input: { startAdornment: <InputAdornment position="start">https://</InputAdornment> } },
          })}
          <Autocomplete
            multiple sx={{ gridColumn: '1 / -1' }} options={topics} value={topics.filter(t => f.topicIds.includes(t.id))}
            getOptionLabel={t => t.name} isOptionEqualToValue={(a, b) => a.id === b.id} filterSelectedOptions
            filterOptions={(opts, { inputValue }) => {
              const q = inputValue.trim()
              const shown = opts.filter(t => t.name.toLowerCase().includes(q.toLowerCase()))
              const exists = topics.some(t => t.name.toLowerCase() === q.toLowerCase())
              return q && !exists ? [...shown, { id: -1, name: q }] : shown
            }}
            renderOption={(props, t) => <li {...props} key={t.id}>{t.id === -1 ? <>Add &quot;<b>{t.name}</b>&quot;</> : t.name}</li>}
            onChange={async (_, picked) => {
              const add = picked.find(t => t.id === -1)
              const ids = picked.filter(t => t.id !== -1).map(t => t.id)
              if (!add) return setF({ ...f, topicIds: ids })
              try {
                const created = await api<Topic>('/api/topics', 'POST', { name: add.name })
                setTopics(prev => [...prev, created].sort((x, y) => x.name.localeCompare(y.name)))
                setF(p => ({ ...p, topicIds: [...ids, created.id] }))
              } catch (err) {
                setError(errText(err))
              }
            }}
            renderInput={p => (
              <TextField {...p} label="Topics" placeholder="Choose or type to add" helperText="Optional. An event can have several."
                slotProps={{ ...p.slotProps, inputLabel: { ...p.slotProps.inputLabel, shrink: true } }} />
            )}
          />
          {text('ticketCost', 'Estimated ticket cost', { placeholder: '0', helperText: 'Per person. Leave 0 for free events.', slotProps: { inputLabel: { shrink: true }, htmlInput: { inputMode: 'decimal', min: 0, step: '0.01' } }, type: 'number' })}
          {refPicker('Currency', currencies, f.currency, v => setF({ ...f, currency: v }), c => `${c.code} · ${c.name}`)}
          {datePicker('startDate', 'Start date', { required: true, error: dateError, helperText: dateError ? 'Choose a start date.' : undefined })}
          {datePicker('endDate', 'End date', { error: endError, helperText: endError ? 'End date must be on or after the start date.' : undefined, minDate: f.startDate ? dayjs(f.startDate) : undefined })}
        </>)}

        {section('Location', placesEnabled ? 'Search the venue. Address, city and country are filled in for you.' : 'City and country drive filtering. Coordinates are optional.',
          placesEnabled === null ? null : placesEnabled ? (<>
            <PlaceSearch
              venue={f.venue} summary={f.address || [f.city, f.region, f.country].filter(Boolean).join(', ')} onError={setError}
              onPick={l => setF(p => ({ ...p, ...Object.fromEntries(Object.entries(l).map(([k, v]) => [k, v == null ? '' : String(v)])) }))}
              onClear={() => setF(p => ({ ...p, venue: '', address: '', city: '', region: '', country: '', latitude: '', longitude: '' }))}
            />
            {f.latitude !== '' && f.longitude !== '' && (
              <PlaceMap lat={Number(f.latitude)} lng={Number(f.longitude)} title={f.venue || f.title || 'event'} height={240} sx={{ gridColumn: '1 / -1' }} />
            )}
          </>) : (<>
            {text('venue', 'Venue', { placeholder: 'e.g. Riverside Congress Centre', sx: full })}
            {text('address', 'Address', { placeholder: 'Street and number, postal code', sx: full })}
            {text('city', 'City')}
            {text('region', 'Region', { placeholder: 'State, province or district' })}
            <Box sx={{ ...full, display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, minmax(0, 1fr))' }, gap: '18px 16px' }}>
              {refPicker('Country', countries, f.country, v => setF({ ...f, country: v }), c => c.name)}
              {text('latitude', 'Latitude', { placeholder: '38.7684', type: 'number', slotProps: { inputLabel: { shrink: true }, htmlInput: { step: 'any' } } })}
              {text('longitude', 'Longitude', { placeholder: '-9.0940', type: 'number', slotProps: { inputLabel: { shrink: true }, htmlInput: { step: 'any' } } })}
            </Box>
          </>))}

        <Stack spacing={1.5} sx={{ px: 3, py: 2 }}>
          {(error || (tried && invalid)) && <Alert severity="error">{error || 'Fix the highlighted field, then save.'}</Alert>}
          <Stack direction="row" spacing={1}>
            <Button type="submit" variant="contained" loading={saving} loadingPosition="start" startIcon={<Save />}>
              {saving ? 'Saving...' : id ? 'Save changes' : 'Save event'}
            </Button>
            <Button component={RouterLink} to={id ? `/events/${id}` : '/events'} disabled={saving}>Cancel</Button>
          </Stack>
        </Stack>
      </Paper>
    </Box>
  )
}
