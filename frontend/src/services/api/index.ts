import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

export class ApiSingleton {
  private static instance: Client | null = null;

  public static async getInstance() {
    const authStore = useAuthStore();

    if (this.instance) {
      (this.instance as any).headers = {
        Authorization: `Bearer ${authStore.accessToken}`,
      };
      return this.instance;
    }

    const api = createClient<paths>({
      baseUrl: import.meta.env.VITE_API_URL || "http://localhost:5165/",
      headers: {
        Authorization: `Bearer ${authStore.accessToken}`,
      },
    });

    this.instance = api;

    return api;
  }

  public static reset() {
    this.instance = null;
  }
}
