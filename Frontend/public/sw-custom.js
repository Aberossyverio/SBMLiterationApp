// Custom service worker code for handling reminder notifications

self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SHOW_REMINDER') {
    const { title, body, icon, badge, tag, data } = event.data
    
    self.registration.showNotification(title, {
      body,
      icon,
      badge,
      tag,
      data,
      requireInteraction: false,
      vibrate: [200, 100, 200],
      actions: [
        {
          action: 'open',
          title: 'Open App'
        },
        {
          action: 'dismiss',
          title: 'Dismiss'
        }
      ]
    })
  }
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  
  if (event.action === 'open' || !event.action) {
    const urlToOpen = event.notification.data?.url || '/'
    
    event.waitUntil(
      clients.matchAll({ type: 'window', includeUncontrolled: true })
        .then((clientList) => {
          // Check if there's already a window open
          for (const client of clientList) {
            if (client.url === urlToOpen && 'focus' in client) {
              return client.focus()
            }
          }
          // If no window is open, open a new one
          if (clients.openWindow) {
            return clients.openWindow(urlToOpen)
          }
        })
    )
  }
  // If action is 'dismiss', just close the notification (already done above)
})

self.addEventListener('push', (event) => {
  // Handle push notifications from server if needed in the future
  if (event.data) {
    const data = event.data.json()
    
    const options = {
      body: data.body || 'You have a new notification',
      icon: data.icon || '/icons/pwa-192x192.png',
      badge: data.badge || '/icons/pwa-64x64.png',
      tag: data.tag || 'general-notification',
      data: data.data || {},
      requireInteraction: false,
      vibrate: [200, 100, 200]
    }
    
    event.waitUntil(
      self.registration.showNotification(data.title || 'SIGMA', options)
    )
  }
})
