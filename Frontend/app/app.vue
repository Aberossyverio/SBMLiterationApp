<script setup lang="ts">
const { initializeReminder } = useReminder()

onMounted(() => {
  initializeReminder()
})
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
    <NuxtPwaManifest />
  </NuxtLayout>
</template>
