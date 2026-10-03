// Local-only Vite config for the browser check. It aliases Auth0 to a stub so
// the route guard passes without an interactive login, and points the API at
// the local backend. The app's own code is untouched.
import { defineConfig, mergeConfig } from 'vite'
import base from './vite.config'

export default mergeConfig(
  base,
  defineConfig({
    resolve: {
      alias: [{ find: /^@auth0\/auth0-vue$/, replacement: '/.browser-check/auth0-stub.ts' }],
    },
    // Port 5173 because the dev backend's CORS allowlist is localhost:5173.
    server: { port: 5173, strictPort: true, https: false },
  }),
)
