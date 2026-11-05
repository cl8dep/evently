import { useEffect, useState } from 'react'
import { api } from './api'

export type Ref = { code: string; name: string }

const cache: Record<string, Promise<Ref[]>> = {}

export function useRefs(kind: 'countries' | 'currencies') {
  const [items, setItems] = useState<Ref[]>([])
  useEffect(() => {
    cache[kind] ??= api<Ref[]>(`/api/${kind}`)
    cache[kind].then(setItems, () => { delete cache[kind] })
  }, [kind])
  return items
}
