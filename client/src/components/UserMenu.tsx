import { Logout } from '@mui/icons-material'
import { Avatar, Box, Divider, IconButton, ListItemIcon, ListItemText, Menu, MenuItem, Typography } from '@mui/material'
import { useState } from 'react'

type Props = { email: string; name: string; role: string; onLogout: () => void }

export default function UserMenu({ email, name, role, onLogout }: Props) {
  const initials = (name ? name.split(' ').map(w => w.charAt(0)).slice(0, 2).join('') : email.charAt(0)).toUpperCase()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  return (
    <>
      <IconButton aria-label="Account menu" aria-haspopup="menu" aria-expanded={!!anchor} onClick={e => setAnchor(e.currentTarget)} size="small">
        <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', color: '#fff', fontSize: 13, fontWeight: 600 }}>
          {initials}
        </Avatar>
      </IconButton>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
        <Box sx={{ px: 2, py: 1.25, minWidth: 220 }}>
          <Typography noWrap sx={{ fontWeight: 600 }}>{name || email}</Typography>
          {name && <Typography noWrap variant="body2" color="text.secondary">{email}</Typography>}
          <Typography variant="body2" color="text.secondary">{role}</Typography>
        </Box>
        <Divider />
        <MenuItem onClick={() => { setAnchor(null); onLogout() }}>
          <ListItemIcon><Logout fontSize="small" /></ListItemIcon>
          <ListItemText>Log out</ListItemText>
        </MenuItem>
      </Menu>
    </>
  )
}
