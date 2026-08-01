import { useAuthStore } from '@/stores/auth-store'

let isRefreshing = false
let refreshPromise: Promise<void> | null = null

export const authFetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
  const authStore = useAuthStore()

  const makeRequest = (token: string) => {
    const headers = new Headers(init?.headers)
    headers.set('Authorization', `Bearer ${token}`)
    return fetch(input, { ...init, headers })
  }

  let response = await makeRequest(authStore.accessToken)

  if (response.status === 401) {
    if (!isRefreshing) {
      isRefreshing = true
      refreshPromise = authStore.update().finally(() => {
        isRefreshing = false
        refreshPromise = null
      })
    }
    await (refreshPromise ?? Promise.resolve())
    response = await makeRequest(authStore.accessToken)
  }

  return response
}
