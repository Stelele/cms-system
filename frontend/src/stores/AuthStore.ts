import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { ApiSingleton } from "@/services/api";

export const useAuthStore = defineStore("authStore", () => {
  const givenName = ref("");
  const email = ref("");
  const userId = ref("");
  const accessToken = ref("");

  const token = computed(() => accessToken.value);

  async function update() {
    const { getAccessTokenSilently, user } = useAuth0();

    const token = await getAccessTokenSilently();
    accessToken.value = token;

    givenName.value = user.value?.name || "Guest User";
    email.value = user.value?.email || "";
    userId.value = user.value?.sub || "";
  }

  async function getApi() {
    const api = await ApiSingleton.getInstance();
    return api;
  }

  return {
    token,
    givenName,
    email,
    userId,
    accessToken,
    update,
    getApi,
  };
});
