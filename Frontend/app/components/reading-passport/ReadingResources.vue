<template>
  <div class="flex flex-row gap-x-4">
    <UCard
      v-for="res in resources"
      :key="res.title"
    >
      <template #header>
        <UPageHeader
          :title="res.title"
        />
      </template>

      <img
        :src="res.imageUrl"
        alt="Resource Image"
        class="w-full h-48 object-cover rounded-md"
      >

      <template #footer>
        <div class="flex flex-row justify-between">
          <div>
            {{ res.type === 'book' ? 'Book' : 'Journal' }}
          </div>
          <div class="text-sm text-gray-500">
            {{ res.totalReadPages }} of {{ res.totalPages }} pages read
          </div>
        </div>
      </template>
    </UCard>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  resources: {
    title: string
    imageUrl: string
    totalPages: number
    totalReadPages: number
    type: 'book' | 'journal'
  }[]
}>()
</script>
