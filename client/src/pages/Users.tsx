import { Add, DeleteOutlined, Edit, Visibility, VisibilityOff } from '@mui/icons-material'
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, InputAdornment, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { api, errText, type Role, type UserRow } from '../api'
import { useAuth } from '../auth'

const ROLES: { role: Role; hint: string }[] = [
  { role: 'Admin', hint: 'Everything, including managing users.' },
  { role: 'Editor', hint: 'Create, edit and delete events and categories.' },
  { role: 'Viewer', hint: 'Read-only. Can export the CSV.' },
]
const COLS = 'minmax(0, 1fr) minmax(0, 1.3fr) 100px 88px'

type Draft = { id?: string; firstName: string; lastName: string; email: string; role: Role; password: string }
const blank: Draft = { firstName: '', lastName: '', email: '', role: 'Viewer', password: '' }

export default function Users() {
  const me = useAuth()
  const [rows, setRows] = useState<UserRow[]>([])
  const [draft, setDraft] = useState<Draft | null>(null)
  const [removing, setRemoving] = useState<UserRow | null>(null)
  const [show, setShow] = useState(false)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const busy = useRef(false)

  const load = () => api<UserRow[]>('/api/users').then(setRows, e => setError(errText(e)))
  useEffect(() => { void load() }, [])

  // One request at a time: a double click must not create the user twice.
  const run = async (fn: () => Promise<unknown>, done: () => void) => {
    if (busy.current) return
    busy.current = true
    setSaving(true)
    setError('')
    try {
      await fn()
      done()
      await load()
    } catch (e) {
      setError(errText(e))
    } finally {
      busy.current = false
      setSaving(false)
    }
  }

  const save = (e: React.FormEvent) => {
    e.preventDefault()
    if (!draft) return
    const body = { firstName: draft.firstName, lastName: draft.lastName, email: draft.email, role: draft.role, password: draft.password || undefined }
    void run(() => (draft.id ? api(`/api/users/${draft.id}`, 'PUT', body) : api('/api/users', 'POST', body)), () => setDraft(null))
  }

  const remove = () => removing && run(() => api(`/api/users/${removing.id}`, 'DELETE'), () => setRemoving(null))

  const open = (d: Draft) => { setError(''); setShow(false); setDraft(d) }

  return (
    <Box sx={{ maxWidth: 820 }}>
      <Stack direction="row" sx={{ flexWrap: 'wrap', alignItems: 'flex-end', justifyContent: 'space-between', gap: 1.5, mb: 3 }}>
        <Box>
          <Typography variant="h2">Users</Typography>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>Who can sign in, and what they can do.</Typography>
        </Box>
        <Button variant="contained" startIcon={<Add />} onClick={() => open(blank)}>Add user</Button>
      </Stack>

      {error && !draft && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}

      <Paper sx={{ overflow: 'hidden' }}>
        <Box sx={{ display: 'grid', gridTemplateColumns: COLS, columnGap: 2, px: 2.5, py: 1.25, borderBottom: '1px solid var(--divider)', fontSize: 12, fontWeight: 600, color: 'text.secondary' }}>
          <span>Name</span><span>Email</span><span>Role</span><span style={{ textAlign: 'right' }}>Actions</span>
        </Box>
        {rows.map(u => (
          <Box key={u.id} sx={{ display: 'grid', gridTemplateColumns: COLS, columnGap: 2, alignItems: 'center', minHeight: 52, py: 0.75, pl: 2.5, pr: 1.5, borderBottom: '1px solid var(--divider)', '&:hover': { bgcolor: 'action.hover' } }}>
            <Typography noWrap sx={{ fontWeight: 600 }}>{[u.firstName, u.lastName].filter(Boolean).join(' ') || '-'}{u.email === me.email && <Box component="span" sx={{ color: 'text.secondary', fontWeight: 400 }}> (you)</Box>}</Typography>
            <Typography noWrap color="text.secondary">{u.email}</Typography>
            <span><Chip size="small" variant="outlined" label={u.role} /></span>
            <Stack direction="row" sx={{ justifyContent: 'flex-end', gap: '2px' }}>
              <IconButton size="small" aria-label={`Edit ${u.email}`} title="Edit" onClick={() => open({ id: u.id, firstName: u.firstName, lastName: u.lastName, email: u.email, role: u.role, password: '' })}><Edit fontSize="small" /></IconButton>
              <IconButton size="small" aria-label={`Delete ${u.email}`} title="Delete" disabled={u.email === me.email} onClick={() => { setError(''); setRemoving(u) }}>
                <DeleteOutlined fontSize="small" sx={{ color: u.email === me.email ? undefined : 'error.main' }} />
              </IconButton>
            </Stack>
          </Box>
        ))}
        <Typography color="text.secondary" sx={{ px: 2.5, py: 1.25, fontSize: 13 }}>{rows.length} {rows.length === 1 ? 'user' : 'users'}</Typography>
      </Paper>

      <Dialog open={!!draft} onClose={() => !saving && setDraft(null)} fullWidth maxWidth="xs" component="form" onSubmit={save}>
        <DialogTitle>{draft?.id ? 'Edit user' : 'Add user'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2.25} sx={{ pt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <Stack direction="row" spacing={2}>
              <TextField label="First name" required autoFocus value={draft?.firstName ?? ''} onChange={e => setDraft(d => d && { ...d, firstName: e.target.value })} slotProps={{ inputLabel: { shrink: true }, htmlInput: { maxLength: 100 } }} />
              <TextField label="Last name" required value={draft?.lastName ?? ''} onChange={e => setDraft(d => d && { ...d, lastName: e.target.value })} slotProps={{ inputLabel: { shrink: true }, htmlInput: { maxLength: 100 } }} />
            </Stack>
            <TextField label="Email" type="email" required value={draft?.email ?? ''} onChange={e => setDraft(d => d && { ...d, email: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
            <TextField
              label="Role" select value={draft?.role ?? 'Viewer'} onChange={e => setDraft(d => d && { ...d, role: e.target.value as Role })}
              helperText={ROLES.find(r => r.role === draft?.role)?.hint} slotProps={{ inputLabel: { shrink: true } }}
            >
              {ROLES.map(r => <MenuItem key={r.role} value={r.role}>{r.role}</MenuItem>)}
            </TextField>
            <TextField
              label={draft?.id ? 'New password' : 'Password'} type={show ? 'text' : 'password'} required={!draft?.id} autoComplete="new-password"
              placeholder={draft?.id ? 'Leave empty to keep the current one' : 'At least 8 characters'} value={draft?.password ?? ''}
              onChange={e => setDraft(d => d && { ...d, password: e.target.value })}
              slotProps={{
                inputLabel: { shrink: true },
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton size="small" aria-label={show ? 'Hide password' : 'Show password'} onClick={() => setShow(!show)}>
                        {show ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setDraft(null)} disabled={saving}>Cancel</Button>
          <Button type="submit" variant="contained" loading={saving}>{draft?.id ? 'Save changes' : 'Add user'}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!removing} onClose={() => !saving && setRemoving(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete {removing?.email}?</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 1.5 }}>{error}</Alert>}
          <Typography>They lose access immediately. Events they created stay.</Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setRemoving(null)} disabled={saving}>Cancel</Button>
          <Button variant="contained" color="error" loading={saving} onClick={remove}>Delete user</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
