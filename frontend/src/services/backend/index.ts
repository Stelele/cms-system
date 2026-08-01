import createClient from 'openapi-fetch'
import type { paths } from './schema'
import { authFetch } from '@/services/auth-fetch'

export type Client = ReturnType<typeof createClient<paths>>

export class BackendApiSingleton {
  private static client: Client | null = null

  public static getInstance(): Client {
    if (!this.client) {
      this.client = createClient<paths>({
        baseUrl: import.meta.env.VITE_API_URL,
        fetch: authFetch,
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
      })
    }
    return this.client
  }
}
