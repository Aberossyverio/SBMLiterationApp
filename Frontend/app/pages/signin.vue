<template>
  <div>
    <div class="flex flex-col items-center justify-center gap-4 p-4 h-full">
      <UPageCard class="w-full max-w-md">
        <UAuthForm
          title="Login"
          description="Enter your credentials to access your account."
          icon="i-lucide-user"
          :providers="providers"
          :loading="loading"
        />
      </UPageCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuth } from '~/apis/api'

definePageMeta({
  layout: 'landing',
  middleware: [
    function () {
      if (import.meta.client)
        return

      const auth = useAuth()
      if (!auth.getToken())
        return
      if (auth.getRoles() && auth.getRoles().includes('admin')) {
        return '/admin'
      } else if (auth.getRoles()) {
        return '/dashboard'
      }
    }
  ]
})

const loading = ref(false)
const router = useRouter()
const toast = useToast()
const $api = useNuxtApp().$backendApi
const { loginWithPasskey, isPasskeySupported } = usePasskey()

const providers = ref([
  {
    loading: loading,
    label: 'Passkey',
    icon: 'i-lucide-fingerprint',
    onClick: async () => {
      try {
        loading.value = true
        await loginWithPasskey()
        toast.add({
          title: 'Success',
          description: 'Signed in successfully!',
          color: 'success',
          icon: 'i-lucide-check-circle'
        })
        router.push('/onboarding')
      } catch (error: unknown) {
        const errorMessage = error && typeof error === 'object' && 'message' in error ? String(error.message) : 'Failed to sign in with passkey.'
        toast.add({
          title: 'Error',
          description: errorMessage,
          color: 'error',
          icon: 'i-lucide-circle-off'
        })
      } finally {
        loading.value = false
      }
    }
  },
  {
    loading: loading,
    label: 'Google',
    icon: 'i-simple-icons-google',
    onClick: async () => {
      try {
        loading.value = true
        const result = await $api<{ authUrl: string }>('/auth/google/url')

        if (result.authUrl)
          window.location.href = result.authUrl
      } catch {
        toast.add({
          title: 'Error',
          description: 'Failed to initiate Google sign-in.',
          color: 'error',
          icon: 'i-lucide-circle-off'
        })
      } finally {
        loading.value = false
      }
    }
  }
])

// Remove passkey option if not supported
onMounted(() => {
  if (!isPasskeySupported()) {
    providers.value = providers.value.filter(p => p.label !== 'Passkey')
  }
})
</script>
