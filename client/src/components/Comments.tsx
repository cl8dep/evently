import { Delete, Send } from '@mui/icons-material'
import { Alert, Box, Button, IconButton, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import Markdown from 'react-markdown'
import { errText } from '../api'
import { useCreateComment, useDeleteComment, useListComments } from '../api/generated/endpoints'
import { useAuth } from '../auth'

const when = (iso: string) => new Date(iso).toLocaleString(undefined, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })

export default function Comments({ eventId }: { eventId: number }) {
  const { email, isAdmin } = useAuth()
  const list = useListComments(eventId)
  const create = useCreateComment()
  const remove = useDeleteComment()
  const [body, setBody] = useState('')
  const [error, setError] = useState('')

  const send = async () => {
    if (!body.trim()) return
    setError('')
    try {
      await create.mutateAsync({ id: eventId, data: { body } })
      setBody('')
    } catch (err) {
      setError(errText(err))
    }
  }

  return (
    <Box sx={{ mt: 4 }}>
      <Typography variant="h3" sx={{ mb: 2 }}>Comments</Typography>
      <Stack spacing={2} sx={{ mb: 2 }}>
        {list.data?.length === 0 && <Typography color="text.secondary">No comments yet.</Typography>}
        {list.data?.map(c => (
          <Box key={c.id} sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline' }}>
              <Typography sx={{ fontWeight: 600, fontSize: 14 }}>{c.authorName || c.author}</Typography>
              <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{when(c.createdAt)}</Typography>
              {(isAdmin || c.author === email) && (
                <IconButton size="small" aria-label="Delete comment" sx={{ ml: 'auto !important' }} onClick={() => remove.mutate({ id: c.id })}>
                  <Delete fontSize="small" />
                </IconButton>
              )}
            </Stack>
            <Box sx={{ fontSize: 15, lineHeight: 1.55, overflowWrap: 'anywhere', '& p': { m: 0, mt: 0.5 }, '& pre': { overflow: 'auto' }, '& img': { maxWidth: '100%' } }}>
              <Markdown>{c.body}</Markdown>
            </Box>
          </Box>
        ))}
      </Stack>
      {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}
      <TextField
        multiline minRows={2} fullWidth value={body} placeholder="Write a comment. Markdown is supported. Ctrl+Enter to send."
        onChange={e => setBody(e.target.value)}
        onKeyDown={e => { if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) { e.preventDefault(); send() } }}
        slotProps={{ htmlInput: { maxLength: 2000 } }}
      />
      <Button sx={{ mt: 1 }} variant="contained" startIcon={<Send />} disabled={!body.trim() || create.isPending} onClick={send}>Comment</Button>
    </Box>
  )
}
