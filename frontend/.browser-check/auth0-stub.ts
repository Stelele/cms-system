// Test-only stand-in for @auth0/auth0-vue, wired in by vite.browser-check.config.ts.
//
// It reports the user as authenticated and returns a token from
// VITE_BROWSER_CHECK_TOKEN, so the browser check exercises the real app against a
// real backend over real HTTP. Only the interactive login is faked.
//
// The token is read from the environment and never committed. It needs
// read:blogs and read:posts only, which is what the page load path uses.
import type { App, Plugin } from 'vue'
import { ref } from 'vue'

const TOKEN = import.meta.env.VITE_BROWSER_CHECK_TOKEN ?? ''

export function useAuth0() {
  return {
    isAuthenticated: ref(true),
    isLoading: ref(false),
    user: ref({ sub: 'browser-check', name: 'Browser Check' }),
    getAccessTokenSilently: async () => TOKEN,
    getAccessTokenWithPopup: async () => TOKEN,
    loginWithRedirect: async () => {},
    logout: async () => {},
  }
}

export const createAuth0 = (_options?: unknown): Plugin => ({
  install(app: App) {
    const auth = useAuth0()
    app.provide('auth0', auth)
    app.config.globalProperties.$auth0 = auth
  },
})

// vue-router 4 wants a plain guard function, not an object with beforeEnter.
export const authGuard = () => true
