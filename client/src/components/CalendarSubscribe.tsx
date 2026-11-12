import { ContentCopy, Event as EventIcon } from '@mui/icons-material'
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Stack, Switch, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { errText } from '../api'
import { useGetCalendarFeed, useResetCalendarFeed } from '../api/generated/endpoints'

export default function CalendarSubscribe({ size }: { size?: 'small' | 'medium' }) {
  const [open, setOpen] = useState(false)
  const [mine, setMine] = useState(false)
  const [note, setNote] = useState('')
  const feed = useGetCalendarFeed({ query: { enabled: open } })
  const reset = useResetCalendarFeed()
  const url = feed.data ? `${location.origin}${feed.data.path}${mine ? '?mine=true' : ''}` : ''

  const copy = async () => {
    await navigator.clipboard.writeText(url)
    setNote('Copied.')
  }
  const rotate = async () => {
    try {
      await reset.mutateAsync()
      setNote('Link replaced. The old one no longer works.')
    } catch (err) {
      setNote(errText(err))
    }
  }

  return (
    <>
      <Button variant="outlined" size={size} startIcon={<EventIcon />} onClick={() => setOpen(true)}>Subscribe</Button>
      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Subscribe in your calendar</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" sx={{ mb: 2, fontSize: 14 }}>
            Your calendar app keeps this feed up to date. In Google Calendar choose Other calendars, From URL. Anyone with the link can read the events, so keep it private.
          </Typography>
          <FormControlLabel control={<Switch checked={mine} onChange={e => setMine(e.target.checked)} />} label="Only events I'm going to or might attend" sx={{ mb: 1.5 }} />
          <TextField fullWidth value={url} slotProps={{ input: { readOnly: true } }} onFocus={e => e.target.select()} aria-label="Feed URL" />
          {note && <Alert severity="info" sx={{ mt: 1.5 }}>{note}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button color="error" onClick={rotate} disabled={reset.isPending} sx={{ mr: 'auto' }}>Replace link</Button>
          <Stack direction="row" spacing={1}>
            <Button component="a" href={url.replace(/^https?:/, 'webcal:')} disabled={!url}>Open in calendar app</Button>
            <Button variant="contained" startIcon={<ContentCopy />} onClick={copy} disabled={!url}>Copy link</Button>
          </Stack>
        </DialogActions>
      </Dialog>
    </>
  )
}
