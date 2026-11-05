export type Category = { id: number; name: string; eventCount?: number }
export type Topic = Category

export type Location = {
  venue?: string | null
  address?: string | null
  city?: string | null
  region?: string | null
  country?: string | null
  latitude?: number | null
  longitude?: number | null
}

export type EventItem = {
  id: number
  title: string
  description?: string | null
  categoryId: number
  category?: Category
  topics: Topic[]
  website?: string | null
  ticketCost?: number | null
  currency?: string | null
  startDate: string
  endDate?: string | null
  location: Location
  createdBy: string
  createdAt: string
  iconUpdatedAt?: string | null
}

export type EventIn = Omit<EventItem, 'id' | 'category' | 'topics' | 'createdBy' | 'createdAt' | 'iconUpdatedAt'> & { topicIds: number[] }

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function errorMessage(res: Response) {
  try {
    const body = await res.json()
    if (body.errors) return (Object.values(body.errors) as string[][]).flat().join(' ')
    if (body.error) return body.error as string
    if (body.detail) return body.detail as string
  } catch {
    // not JSON
  }
  return `Request failed (${res.status})`
}

export async function api<T = void>(path: string, method = 'GET', body?: unknown, headers?: Record<string, string>): Promise<T> {
  const res = await fetch(path, {
    method,
    credentials: 'include',
    headers: body === undefined || body instanceof Blob ? headers : { 'Content-Type': 'application/json', ...headers },
    body: body === undefined || body instanceof Blob ? body : JSON.stringify(body),
  })
  if (!res.ok) throw new ApiError(res.status, await errorMessage(res))
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const errText = (e: unknown) => (e instanceof Error ? e.message : 'Something went wrong')

export type Role = 'Admin' | 'Editor' | 'Viewer'
export type Me = { email: string | null; firstName: string | null; lastName: string | null; role: Role | null; setup: boolean }
export type UserRow = { id: string; firstName: string; lastName: string; email: string; role: Role }
