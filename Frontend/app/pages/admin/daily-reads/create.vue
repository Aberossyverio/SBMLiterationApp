<script setup lang="ts">
import { $authedFetch, handleResponseError } from '~/apis/api'
import DailyReadForm from '~/components/daily-reads/DailyReadForm.vue'
import DashboardNavbar from '~/components/layout/DashboardNavbar.vue'

definePageMeta({
  layout: 'admin',
  middleware: ['auth', 'admin-only']
})

const form = useTemplateRef<typeof DailyReadForm>('form')
const formLoading = ref(false)
const toast = useToast()
const router = useRouter()

async function onSubmit(param: {
  action: 'Create' | 'Update'
  data: {
    title: string
    coverImg: string
    content: string
    date: string
    category: string
    exp: number
    minimalCorrectAnswer: number
  }
  id: number | null
}) {
  try {
    formLoading.value = true
    await $authedFetch('/daily-reads', {
      method: 'POST',
      body: {
        ...param.data
      }
    })

    toast.add({
      title: 'Daily read created successfully',
      color: 'success'
    })

    router.push('/admin/daily-reads')
  } catch (error) {
    handleResponseError(error)
  } finally {
    formLoading.value = false
  }
}
</script>

<template>
  <UDashboardPanel>
    <template #header>
      <DashboardNavbar title="Create Daily Read" />
    </template>

    <template #body>
      <div class="max-w-2xl">
        <div class="flex items-center gap-2 mb-6">
          <UButton
            icon="i-lucide-arrow-left"
            color="neutral"
            variant="ghost"
            to="/admin/daily-reads"
          />
          <h1 class="text-2xl font-bold">
            Create New Daily Read
          </h1>
        </div>

        <DailyReadForm
          ref="form"
          :loading="formLoading"
          @submit="onSubmit"
        />
      </div>
    </template>
  </UDashboardPanel>
</template>
