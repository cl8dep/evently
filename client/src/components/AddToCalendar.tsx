import { Event as EventIcon, Download, OpenInNew } from '@mui/icons-material'
import { Button, ListItemIcon, ListItemText, Menu, MenuItem } from '@mui/material'
import { useState } from 'react'

type Props = { id: number; title: string; start: string; end?: string | null; details?: string | null; where?: string }

const compact = (iso: string, plusDays = 0) => {
  const d = new Date(`${iso}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + plusDays)
  return d.toISOString().slice(0, 10).replaceAll('-', '')
}

export default function AddToCalendar({ id, title, start, end, details, where }: Props) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const google = new URL('https://calendar.google.com/calendar/render')
  google.searchParams.set('action', 'TEMPLATE')
  google.searchParams.set('text', title)
  // All-day events end on the day after their last day.
  google.searchParams.set('dates', `${compact(start)}/${compact(end ?? start, 1)}`)
  if (details) google.searchParams.set('details', details)
  if (where) google.searchParams.set('location', where)
  return (
    <>
      <Button variant="outlined" startIcon={<EventIcon />} onClick={e => setAnchor(e.currentTarget)}>Add to calendar</Button>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <MenuItem component="a" href={google.toString()} target="_blank" rel="noopener noreferrer" onClick={() => setAnchor(null)}>
          <ListItemIcon><OpenInNew fontSize="small" /></ListItemIcon>
          <ListItemText>Google Calendar</ListItemText>
        </MenuItem>
        <MenuItem component="a" href={`/api/events/${id}/ics`} onClick={() => setAnchor(null)}>
          <ListItemIcon><Download fontSize="small" /></ListItemIcon>
          <ListItemText>Download .ics (Apple, Outlook)</ListItemText>
        </MenuItem>
      </Menu>
    </>
  )
}
