const parse = (iso: string) => {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const fmt = (d: Date, o: Intl.DateTimeFormatOptions) => d.toLocaleDateString('en-GB', o)

export function shortRange(start: string, end?: string | null) {
  const a = parse(start)
  if (!end || end === start) return fmt(a, { day: 'numeric', month: 'short', year: 'numeric' })
  const b = parse(end)
  if (a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth())
    return `${a.getDate()}–${fmt(b, { day: 'numeric', month: 'short', year: 'numeric' })}`
  return `${fmt(a, { day: 'numeric', month: 'short' })} – ${fmt(b, { day: 'numeric', month: 'short', year: 'numeric' })}`
}

export function longRange(start: string, end?: string | null) {
  const o: Intl.DateTimeFormatOptions = { weekday: 'short', day: 'numeric', month: 'short' }
  const a = parse(start)
  if (!end || end === start) return { text: fmt(a, { ...o, year: 'numeric' }), days: 1 }
  const b = parse(end)
  const days = Math.round((b.getTime() - a.getTime()) / 86_400_000) + 1
  return { text: `${fmt(a, o)} – ${fmt(b, { ...o, year: 'numeric' })}`, days }
}

export const day = (iso: string) => fmt(new Date(iso), { day: 'numeric', month: 'short', year: 'numeric' })

export function ticket(cost?: number | null, currency?: string | null) {
  if (cost == null) return ''
  if (cost === 0) return 'Free'
  return `${currency ? currency + ' ' : ''}${cost.toLocaleString('en-US')}`
}

export const place = (l: { city?: string | null; country?: string | null }) => [l.city, l.country].filter(Boolean).join(', ')
