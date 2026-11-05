import { Box, Link } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'

export default function Crumbs({ items, current }: { items: { label: string; to: string }[]; current: string }) {
  return (
    <Box component="nav" aria-label="Breadcrumb" sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 1.25, fontSize: 14 }}>
      {items.map(i => (
        <Box key={i.to} sx={{ display: 'contents' }}>
          <Link component={RouterLink} to={i.to} underline="hover" sx={{ color: 'text.secondary', fontWeight: 400 }}>{i.label}</Link>
          <Box component="span" sx={{ color: 'text.disabled' }}>/</Box>
        </Box>
      ))}
      <Box component="span" sx={{ fontWeight: 600 }}>{current}</Box>
    </Box>
  )
}
