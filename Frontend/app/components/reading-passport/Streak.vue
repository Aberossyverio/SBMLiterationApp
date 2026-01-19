<script setup lang="ts">
interface WeekDate {
  date: Date
  day: string
  isStreak: boolean
}

defineProps<{
  weekDates: WeekDate[]
}>()
</script>

<template>
  <div class="flex flex-row  items-center gap-x-4">
    <!-- TODO: Change below to the streak fire image -->
    <div>
      Fire Image
    </div>
    <div>
      <p>7 Days Streak. Keep it up!</p>
      <div class="flex flex-row gap-x-2">
        <div
          v-for="(dateItem, index) in weekDates"
          :key="index"
        >
          <p>{{ dateItem.day[0] }}</p>
          <nuxt-icon
            class="text-[20px]"
            :name="dateItem.isStreak ? 'check' : 'no-streak'"
          />
        </div>
      </div>
    </div>
  </div>
</template>
