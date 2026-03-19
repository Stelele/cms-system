<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";

const authStore = useAuthStore();

onBeforeMount(async () => {
    await authStore.update();
});
</script>
