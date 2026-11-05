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

export const errText = (e: unknown) => (e instanceof Error ? e.message : 'Something went wrong')

type Config = {
  url: string
  method: string
  params?: Record<string, unknown>
  data?: unknown
  headers?: Record<string, string>
  signal?: AbortSignal
}

// Mutator for the Orval generated hooks: same-origin cookies, errors thrown as ApiError.
// `options.headers` carries per-hook headers such as Idempotency-Key.
export async function fetcher<T>(
  { url, method, params, data, headers, signal }: Config,
  options?: { headers?: Record<string, string> },
): Promise<T> {
  headers = { ...headers, ...options?.headers }
  const query = new URLSearchParams()
  for (const [k, v] of Object.entries(params ?? {})) if (v != null && v !== '') query.set(k, String(v))
  const raw = typeof data === 'string'
  const res = await fetch(query.size ? `${url}?${query}` : url, {
    method,
    credentials: 'include',
    signal,
    headers: data === undefined || raw ? headers : { 'Content-Type': 'application/json', ...headers },
    body: data === undefined || raw ? (data as string | undefined) : JSON.stringify(data),
  })
  if (!res.ok) throw new ApiError(res.status, await errorMessage(res))
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}
