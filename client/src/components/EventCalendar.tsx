import { ChevronLeft, ChevronRight } from '@mui/icons-material'
import { Box, Button, IconButton, Stack, Typography } from '@mui/material'
import dayjs from 'dayjs'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { EventItem } from '../api'

const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const SHOWN = 3
const iso = (d: dayjs.Dayjs) => d.format('YYYY-MM-DD')

export default function EventCalendar({ events }: { events: EventItem[] }) {
  const navigate = useNavigate()
  const [month, setMonth] = useState(() => dayjs().startOf('month'))
  const today = iso(dayjs())

  const first = month.subtract((month.day() + 6) % 7, 'day')
  const weeks = Math.ceil(((month.day() + 6) % 7 + month.daysInMonth()) / 7)
  const days = Array.from({ length: weeks * 7 }, (_, i) => first.add(i, 'day'))
  const onDay = (day: string) => events.filter(e => e.startDate <= day && day <= (e.endDate ?? e.startDate))

  return (
    <Box>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 1, px: 2.5, py: 1.5, borderBottom: '1px solid var(--divider)' }}>
        <Typography variant="h6" sx={{ minWidth: 140 }}>{month.format('MMMM YYYY')}</Typography>
        <IconButton aria-label="Previous month" size="small" onClick={() => setMonth(month.subtract(1, 'month'))}><ChevronLeft /></IconButton>
        <IconButton aria-label="Next month" size="small" onClick={() => setMonth(month.add(1, 'month'))}><ChevronRight /></IconButton>
        <Button size="small" variant="outlined" onClick={() => setMonth(dayjs().startOf('month'))}>Today</Button>
      </Stack>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, minmax(0, 1fr))' }}>
        {WEEKDAYS.map(w => (
          <Box key={w} sx={{ px: 1, py: 1, fontSize: 12, fontWeight: 600, color: 'text.secondary', borderBottom: '1px solid var(--divider)' }}>{w}</Box>
        ))}
        {days.map((d, i) => {
          const key = iso(d)
          const list = onDay(key)
          const outside = d.month() !== month.month()
          return (
            <Box
              key={key}
              sx={{
                minHeight: { xs: 64, md: 104 }, p: 0.75, minWidth: 0,
                borderBottom: '1px solid var(--divider)', borderRight: i % 7 === 6 ? 'none' : '1px solid var(--divider)',
                bgcolor: outside ? 'var(--fill)' : undefined,
              }}
            >
              <Box
                sx={{
                  display: 'grid', placeItems: 'center', width: 24, height: 24, mb: 0.5, borderRadius: 999, fontSize: 12.5, fontWeight: 590,
                  ...(key === today ? { bgcolor: 'primary.main', color: 'primary.contrastText' } : { color: outside ? 'text.disabled' : 'text.primary' }),
                }}
              >
                {d.date()}
              </Box>
              <Stack spacing={0.4}>
                {list.slice(0, SHOWN).map(e => (
                  <Box
                    key={e.id} component="button" type="button" title={e.title} onClick={() => navigate(`/events/${e.id}`)}
                    sx={{
                      all: 'unset', boxSizing: 'border-box', cursor: 'pointer', px: 0.75, py: 0.25, borderRadius: '6px', fontSize: 12, fontWeight: 500,
                      overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', bgcolor: 'var(--surface-selected)', boxShadow: 'var(--shadow-raised)',
                      '&:hover': { bgcolor: 'action.hover' }, '&:focus-visible': { boxShadow: 'var(--focus-ring)' },
                    }}
                  >
                    {e.title}
                  </Box>
                ))}
                {list.length > SHOWN && <Typography sx={{ fontSize: 12, color: 'text.secondary', px: 0.75 }}>+{list.length - SHOWN} more</Typography>}
              </Stack>
            </Box>
          )
        })}
      </Box>
    </Box>
  )
}
