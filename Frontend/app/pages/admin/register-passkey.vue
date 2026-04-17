<script setup lang="ts">
import DashboardNavbar from '~/components/layout/DashboardNavbar.vue'
import { useAuth, $authedFetch } from '~/apis/api'

definePageMeta({
  layout: 'admin',
  middleware: ['auth', 'admin-only']
})

interface PasskeyItem {
  id: number
  deviceName: string
  createdAt: string
  lastUsedAt: string | null
}

const registerLoading = ref(false)
const registerEmail = ref('')
const registerDisplayName = ref('')
const passkeys = ref<PasskeyItem[]>([])
const loadingPasskeys = ref(false)
const toast = useToast()
const { registerPasskey } = usePasskey()
const auth = useAuth()
const dialog = useDialog()

onMounted(() => {
  // Pre-fill email from JWT token
  const token = auth.getToken()
  if (token) {
    const parts = token.split('.')
    if (parts.length === 3 && parts[1]) {
      const jwtPayload = JSON.parse(atob(parts[1]))
      registerEmail.value = jwtPayload.email || jwtPayload.sub || ''
    }
  }
  loadPasskeys()
})

async function loadPasskeys() {
  try {
    loadingPasskeys.value = true
    const response = await $authedFetch<{ data: PasskeyItem[], message: string }>('/auth/passkey/list')
    if (response && response.data) {
      passkeys.value = response.data
    }
  } catch (error) {
    console.error('Failed to load passkeys:', error)
  } finally {
    loadingPasskeys.value = false
  }
}

async function handleRegisterPasskey() {
  if (!registerEmail.value) {
    toast.add({
      title: 'Error',
      description: 'Email is required',
      color: 'error',
      icon: 'i-lucide-circle-off'
    })
    return
  }

  try {
    registerLoading.value = true
    await registerPasskey(registerEmail.value, registerDisplayName.value || undefined)

    toast.add({
      title: 'Success',
      description: 'Passkey registered successfully!',
      color: 'success',
      icon: 'i-lucide-check-circle'
    })

    registerDisplayName.value = ''
    await loadPasskeys()
  } catch (error: unknown) {
    const errorMessage = error && typeof error === 'object' && 'message' in error ? String(error.message) : 'Failed to register passkey'
    toast.add({
      title: 'Error',
      description: errorMessage,
      color: 'error',
      icon: 'i-lucide-circle-off'
    })
  } finally {
    registerLoading.value = false
  }
}

function handleDeletePasskey(passkey: PasskeyItem) {
  dialog.confirm({
    title: 'Delete Passkey',
    subTitle: 'This action cannot be undone',
    message: `Are you sure you want to delete passkey "${passkey.deviceName}"?`,
    onOk: async () => {
      try {
        await $authedFetch(`/auth/passkey/${passkey.id}`, {
          method: 'DELETE'
        })

        toast.add({
          title: 'Success',
          description: 'Passkey deleted successfully',
          color: 'success',
          icon: 'i-lucide-check-circle'
        })
        await loadPasskeys()
      } catch {
        toast.add({
          title: 'Error',
          description: 'Failed to delete passkey',
          color: 'error',
          icon: 'i-lucide-circle-off'
        })
      }
    }
  })
}

