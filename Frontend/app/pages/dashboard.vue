<template>
  <div
    class="flex flex-col items-center justify-center gap-4 p-4 h-full"
  >
    <UContainer>
      <UPageHeader
        class="border-none"
        title="Welcome, Budi"
        description="Start your reading journey now"
      />

      <Streak :week-dates="weekDates" />

      <UTabs
        :items="tabs"
        class="max-w-[300px]"
      />

      <ReadingResources
        :resources="resources"
      />
    </UContainer>
  </div>
</template>

<script setup lang="ts">
import ReadingResources from '~/components/reading-passport/ReadingResources.vue'
import Streak from '~/components/reading-passport/Streak.vue'

// definePageMeta({
//   middleware: 'auth'
// })

const getCurrentWeekDates = () => {
  const week = []
  const today = new Date()
  const currentDay = today.getDay()
  const startOfWeek = new Date(today)
  startOfWeek.setDate(today.getDate() - currentDay)

  for (let i = 0; i < 7; i++) {
    const date = new Date(startOfWeek)
    date.setDate(startOfWeek.getDate() + i)
    week.push({
      date: date,
      day: date.toLocaleDateString('en-US', { weekday: 'long' }),
      isStreak: Math.random() > 0.5 // Replace with actual streak logic
    })
  }
  return week
}

const weekDates = ref<{
  date: Date
  day: string
  isStreak: boolean
}[]>([])

onMounted(() => {
  weekDates.value = getCurrentWeekDates()
})

const resources = ref([
  {
    title: 'The Great Gatsby',
    imageUrl: 'https://picsum.photos/seed/book1/400/300',
    totalPages: 180,
    totalReadPages: 125,
    type: 'book' as const
  },
  {
    title: 'Machine Learning Fundamentals',
    imageUrl: 'https://picsum.photos/seed/journal1/400/300',
    totalPages: 45,
    totalReadPages: 32,
    type: 'journal' as const
  },
  {
    title: '1984',
    imageUrl: 'https://picsum.photos/seed/book2/400/300',
    totalPages: 328,
    totalReadPages: 89,
    type: 'book' as const
  }
])

const tabs = [
  {
    label: 'Books',
    icon: 'i-lucide-book',
    slot: 'books'
  },
  {
    label: 'Research Article',
    icon: 'i-lucide-form',
    slot: 'journal-paper'
  }
]
</script>
