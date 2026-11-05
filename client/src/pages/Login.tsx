import { Visibility, VisibilityOff } from '@mui/icons-material'
import { Alert, Box, Button, Card, IconButton, InputAdornment, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { ApiError, errText } from '../api'
import { usePostApiAuthLogin, usePostApiAuthRegister } from '../api/generated/endpoints'
import Logo from '../components/Logo'

export default function Login({ onDone, setup }: { onDone: () => void; setup: boolean }) {
  const mode = setup ? 'register' : 'login'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [show, setShow] = useState(false)
  const [error, setError] = useState('')
  const signIn = mode === 'login'
  const register = usePostApiAuthRegister()
  const login = usePostApiAuthLogin()

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      const creds = { email, password }
      if (!signIn) await register.mutateAsync({ data: creds })
      await login.mutateAsync({ data: creds, params: { useCookies: true } })
      onDone()
    } catch (err) {
      if (err instanceof ApiError && err.status === 401)
        setError(err.message === 'LockedOut' ? 'Too many attempts. Try again in a few minutes.' : 'Invalid email or password.')
      else setError(errText(err))
    }
  }

  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '100vh', p: 2 }}>
      <Card component="form" onSubmit={submit} sx={{ width: '100%', maxWidth: 420, p: 4.5, borderRadius: '20px', boxShadow: 'var(--shadow-dialog)' }}>
        <Stack spacing={2.25}>
          <Stack direction="row" spacing={1.25} sx={{ justifyContent: 'center', alignItems: 'center' }}>
            <Logo size={36} />
            <Typography sx={{ fontSize: 20, fontWeight: 650, letterSpacing: '-0.02em' }}>Evently</Typography>
          </Stack>
          <Box sx={{ textAlign: 'center' }}>
            <Typography variant="h3">{signIn ? 'Sign in' : 'Create account'}</Typography>
            <Typography color="text.secondary" sx={{ mt: 0.75 }}>
              {signIn ? 'Track the events your team may attend.' : 'This is the first account. It will be the admin.'}
            </Typography>
          </Box>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Email" type="email" autoComplete="email" placeholder="you@company.com" value={email} onChange={e => { setEmail(e.target.value); setError('') }} required autoFocus error={!!error} />
          <TextField
            label="Password" type={show ? 'text' : 'password'} autoComplete={signIn ? 'current-password' : 'new-password'}
            placeholder="At least 8 characters" value={password} onChange={e => { setPassword(e.target.value); setError('') }} required error={!!error}
            slotProps={{
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
          <Button type="submit" variant="contained" fullWidth sx={{ height: 44, fontSize: 15 }}>{signIn ? 'Sign in' : 'Create account'}</Button>
        </Stack>
      </Card>
    </Box>
  )
}