function formatDate(dateString: string | null) {
  if (!dateString) return 'Never'
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<template>
  <UDashboardPanel>
    <template #header>
      <DashboardNavbar title="Passkey Management" />
    </template>

    <template #body>
      <div class="max-w-4xl mx-auto p-6 space-y-6">
        <!-- Register New Passkey Card -->
        <UCard>
          <template #header>
            <div class="flex items-center gap-2">
              <UIcon
                name="i-lucide-fingerprint"
                class="size-6"
              />
              <h2 class="text-xl font-semibold">
                Register New Passkey
              </h2>
            </div>
          </template>

          <div class="space-y-6">
            <div class="p-4 rounded-lg bg-primary-50 dark:bg-primary-950 border border-primary-200 dark:border-primary-800">
              <h3 class="font-medium text-primary-900 dark:text-primary-100 mb-2">
                What is a Passkey?
              </h3>
              <p class="text-sm text-primary-700 dark:text-primary-300 mb-3">
                Passkeys let you sign in using your device's biometric authentication (fingerprint, face recognition) or PIN. It's faster and more secure than passwords.
              </p>
              <ul class="text-sm text-primary-700 dark:text-primary-300 space-y-1 list-disc list-inside">
                <li>Sign in with Windows Hello PIN, fingerprint, or face recognition</li>
                <li>Use your phone's biometric authentication</li>
                <li>Works with USB security keys</li>
              </ul>
            </div>

            <UFormField
              label="Email"
              required
              class="w-full"
            >
              <UInput
                v-model="registerEmail"
                type="email"
                placeholder="your.email@example.com"
                icon="i-lucide-mail"
                class="w-full"
              />
            </UFormField>

            <UFormField
              label="Display Name (Optional)"
              class="w-full"
            >
              <UInput
                v-model="registerDisplayName"
                placeholder="Your Name"
                icon="i-lucide-user"
                class="w-full"
              />
            </UFormField>

            <div class="p-4 rounded-lg bg-blue-50 dark:bg-blue-950 border border-blue-200 dark:border-blue-800">
              <p class="text-sm text-blue-700 dark:text-blue-300">
                💡 After clicking register, your browser will prompt you to authenticate using your device's biometric or PIN.
              </p>
            </div>
          </div>

          <template #footer>
            <div class="flex justify-end">
              <UButton
                color="primary"
                icon="i-lucide-fingerprint"
                size="lg"
                :loading="registerLoading"
                :disabled="registerLoading"
                @click="handleRegisterPasskey"
              >
                Register Passkey
              </UButton>
            </div>
          </template>
        </UCard>

        <!-- Registered Passkeys List -->
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <UIcon
                  name="i-lucide-key-round"
                  class="size-6"
                />
                <h2 class="text-xl font-semibold">
                  Your Passkeys
                </h2>
              </div>
              <div class="flex items-center gap-2">
                <UButton
                  color="neutral"
                  variant="ghost"
                  icon="i-heroicons-arrow-path"
                  :loading="loadingPasskeys"
                  @click="loadPasskeys"
                >
                  Refresh
                </UButton>
                <UBadge
                  color="primary"
                  variant="subtle"
                  size="lg"
                >
                  {{ passkeys.length }}
                </UBadge>
              </div>
            </div>
          </template>

          <div
            v-if="loadingPasskeys"
            class="flex items-center justify-center py-12"
          >
            <UIcon
              name="i-heroicons-arrow-path"
              class="animate-spin text-4xl"
            />
          </div>

          <div
            v-else-if="passkeys.length === 0"
            class="text-center py-12"
          >
            <UIcon
              name="i-lucide-key-round"
              class="size-12 text-gray-400 mx-auto mb-4"
            />
            <p class="text-gray-600 dark:text-gray-400">
              No passkeys registered yet. Register your first passkey above.
            </p>
          </div>

          <div
            v-else
            class="space-y-3"
          >
            <div
              v-for="passkey in passkeys"
              :key="passkey.id"
              class="flex items-center justify-between p-4 rounded-lg border-2 border-gray-200 dark:border-gray-700 hover:border-primary-300 dark:hover:border-primary-700 transition-colors"
            >
              <div class="flex-1">
                <div class="flex items-center gap-3 mb-2">
                  <UIcon
                    name="i-lucide-fingerprint"
                    class="size-5 text-primary-600"
                  />
                  <h3 class="font-medium">
                    {{ passkey.deviceName }}
                  </h3>
                </div>
                <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-600 dark:text-gray-400">
                  <span class="flex items-center gap-1">
                    <UIcon
                      name="i-lucide-calendar"
                      class="size-3"
                    />
                    Created: {{ formatDate(passkey.createdAt) }}
                  </span>
                  <span class="flex items-center gap-1">
                    <UIcon
                      name="i-lucide-clock"
                      class="size-3"
                    />
                    Last used: {{ formatDate(passkey.lastUsedAt) }}
                  </span>
                </div>
              </div>
              <UButton
                color="error"
                variant="ghost"
                icon="i-lucide-trash-2"
                size="lg"
                class="cursor-pointer"
                @click="handleDeletePasskey(passkey)"
              />
            </div>
          </div>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
