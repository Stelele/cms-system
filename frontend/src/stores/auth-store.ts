import { useAuth0 } from '@auth0/auth0-vue'
import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAuthStore = defineStore('authStore', () => {
  const accessToken = ref('')

  async function update() {
    const { getAccessTokenSilently } = useAuth0()

    const token = await getAccessTokenSilently({
      authorizationParams: {
        audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      },
    })
    accessToken.value = token
  }

  return { accessToken, update }
})
