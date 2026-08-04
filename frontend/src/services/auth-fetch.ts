import { useAuthStore } from "@/stores/auth-store";

let isRefreshing = false;
let refreshPromise: Promise<void> | null = null;

export const authFetch = async (
  input: RequestInfo | URL,
  init?: RequestInit,
): Promise<Response> => {
  const authStore = useAuthStore();

  const makeRequest = (token: string, signal?: AbortSignal) => {
    const headers = new Headers(init?.headers);
    headers.set("Authorization", `Bearer ${token}`);
    const isFormData = init?.body instanceof FormData;
    if (!isFormData && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }
    return fetch(input, { ...init, headers, signal });
  };

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 30000);

  let response = await makeRequest(authStore.accessToken, controller.signal);

  if (response.status === 401) {
    if (!isRefreshing) {
      isRefreshing = true;
      refreshPromise = authStore.update().finally(() => {
        isRefreshing = false;
        refreshPromise = null;
      });
    }
    await (refreshPromise ?? Promise.resolve());
    response = await makeRequest(authStore.accessToken, controller.signal);

    if (response.status === 401) {
      try {
        await authStore.updateWithPopup();
        response = await makeRequest(authStore.accessToken, controller.signal);
      } catch {
        // Both silent and popup refresh failed
      }
    }
  }

  clearTimeout(timeout);
  return response;
};
