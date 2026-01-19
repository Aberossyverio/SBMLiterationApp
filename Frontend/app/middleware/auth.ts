import { useAuth } from '~/apis/api'

export default defineNuxtRouteMiddleware(() => {
  if (import.meta.server) {
    return
  }
  const auth = useAuth()
  const token = auth.getToken()

  if (!token) {
    return navigateTo('/')
  }
})
