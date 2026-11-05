import { Autocomplete, TextField } from '@mui/material'
import { useEffect, useState } from 'react'
import { errText, type Location } from '../api'
import { getPlace, useAutocompletePlaces } from '../api/generated/endpoints'
import type { PlaceSuggestion as Suggestion } from '../api/generated/model'

const EMPTY: Suggestion[] = []
type Props = { venue: string; summary: string; onPick: (l: Location) => void; onClear: () => void; onError: (message: string) => void }

export default function PlaceSearch({ venue, summary, onPick, onClear, onError }: Props) {
  const [input, setInput] = useState(venue)
  const [term, setTerm] = useState('')
  // Autocomplete requests and the final details call share one session so Google bills them together.
  const [session, setSession] = useState(() => crypto.randomUUID())

  useEffect(() => { setInput(venue) }, [venue])

  useEffect(() => {
    if (input.trim().length < 3 || input === venue) { setTerm(''); return }
    const t = setTimeout(() => setTerm(input.trim()), 250)
    return () => clearTimeout(t)
  }, [input, venue])

  const { data: options = EMPTY, error } = useAutocompletePlaces({ q: term, session }, { query: { enabled: term.length >= 3 } })
  useEffect(() => { if (error) onError(errText(error)) }, [error, onError])

  const pick = async (s: Suggestion) => {
    try {
      onPick(await getPlace(s.placeId, { session }))
      setSession(crypto.randomUUID())
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
