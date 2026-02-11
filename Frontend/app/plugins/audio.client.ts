export default defineNuxtPlugin(async () => {
  const sound = useSound()

  const essentialSounds = [
    '/sounds/book-complete.wav',
    '/sounds/report.wav',
    '/sounds/streak.wav'
  ]

  // Non-blocking: preload in background
  sound.preloadSounds(essentialSounds).catch((err) => {
    console.error('Sound preload failed:', err)
  })
  const streak = useStreak()
  streak.loadStreak()
  // Or blocking: wait for preload before app is ready
  // await preloadSounds(essentialSounds)
})
