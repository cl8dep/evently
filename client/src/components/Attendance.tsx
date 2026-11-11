import { Alert, Box, Chip, Stack, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material'
import { errText } from '../api'
import { useClearAttendance, useListAttendees, useSetAttendance } from '../api/generated/endpoints'
import type { AttendanceStatus } from '../api/generated/model'
import { useAuth } from '../auth'
import { useState } from 'react'

const OPTIONS: { value: AttendanceStatus; label: string }[] = [
  { value: 'Going', label: 'Going' },
  { value: 'Maybe', label: 'Maybe' },
  { value: 'Declined', label: "Can't go" },
]

export default function Attendance({ eventId }: { eventId: number }) {
  const { email } = useAuth()
  const list = useListAttendees(eventId)
  const set = useSetAttendance()
  const clear = useClearAttendance()
  const [error, setError] = useState('')
  const people = list.data ?? []
  const mine = people.find(a => a.email === email)?.status ?? null

  const change = async (_: unknown, value: AttendanceStatus | null) => {
    setError('')
    try {
      // Clicking the selected button again clears the answer.
      if (value === null) await clear.mutateAsync({ id: eventId })
      else await set.mutateAsync({ id: eventId, data: { status: value } })
    } catch (err) {
      setError(errText(err))
    }
  }

  return (
    <Box>
      <Stack direction="row" sx={{ flexWrap: 'wrap', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
        <Typography sx={{ fontWeight: 600 }}>Are you going?</Typography>
        <ToggleButtonGroup exclusive size="small" value={mine} onChange={change} aria-label="Your attendance">
          {OPTIONS.map(o => <ToggleButton key={o.value} value={o.value} sx={{ px: 2 }}>{o.label}</ToggleButton>)}
        </ToggleButtonGroup>
      </Stack>
      {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}
      {people.length === 0 ? (
        <Typography color="text.secondary" sx={{ fontSize: 14 }}>Nobody has answered yet.</Typography>
      ) : (
        OPTIONS.map(o => {
          const group = people.filter(a => a.status === o.value)
          return group.length > 0 && (
            <Stack key={o.value} direction="row" sx={{ flexWrap: 'wrap', alignItems: 'center', gap: 0.75, mt: 1 }}>
              <Typography sx={{ width: 72, fontSize: 13, color: 'text.secondary' }}>{o.label}</Typography>
              {group.map(a => <Chip key={a.email} size="small" variant="outlined" label={a.name} />)}
            </Stack>
          )
        })
      )}
    </Box>
  )
}
