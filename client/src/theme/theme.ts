import { createTheme } from '@mui/material/styles'

const ink = '#1d1d1f'
const font = '"Geist", "Helvetica Neue", Helvetica, Arial, sans-serif'
const divider = 'rgba(60, 60, 67, 0.12)'
const focusRing = '0 0 0 3px rgba(255, 198, 41, 0.5)'

export const theme = createTheme({
  shape: { borderRadius: 12 },
  palette: {
    primary: { main: ink, dark: '#3a3a3c', light: '#48484a', contrastText: '#fff' },
    secondary: { main: '#fff', contrastText: ink },
    error: { main: '#ff3b30', dark: '#b42318' },
    warning: { main: '#ff9f0a', dark: '#8a5a00' },
    success: { main: '#34c759', dark: '#1d7a3a' },
    info: { main: '#8e8e93', dark: '#6e6e73' },
    text: { primary: ink, secondary: '#6e6e73', disabled: 'rgba(60, 60, 67, 0.3)' },
    background: { default: '#f2f2f7', paper: '#fff' },
    divider,
    action: { hover: 'rgba(0, 0, 0, 0.05)', selected: 'rgba(0, 0, 0, 0.06)' },
  },
  typography: {
    fontFamily: font,
    fontSize: 14,
    h1: { fontSize: 34, fontWeight: 700, lineHeight: 1.15, letterSpacing: '-0.025em' },
    h2: { fontSize: 30, fontWeight: 700, lineHeight: 1.15, letterSpacing: '-0.025em' },
    h3: { fontSize: 24, fontWeight: 700, lineHeight: 1.25, letterSpacing: '-0.025em' },
    h4: { fontSize: 20, fontWeight: 650, lineHeight: 1.3 },
    h5: { fontSize: 17, fontWeight: 600, lineHeight: 1.35 },
    h6: { fontSize: 15, fontWeight: 600, lineHeight: 1.4 },
    subtitle1: { fontSize: 16, fontWeight: 650, lineHeight: 1.35 },
    subtitle2: { fontSize: 13, fontWeight: 500, lineHeight: 1.4 },
    body1: { fontSize: 14, lineHeight: 1.45, letterSpacing: '-0.01em' },
    body2: { fontSize: 14, lineHeight: 1.45, letterSpacing: '-0.01em' },
    caption: { fontSize: 12.5, lineHeight: 1.4 },
    button: { fontSize: 13.5, fontWeight: 590, lineHeight: 1, textTransform: 'none' },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          minHeight: '100vh',
          background: 'radial-gradient(900px 520px at 0 0, #fff7dc, transparent 60%), #f2f2f7',
          backgroundRepeat: 'no-repeat',
        },
        a: { color: ink },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 999, padding: '9px 16px' },
        sizeSmall: { padding: '6px 12px' },
        outlined: { borderColor: divider, backgroundColor: '#fff', '&:hover': { borderColor: divider, backgroundColor: '#f7f7f9' } },
        text: { '&:hover': { backgroundColor: 'rgba(0, 0, 0, 0.05)' } },
      },
    },
    MuiIconButton: { styleOverrides: { root: { color: '#8e8e93' } } },
    MuiPaper: {
      styleOverrides: {
        rounded: { borderRadius: 16 },
        elevation1: { boxShadow: '0 1px 2px rgba(0, 0, 0, 0.04), 0 8px 24px rgba(0, 0, 0, 0.04)' },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 10,
          backgroundColor: '#fff',
          '& .MuiOutlinedInput-notchedOutline': { borderColor: divider },
          '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(60, 60, 67, 0.3)' },
          '&.Mui-focused': { boxShadow: focusRing },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#e0a800', borderWidth: 1 },
        },
      },
    },
    MuiChip: { styleOverrides: { root: { borderRadius: 999, fontWeight: 500 }, outlined: { borderColor: divider } } },
    MuiAlert: { styleOverrides: { root: { borderRadius: 12 } } },
    MuiTableCell: {
      styleOverrides: {
        root: { borderBottom: `1px solid ${divider}`, padding: '12px 20px' },
        head: { fontSize: 12, fontWeight: 600, color: '#6e6e73' },
      },
    },
    MuiLink: { defaultProps: { underline: 'hover' }, styleOverrides: { root: { color: ink, fontWeight: 500 } } },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: 10,
          minHeight: 36,
          '&.Mui-selected, &.Mui-selected:hover': { backgroundColor: '#fff', boxShadow: '0 1px 3px rgba(0, 0, 0, 0.08), 0 0 0 0.5px rgba(0, 0, 0, 0.04)' },
        },
      },
    },
    // Segmented control: gray track, raised white thumb for the selected tab.
    MuiTabs: {
      defaultProps: { slotProps: { indicator: { style: { display: 'none' } } } },
      styleOverrides: {
        root: { minHeight: 0, padding: 2, borderRadius: 999, backgroundColor: 'var(--fill)', width: 'fit-content', maxWidth: '100%' },
        list: { gap: 2 },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          minHeight: 0, minWidth: 0, padding: '7px 18px', borderRadius: 999, fontSize: 13.5, fontWeight: 590, textTransform: 'none',
          color: ink,
          '&.Mui-selected': { color: ink, backgroundColor: 'var(--surface-selected)', boxShadow: 'var(--shadow-raised)' },
          '&.Mui-focusVisible': { boxShadow: focusRing },
        },
      },
    },
    MuiListItemIcon: { styleOverrides: { root: { minWidth: 32, color: 'inherit' } } },
  },
})
