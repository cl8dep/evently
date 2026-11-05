import { useListCountries, useListCurrencies } from './api/generated/endpoints'

export type Ref = { code: string; name: string }

const EMPTY: Ref[] = []

export function useRefs(kind: 'countries' | 'currencies') {
  const countries = useListCountries({ query: { enabled: kind === 'countries', staleTime: Infinity } })
  const currencies = useListCurrencies({ query: { enabled: kind === 'currencies', staleTime: Infinity } })
  return (kind === 'countries' ? countries.data : currencies.data) ?? EMPTY
}
