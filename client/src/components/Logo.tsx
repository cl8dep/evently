import { Event } from '@mui/icons-material'
import { Box } from '@mui/material'

export default function Logo({ size = 28 }: { size?: number }) {
  return (
    <Box sx={{ width: size, height: size, borderRadius: `${Math.round(size * 0.29)}px`, background: 'var(--accent-gradient)', color: '#fff', display: 'grid', placeItems: 'center', flex: '0 0 auto' }}>
      <Event sx={{ fontSize: size * 0.64 }} />
    </Box>
  )
}
