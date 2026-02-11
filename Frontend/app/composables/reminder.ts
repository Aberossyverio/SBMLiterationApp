export function useReminder() {
  const LAST_OPENED_KEY = 'lastOpened'
  const REMINDER_INTERVAL = 24 * 60 * 60 * 1000 // 24 hours in milliseconds

  /**
   * Initialize the reminder system on app launch
   */
  function initializeReminder() {
    if (import.meta.server) return

    // Update last opened timestamp
    updateLastOpened()

    // Request notification permission if not already granted
    requestNotificationPermission()

    // Set up periodic check for reminders
    setupReminderCheck()
  }

  /**
   * Update the last opened timestamp in localStorage
   */
  function updateLastOpened() {
    if (import.meta.server) return

    const now = Date.now()
    localStorage.setItem(LAST_OPENED_KEY, now.toString())
  }

  /**
   * Get the last opened timestamp from localStorage
   */
  function getLastOpened(): number | null {
    if (import.meta.server) return null

    const lastOpened = localStorage.getItem(LAST_OPENED_KEY)
    return lastOpened ? parseInt(lastOpened, 10) : null
  }

  /**
   * Request notification permission from the user
   */
  async function requestNotificationPermission() {
    if (import.meta.server) return false

    if (!('Notification' in window)) {
      console.warn('This browser does not support notifications')
      return false
    }

    if (Notification.permission === 'granted') {
      return true
    }

    if (Notification.permission !== 'denied') {
      const permission = await Notification.requestPermission()
      return permission === 'granted'
    }

    return false
  }

  /**
   * Check if 24 hours have passed since last opened
   */
  function shouldSendReminder(): boolean {
    if (import.meta.server) return false

    const lastOpened = getLastOpened()
    if (!lastOpened) return false

    const now = Date.now()
    const timeDiff = now - lastOpened

    return timeDiff >= REMINDER_INTERVAL
  }

  /**
   * Send a push notification reminder
   */
  async function sendReminderNotification() {
    if (import.meta.server) return

    if (Notification.permission !== 'granted') {
      console.warn('Notification permission not granted')
      return
    }

    // Check if service worker is available
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
      // Send message to service worker to show notification
      navigator.serviceWorker.controller.postMessage({
        type: 'SHOW_REMINDER',
        title: 'Come back to SIGMA! 📚',
        body: 'It\'s been 24 hours since your last visit. Ready to continue your reading journey?',
        icon: '/icons/pwa-192x192.png',
        badge: '/icons/pwa-64x64.png',
        tag: 'reading-reminder',
        data: {
          url: '/'
        }
      })
    } else {
      // Fallback to regular notification if service worker is not available
      new Notification('Come back to SIGMA! 📚', {
        body: 'It\'s been 24 hours since your last visit. Ready to continue your reading journey?',
        icon: '/icons/pwa-192x192.png',
        badge: '/icons/pwa-64x64.png',
        tag: 'reading-reminder',
        data: {
          url: '/'
        }
      })
    }
  }

  /**
   * Set up periodic check for reminders
   */
  function setupReminderCheck() {
    if (import.meta.server) return

    // Check immediately
    if (shouldSendReminder()) {
      sendReminderNotification()
    }

    // Set up interval to check every hour
    const checkInterval = 60 * 60 * 1000 // 1 hour
    setInterval(() => {
      if (shouldSendReminder()) {
        sendReminderNotification()
      }
    }, checkInterval)

    // Register visibility change listener to update last opened when app becomes visible
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        updateLastOpened()
      }
    })

    // Register before unload to ensure we catch app closes
    window.addEventListener('beforeunload', () => {
      updateLastOpened()
    })
  }

  /**
   * Clear the last opened timestamp (useful for testing or reset)
   */
  function clearLastOpened() {
    if (import.meta.server) return
    localStorage.removeItem(LAST_OPENED_KEY)
  }

  /**
   * Get time until next reminder in milliseconds
   */
  function getTimeUntilNextReminder(): number | null {
    if (import.meta.server) return null

    const lastOpened = getLastOpened()
    if (!lastOpened) return null

    const now = Date.now()
    const nextReminderTime = lastOpened + REMINDER_INTERVAL
    const timeUntilNext = nextReminderTime - now

    return timeUntilNext > 0 ? timeUntilNext : 0
  }

  return {
    initializeReminder,
    updateLastOpened,
    getLastOpened,
    requestNotificationPermission,
    shouldSendReminder,
    sendReminderNotification,
    clearLastOpened,
    getTimeUntilNextReminder
  }
}
