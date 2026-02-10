<script setup lang="ts">
import { $authedFetch, handleResponseError } from '~/apis/api'
import DailyReadForm from '~/components/daily-reads/DailyReadForm.vue'
import DailyReadQuizForm from '~/components/daily-reads/DailyReadQuizForm.vue'
import DashboardNavbar from '~/components/layout/DashboardNavbar.vue'

definePageMeta({
  layout: 'admin',
  middleware: ['auth', 'admin-only']
})

const quizForm = useTemplateRef<typeof DailyReadQuizForm>('quizForm')
const form = useTemplateRef<typeof DailyReadForm>('form')
const formLoading = ref(false)
const toast = useToast()
const router = useRouter()
const dailyReadId = ref<number | null>(null)

const tabs = [
  {
    label: 'Daily Read Details',
    slot: 'details',
    icon: 'i-heroicons-document-text'
  },
  {
    label: 'Quiz Questions',
    slot: 'quiz',
    icon: 'i-heroicons-question-mark-circle'
  }
]

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

    // First, create the daily read
    const response = await $authedFetch<{ data: { id: number } }>('/daily-reads', {
      method: 'POST',
      body: {
        ...param.data
      }
    })

    dailyReadId.value = response.data?.id

    if (!dailyReadId.value) {
      handleResponseError(response)
      return
    }

    // Then, upload quiz if file is selected
    const quizFile = quizForm.value?.getQuizFile()
    if (quizFile) {
      const formData = new FormData()
      formData.append('file', quizFile)

      await $authedFetch(`/daily-reads/${dailyReadId.value}/quiz/upload`, {
        method: 'POST',
        body: formData
      })

      quizForm.value?.refresh()
      quizForm.value?.clearQuizFile()
    }

    // Then, submit bulk paste if exists
    if (quizForm.value?.hasBulkPaste()) {
      await quizForm.value?.submitBulkPaste(dailyReadId.value)
      quizForm.value?.refresh()
    }

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
      <DashboardNavbar />
    </template>

    <template #body>
      <div>
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

        <UTabs
          :items="tabs"
          :unmount-on-hide="false"
        >
          <template #details>
            <DailyReadForm
              ref="form"
              :loading="formLoading"
              @submit="onSubmit"
            />
          </template>

          <template #quiz>
            <DailyReadQuizForm
              ref="quizForm"
              :daily-read-id="dailyReadId"
              no-submit
            />
          </template>
        </UTabs>
      </div>
    </template>
  </UDashboardPanel>
</template>
