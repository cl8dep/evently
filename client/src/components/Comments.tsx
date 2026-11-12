import { Delete, Edit, ExpandLess, Send } from '@mui/icons-material'
import { Alert, Box, Button, IconButton, Stack, TextField, Typography } from '@mui/material'
import { keepPreviousData } from '@tanstack/react-query'
import { useState } from 'react'
import Markdown from 'react-markdown'
import { errText } from '../api'
import { useCreateComment, useDeleteComment, useListComments, useUpdateComment } from '../api/generated/endpoints'
import { useAuth } from '../auth'

const PAGE = 20
const when = (iso: string) => new Date(iso).toLocaleString(undefined, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
const sendOnCtrlEnter = (fn: () => void) => (e: React.KeyboardEvent) => {
  if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) { e.preventDefault(); fn() }
}

export default function Comments({ eventId }: { eventId: number }) {
  const { email, isAdmin } = useAuth()
  // Asking for more just widens the window of newest comments, so edits and deletes stay in sync.
  const [limit, setLimit] = useState(PAGE)
  const list = useListComments(eventId, { limit }, { query: { placeholderData: keepPreviousData } })
  const create = useCreateComment()
  const update = useUpdateComment()
  const remove = useDeleteComment()
  const [body, setBody] = useState('')
  const [editing, setEditing] = useState<{ id: number; body: string } | null>(null)
  const [error, setError] = useState('')

  const run = async (fn: () => Promise<unknown>, done: () => void) => {
    setError('')
    try {
      await fn()
      done()
    } catch (err) {
      setError(errText(err))
    }
  }
  const send = () => body.trim() && run(() => create.mutateAsync({ id: eventId, data: { body } }), () => setBody(''))
  const save = () => editing?.body.trim() && run(() => update.mutateAsync({ id: editing.id, data: { body: editing.body } }), () => setEditing(null))

  const items = list.data?.items
  return (
    <Box>
      <Stack spacing={2} sx={{ mb: 2 }}>
        {list.data?.hasMore && (
          <Button size="small" startIcon={<ExpandLess />} disabled={list.isFetching} onClick={() => setLimit(l => l + PAGE)} sx={{ alignSelf: 'center' }}>
            Load earlier comments
          </Button>
        )}
        {items?.length === 0 && <Typography color="text.secondary">No comments yet.</Typography>}
        {items?.map(c => (
          <Box key={c.id} sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline' }}>
              <Typography sx={{ fontWeight: 600, fontSize: 14 }}>{c.authorName || c.author}</Typography>
              <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{when(c.createdAt)}{c.editedAt && ' · edited'}</Typography>
              <Box sx={{ ml: 'auto !important', whiteSpace: 'nowrap' }}>
                {c.author === email && (
                  <IconButton size="small" aria-label="Edit comment" onClick={() => setEditing({ id: c.id, body: c.body })}>
                    <Edit fontSize="small" />
                  </IconButton>
                )}
                {(isAdmin || c.author === email) && (
                  <IconButton size="small" aria-label="Delete comment" onClick={() => remove.mutate({ id: c.id })}>
                    <Delete fontSize="small" />
                  </IconButton>
                )}
              </Box>
            </Stack>
            {editing?.id === c.id ? (
              <Box sx={{ mt: 0.5 }}>
                <TextField
                  multiline minRows={2} fullWidth autoFocus value={editing.body}
                  onChange={e => setEditing({ id: c.id, body: e.target.value })}
                  onKeyDown={e => { if (e.key === 'Escape') setEditing(null); else sendOnCtrlEnter(save)(e) }}
                  slotProps={{ htmlInput: { maxLength: 2000 } }}
                />
                <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                  <Button size="small" variant="contained" disabled={!editing.body.trim() || update.isPending} onClick={save}>Save</Button>
                  <Button size="small" onClick={() => setEditing(null)}>Cancel</Button>
                </Stack>
              </Box>
            ) : (
              <Box sx={{ fontSize: 15, lineHeight: 1.55, overflowWrap: 'anywhere', '& p': { m: 0, mt: 0.5 }, '& pre': { overflow: 'auto' }, '& img': { maxWidth: '100%' } }}>
                <Markdown>{c.body}</Markdown>
              </Box>
            )}
          </Box>
        ))}
      </Stack>
      {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}
      <TextField
        multiline minRows={2} fullWidth value={body} placeholder="Write a comment. Markdown is supported. Ctrl+Enter to send."
        onChange={e => setBody(e.target.value)}
        onKeyDown={sendOnCtrlEnter(send)}
        slotProps={{ htmlInput: { maxLength: 2000 } }}
      />
      <Button sx={{ mt: 1 }} variant="contained" startIcon={<Send />} disabled={!body.trim() || create.isPending} onClick={send}>Comment</Button>
    </Box>
  )
}
