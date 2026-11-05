import { Autocomplete, TextField } from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { api, errText, type Location } from '../api'

type Suggestion = { placeId: string; text: string }
type Props = { venue: string; summary: string; onPick: (l: Location) => void; onClear: () => void; onError: (message: string) => void }

export default function PlaceSearch({ venue, summary, onPick, onClear, onError }: Props) {
  const [input, setInput] = useState(venue)
  const [options, setOptions] = useState<Suggestion[]>([])
  // Autocomplete requests and the final details call share one session so Google bills them together.
  const session = useRef(crypto.randomUUID())

  useEffect(() => { setInput(venue) }, [venue])

  useEffect(() => {
    if (input.trim().length < 3 || input === venue) { setOptions([]); return }
    const ctrl = new AbortController()
    const t = setTimeout(() => {
      api<Suggestion[]>(`/api/places/autocomplete?q=${encodeURIComponent(input.trim())}&session=${session.current}`)
        .then(r => { if (!ctrl.signal.aborted) setOptions(r) }, e => { if (!ctrl.signal.aborted) { setOptions([]); onError(errText(e)) } })
    }, 250)
    return () => { ctrl.abort(); clearTimeout(t) }
  }, [input, venue, onError])

  const pick = async (s: Suggestion) => {
    try {
      onPick(await api<Location>(`/api/places/${encodeURIComponent(s.placeId)}?session=${session.current}`))
      session.current = crypto.randomUUID()
    } catch (e) {
      onError(errText(e))
    }
  }

  return (
    <Autocomplete
      sx={{ gridColumn: '1 / -1' }}
      options={options} getOptionLabel={o => o.text} filterOptions={x => x} value={null} inputValue={input}
      onInputChange={(_, v, reason) => {
        if (reason === 'clear') { setInput(''); onClear() } else if (reason === 'input') setInput(v)
      }}
      onChange={(_, o) => { if (o) void pick(o) }}
      onBlur={() => setInput(venue)}
      noOptionsText={input.trim().length < 3 ? 'Type at least 3 characters' : 'No places found'}
      renderInput={p => (
        <TextField {...p} label="Venue" placeholder="Search a venue or address" helperText={summary || 'Address, city and country are filled in for you.'}
          slotProps={{ ...p.slotProps, inputLabel: { ...p.slotProps.inputLabel, shrink: true } }} />
      )}
    />
  )
}
