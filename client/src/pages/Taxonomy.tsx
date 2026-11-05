import { Add, Check, DeleteOutlined, Edit } from '@mui/icons-material'
import { Alert, AlertTitle, Box, Button, IconButton, Paper, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { api, ApiError, errText, type Category } from '../api'
import { useAuth } from '../auth'

const COLS = 'minmax(0, 1fr) 110px 88px'
const COLS_READONLY = 'minmax(0, 1fr) 110px'
const plural = (n: number) => (n === 1 ? '1 event' : `${n} events`)

type Problem = { title: string; body: string }

const KINDS = {
  categories: {
    path: '/api/categories', title: 'Categories', noun: 'category', nouns: 'categories', example: 'e.g. Webinar',
    intro: "Used to group and filter events. A category in use can't be deleted.", fix: 'Move them to another category before deleting it.',
    empty: 'No categories yet. Add one to start creating events.',
  },
  topics: {
    path: '/api/topics', title: 'Topics', noun: 'topic', nouns: 'topics', example: 'e.g. Fintech',
    intro: "Tag events with one or more topics. A topic in use can't be deleted.", fix: 'Remove it from those events before deleting it.',
    empty: 'No topics yet. Add one to tag your events.',
  },
}

export default function Taxonomy({ kind }: { kind: keyof typeof KINDS }) {
  const k = KINDS[kind]
  const { canEdit } = useAuth()
  const [items, setItems] = useState<Category[]>([])
  const [name, setName] = useState('')
  const [editing, setEditing] = useState<Category | null>(null)
  const [problem, setProblem] = useState<Problem | null>(null)

  const load = () => api<Category[]>(k.path).then(setItems, e => setProblem({ title: errText(e), body: '' }))
  useEffect(() => { setItems([]); setEditing(null); setName(''); setProblem(null); void load() }, [kind])  // eslint-disable-line react-hooks/exhaustive-deps

  const duplicate = (n: string): Problem => ({ title: `This ${k.noun} already exists.`, body: `Pick a different name for "${n}".` })

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    const n = name.trim()
    if (!n) return
    setProblem(null)
    try {
      if (editing) await api(`${k.path}/${editing.id}`, 'PUT', { name: n })
      else await api(k.path, 'POST', { name: n })
      setName('')
      setEditing(null)
      await load()
    } catch (err) {
      setProblem(err instanceof ApiError && err.status === 409 ? duplicate(n) : { title: errText(err), body: '' })
    }
  }

  const remove = async (c: Category) => {
    setProblem(null)
    try {
      await api(`${k.path}/${c.id}`, 'DELETE')
      if (editing?.id === c.id) { setEditing(null); setName('') }
      await load()
    } catch (err) {
      setProblem(
        err instanceof ApiError && err.status === 409
          ? { title: `${k.noun[0].toUpperCase()}${k.noun.slice(1)} is used by events.`, body: `${c.name} has ${plural(c.eventCount ?? 0)}. ${k.fix}` }
          : { title: errText(err), body: '' },
      )
    }
  }

  const total = items.reduce((n, c) => n + (c.eventCount ?? 0), 0)

  return (
    <Box sx={{ maxWidth: 720 }}>
      <Typography variant="h2">{k.title}</Typography>
      <Typography color="text.secondary" sx={{ mt: 0.75, mb: 3 }}>{k.intro}</Typography>

      <Paper sx={{ overflow: 'hidden' }}>
        {canEdit && <Stack spacing={1.75} sx={{ p: 2.5, borderBottom: '1px solid var(--divider)' }}>
          <Stack component="form" onSubmit={save} direction="row" sx={{ flexWrap: 'wrap', alignItems: 'flex-end', gap: 1 }}>
            <TextField
              label={editing ? `Rename "${editing.name}"` : `${k.noun[0].toUpperCase()}${k.noun.slice(1)} name`} placeholder={k.example} value={name}
              onChange={e => setName(e.target.value)} slotProps={{ inputLabel: { shrink: true }, htmlInput: { maxLength: 100 } }} sx={{ flex: '1 1 260px' }}
            />
            <Button type="submit" variant="contained" disabled={!name.trim()} startIcon={editing ? <Check /> : <Add />}>
              {editing ? 'Rename' : `Add ${k.noun}`}
            </Button>
            {editing && <Button onClick={() => { setEditing(null); setName('') }}>Cancel</Button>}
          </Stack>
          {problem && (
            <Alert severity="error" onClose={() => setProblem(null)}>
              <AlertTitle>{problem.title}</AlertTitle>
              {problem.body}
            </Alert>
          )}
        </Stack>}

        <Box sx={{ display: 'grid', gridTemplateColumns: canEdit ? COLS : COLS_READONLY, columnGap: 2, alignItems: 'center', px: 2.5, py: 1.25, borderBottom: '1px solid var(--divider)', fontSize: 12, fontWeight: 600, color: 'text.secondary' }}>
          <span>Name</span><span style={{ textAlign: 'right' }}>Events</span>{canEdit && <span style={{ textAlign: 'right' }}>Actions</span>}
        </Box>
        {items.map(c => (
          <Box
            key={c.id}
            sx={{
              display: 'grid', gridTemplateColumns: canEdit ? COLS : COLS_READONLY, columnGap: 2, alignItems: 'center', minHeight: 52, py: 0.75, pl: 2.5, pr: 1.5,
              borderBottom: '1px solid var(--divider)', bgcolor: editing?.id === c.id ? 'var(--accent-a20)' : undefined, '&:hover': { bgcolor: editing?.id === c.id ? undefined : 'action.hover' },
            }}
          >
            <Typography noWrap sx={{ fontWeight: 600 }}>{c.name}</Typography>
            <Typography color="text.secondary" sx={{ textAlign: 'right', fontVariantNumeric: 'tabular-nums' }}>{plural(c.eventCount ?? 0)}</Typography>
            {canEdit && (
            <Stack direction="row" sx={{ justifyContent: 'flex-end', gap: '2px' }}>
              <IconButton size="small" aria-label={`Rename ${c.name}`} title="Rename" onClick={() => { setEditing(c); setName(c.name); setProblem(null) }}><Edit fontSize="small" /></IconButton>
              <IconButton size="small" aria-label={`Delete ${c.name}`} title="Delete" onClick={() => remove(c)}><DeleteOutlined fontSize="small" sx={{ color: 'error.main' }} /></IconButton>
            </Stack>
            )}
          </Box>
        ))}
        <Typography color="text.secondary" sx={{ px: 2.5, py: 1.25, fontSize: 13 }}>
          {items.length === 0 ? k.empty : `${items.length} ${items.length === 1 ? k.noun : k.nouns} · ${plural(total)}`}
        </Typography>
      </Paper>
    </Box>
  )
}
