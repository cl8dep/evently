import { Delete, Edit, OpenInNew, Refresh, WarningAmber } from '@mui/icons-material'
import { Alert, Box, Button, Chip, Link, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { errText } from '../api'
import { useDeleteEvent, useGetEvent, useRefreshEventIcon } from '../api/generated/endpoints'
import Crumbs from '../components/Crumbs'
import { useAuth } from '../auth'
import PlaceMap from '../components/PlaceMap'
import EventIcon from '../components/EventIcon'
import { day, longRange, place, shortRange, ticket } from '../format'

export default function EventDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { canEdit } = useAuth()
  const eventQuery = useGetEvent(Number(id))
  const deleteEvent = useDeleteEvent()
  const refreshIcon = useRefreshEventIcon()
  const e = eventQuery.data
  const [actionError, setError] = useState('')
  const error = actionError || (eventQuery.error ? errText(eventQuery.error) : '')
  const [confirming, setConfirming] = useState(false)


  const remove = async () => {
    try {
      await deleteEvent.mutateAsync({ id: Number(id) })
      navigate('/events')
    } catch (err) {
      setError(errText(err))
    }
  }

  const refresh = async () => {
    setError('')
    try {
      await refreshIcon.mutateAsync({ id: Number(id) })
    } catch (err) {
      setError(errText(err))
    }
  }

  if (!e) return error ? <Alert severity="error">{error}</Alert> : null

  const l = e.location
  const dates = longRange(e.startDate, e.endDate)
  const hasMap = l.latitude != null && l.longitude != null
  const host = e.website ? new URL(e.website).host : null
  const row = (label: string, value: React.ReactNode) => value ? (
    <>
      <Box component="dt" sx={{ py: 1.6, borderBottom: '1px solid var(--divider)', fontSize: 13, fontWeight: 500, color: 'text.secondary' }}>{label}</Box>
      <Box component="dd" sx={{ m: 0, py: 1.4, borderBottom: '1px solid var(--divider)', display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>{value}</Box>
    </>
  ) : null

  return (
    <Box sx={{ maxWidth: hasMap ? 1100 : 880 }}>
      <Crumbs items={[{ label: 'Events', to: '/events' }]} current={e.title} />

      <Stack direction="row" sx={{ flexWrap: { xs: 'wrap', md: 'nowrap' }, alignItems: 'flex-start', justifyContent: 'space-between', gap: 1.5, mb: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center', minWidth: 0, flex: { xs: '1 1 100%', md: '1 1 0' } }}>
          <Box sx={{ width: 56, height: 56, flex: '0 0 auto', borderRadius: '14px', bgcolor: 'background.paper', boxShadow: 'var(--shadow-raised)', display: 'grid', placeItems: 'center' }}>
            <EventIcon id={e.id} title={e.title} iconUpdatedAt={e.iconUpdatedAt} size={32} />
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h2" sx={{ overflowWrap: 'anywhere' }}>{e.title}</Typography>
            <Typography color="text.secondary" sx={{ mt: 0.75 }}>
              {[place(l), shortRange(e.startDate, e.endDate)].filter(Boolean).join(' · ')}
            </Typography>
          </Box>
        </Stack>
        {canEdit && (
        <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
          <Button component={RouterLink} to={`/events/${e.id}/edit`} variant="outlined" startIcon={<Edit />}>Edit</Button>
          <Button variant="outlined" color="error" startIcon={<Delete />} onClick={() => setConfirming(true)}>Delete</Button>
        </Stack>
        )}
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {confirming && (
        <Alert
          severity="warning" icon={<WarningAmber />} sx={{ mb: 2, alignItems: 'center' }}
          action={
            <Stack direction="row" spacing={0.75}>
              <Button size="small" onClick={() => setConfirming(false)}>Cancel</Button>
              <Button size="small" variant="contained" color="error" onClick={remove}>Delete event</Button>
            </Stack>
          }
        >
          <Typography sx={{ fontWeight: 600 }}>Delete {e.title}?</Typography>
          This removes the event for everyone on the team. It can&apos;t be undone.
        </Alert>
      )}

      <Paper sx={{ p: 3, display: 'grid', gap: 3, alignItems: 'start', gridTemplateColumns: { xs: '1fr', md: hasMap ? 'minmax(0, 1fr) 340px' : '1fr' } }}>
        <Box sx={{ minWidth: 0 }}>
        {e.description && <Typography sx={{ mb: 2.5, fontSize: 15, lineHeight: 1.6, maxWidth: 680, whiteSpace: 'pre-wrap' }}>{e.description}</Typography>}
        <Box component="dl" sx={{ m: 0, display: 'grid', gridTemplateColumns: { xs: '110px minmax(0, 1fr)', sm: '180px minmax(0, 1fr)' }, borderTop: '1px solid var(--divider)' }}>
          {row('Category', e.category && <Chip variant="outlined" size="small" label={e.category.name} />)}
          {row('Topics', e.topics.length > 0 && e.topics.map(t => <Chip key={t.id} variant="outlined" size="small" label={t.name} />))
          }
          {row('Dates', <span>{dates.text}{dates.days > 1 && <Box component="span" sx={{ color: 'text.secondary' }}> &middot; {dates.days} days</Box>}</span>)}
          {row('Website', e.website && (
            <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
              <Link href={e.website} target="_blank" rel="noopener noreferrer" underline="always" sx={{ textDecorationColor: 'var(--gray-3)', textUnderlineOffset: 3, fontWeight: 400 }}>{host}</Link>
              <OpenInNew sx={{ fontSize: 16, color: 'text.disabled' }} />
            </Stack>
          ))}
          {e.website && row('Image', (
            <>
              <EventIcon id={e.id} title={e.title} iconUpdatedAt={e.iconUpdatedAt} />
              <span>
                {e.iconUpdatedAt ? 'Site favicon, fetched automatically' : 'No icon found on the site'}
                {e.iconUpdatedAt && <Box component="span" sx={{ color: 'text.secondary' }}> &middot; updated {day(e.iconUpdatedAt)}</Box>}
              </span>
              {canEdit && <Button size="small" startIcon={<Refresh />} disabled={refreshIcon.isPending} onClick={refresh}>{refreshIcon.isPending ? 'Fetching...' : 'Refresh'}</Button>}
            </>
          ))}
          {row('Ticket', e.ticketCost != null && (
            <span>
              <b style={{ fontVariantNumeric: 'tabular-nums' }}>{ticket(e.ticketCost, e.currency)}</b>
              <Box component="span" sx={{ color: 'text.secondary' }}> &middot; estimated, per person</Box>
            </span>
          ))}
          {row('Venue', l.venue)}
          {row('Address', l.address || [l.city, l.region, l.country].filter(Boolean).join(', '))}
          {row('Added by', <span>{e.createdBy}<Box component="span" sx={{ color: 'text.secondary' }}> &middot; {day(e.createdAt)}</Box></span>)}
        </Box>
        </Box>
        {hasMap && <PlaceMap lat={l.latitude!} lng={l.longitude!} title={e.title} height={340} sx={{ position: { md: 'sticky' }, top: { md: 76 } }} />}
      </Paper>
    </Box>
  )
}
