// Types shared across the app. The data layer lives in ./api (Orval hooks over ./api/fetcher).
export { ApiError, errText } from './api/fetcher'
export type { Category, Country, EventIn, Location, MeOut as Me, NameCount, Topic, UserSummary } from './api/generated/model'
export type { Event as EventItem } from './api/generated/model'

export type Role = 'Admin' | 'Editor' | 'Viewer'
