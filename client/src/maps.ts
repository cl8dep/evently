import { useEffect, useState } from 'react'
import { useGetConfig } from './api/generated/endpoints'

let script: Promise<void> | undefined

const useConfig = () => useGetConfig({ query: { staleTime: Infinity } })

// Place search is served by the backend. This only tells the UI whether it is available (null while loading).
export const usePlacesEnabled = () => {
  const { data, isError } = useConfig()
  return isError ? false : (data?.places ?? null)
}

// The interactive map needs a browser key, restricted by HTTP referrer in Google Cloud.
export const useGoogleMaps = () => {
  const mapsKey = useConfig().data?.mapsKey
  const [ready, setReady] = useState(false)
  useEffect(() => {
    if (!mapsKey) return
    script ??= new Promise<void>((resolve, reject) => {
      const s = document.createElement('script')
      s.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(mapsKey)}&loading=async&v=weekly`
      s.async = true
      s.onload = () => resolve()
      s.onerror = () => reject(new Error('Google Maps failed to load'))
      document.head.appendChild(s)
    })
    script.then(() => setReady(true), () => {})
  }, [mapsKey])
  return ready
}
