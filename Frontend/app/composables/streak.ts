const STREAK_STORAGE_KEY = 'user_streak_count'
const TODAY_STREAKED_KEY = 'user_today_streaked'
export const useStreak = defineStore('streak', () => {
  const streakCount = ref(0)
  const todayStreaked = ref(false)

  function loadStreak() {
    const storedStreak = localStorage.getItem(STREAK_STORAGE_KEY)
    const storedTodayStreaked = localStorage.getItem(TODAY_STREAKED_KEY)
    streakCount.value = storedStreak ? parseInt(storedStreak, 10) : 0
    todayStreaked.value = storedTodayStreaked === '1'
  }

  function setStreak(count: number, todayStreakedParam: boolean = true) {
    streakCount.value = count
    localStorage.setItem(STREAK_STORAGE_KEY, count.toString())
    todayStreaked.value = todayStreakedParam
    localStorage.setItem(TODAY_STREAKED_KEY, todayStreakedParam ? '1' : '0')
  }

  return {
    streakCount,
    loadStreak,
    setStreak,
    todayStreaked
  }
})
