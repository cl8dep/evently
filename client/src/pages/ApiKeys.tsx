import { ContentCopy, DeleteOutlined } from '@mui/icons-material'
import { Alert, Box, Button, IconButton, Paper, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { errText } from '../api'
import { useCreateApiKey, useDeleteApiKey, useListApiKeys } from '../api/generated/endpoints'
import { day } from '../format'

const mono = { fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace', fontSize: 13, overflowWrap: 'anywhere' } as const

export default function ApiKeys() {
  const keys = useListApiKeys()
  const create = useCreateApiKey()
  const remove = useDeleteApiKey()
  const [name, setName] = useState('')
  const [secret, setSecret] = useState('')
  const [error, setError] = useState('')
  const endpoint = `${location.origin}/mcp`
  const command = `claude mcp add --transport http evently ${endpoint} --header "Authorization: Bearer ${secret || '<your key>'}"`

  const add = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      setSecret((await create.mutateAsync({ data: { name } })).secret)
      setName('')
    } catch (err) {
      setError(errText(err))
    }
  }

  return (
    <Box sx={{ maxWidth: 880 }}>
      <Typography variant="h2">API keys</Typography>
      <Typography color="text.secondary" sx={{ mt: 0.75, mb: 3 }}>
        Let an AI assistant use Evently over MCP. A key acts as you, with your role, and only opens the MCP endpoint.
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {secret && (
        <Alert severity="success" onClose={() => setSecret('')} sx={{ mb: 2 }} action={
          <IconButton aria-label="Copy key" onClick={() => navigator.clipboard.writeText(secret)}><ContentCopy fontSize="small" /></IconButton>
        }>
          Copy this key now. It will not be shown again.
          <Box sx={{ ...mono, mt: 0.5 }}>{secret}</Box>
        </Alert>
      )}

      <Paper sx={{ p: 3, mb: 3 }}>
        <Stack component="form" direction="row" spacing={1} onSubmit={add}>
          <TextField size="small" fullWidth value={name} onChange={e => setName(e.target.value)} placeholder="Name, for example Claude Desktop" slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Key name' } }} />
          <Button type="submit" variant="contained" disabled={!name.trim() || create.isPending} sx={{ flexShrink: 0 }}>Create key</Button>
        </Stack>
        <Stack divider={<Box sx={{ borderTop: '1px solid var(--divider)' }} />} sx={{ mt: 2 }}>
          {keys.data?.length === 0 && <Typography color="text.secondary" sx={{ fontSize: 14 }}>No keys yet.</Typography>}
          {keys.data?.map(k => (
            <Stack key={k.id} direction="row" sx={{ alignItems: 'center', gap: 2, py: 1 }}>
              <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                <Typography sx={{ fontWeight: 600 }}>{k.name}</Typography>
                <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                  <Box component="span" sx={mono}>{k.prefix}...</Box> &middot; created {day(k.createdAt)} &middot; {k.lastUsedAt ? `last used ${day(k.lastUsedAt)}` : 'never used'}
                </Typography>
              </Box>
              <IconButton aria-label={`Revoke ${k.name}`} onClick={() => remove.mutate({ id: k.id })}><DeleteOutlined /></IconButton>
            </Stack>
          ))}
        </Stack>
      </Paper>

      <Paper sx={{ p: 3 }}>
        <Typography sx={{ fontWeight: 600, mb: 0.5 }}>Connect a client</Typography>
        <Typography color="text.secondary" sx={{ fontSize: 14, mb: 1.5 }}>
          Endpoint <Box component="span" sx={mono}>{endpoint}</Box>, sent with an <Box component="span" sx={mono}>Authorization: Bearer</Box> header. For Claude Code:
        </Typography>
        <Box sx={{ ...mono, p: 1.5, borderRadius: '10px', bgcolor: 'var(--fill)' }}>{command}</Box>
        <Typography color="text.secondary" sx={{ fontSize: 13, mt: 1.5 }}>
          Tools: list_events, get_event, list_attendees, list_comments, add_comment, set_attendance.
        </Typography>
      </Paper>
    </Box>
  )
}
