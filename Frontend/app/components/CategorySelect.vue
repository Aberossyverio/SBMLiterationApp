<script setup lang="ts">
import { $authedFetch, handleResponseError } from '~/apis/api'
import type { PagingResult } from '~/apis/paging'

interface ReadingCategory {
  id: number
  categoryName: string
}

interface Props {
  modelValue: string
  required?: boolean
  label?: string
  name?: string
  placeholder?: string
  size?: 'sm' | 'md' | 'lg'
}

const props = withDefaults(defineProps<Props>(), {
  required: false,
  label: 'Category',
  name: 'category',
  placeholder: 'Select category',
  size: 'md'
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const categories = ref<ReadingCategory[]>([])
const categoriesLoading = ref(false)
const selectedCategory = ref('')
const customCategory = ref('')

const categoryOptions = computed(() => [
  ...categories.value.map(cat => ({
    value: cat.categoryName,
    label: cat.categoryName
  })),
  { value: 'Other', label: 'Other (Custom)' }
])

const isCustomCategory = computed(() => selectedCategory.value === 'Other')

// Sync internal state with external modelValue
watch(() => props.modelValue, (newValue) => {
  if (!newValue) {
    selectedCategory.value = ''
    customCategory.value = ''
    return
  }

  // Check if the value exists in categories
  const categoryExists = categories.value.some(cat => cat.categoryName === newValue)
  if (categoryExists) {
    selectedCategory.value = newValue
    customCategory.value = ''
  } else {
    selectedCategory.value = 'Other'
    customCategory.value = newValue
  }
}, { immediate: true })

// Emit the actual category value (not "Other", but the custom value if applicable)
watch([selectedCategory, customCategory], () => {
  const actualValue = isCustomCategory.value ? customCategory.value : selectedCategory.value
  if (actualValue !== props.modelValue) {
    emit('update:modelValue', actualValue)
  }
}, { flush: 'post' })

async function fetchCategories() {
  try {
    categoriesLoading.value = true
    const response = await $authedFetch<PagingResult<ReadingCategory>>('/reading-categories', {
      query: {
        page: 1,
        pageSize: 100
      }
    })
    if (response.rows) {
      categories.value = response.rows
    }
  } catch (err) {
    handleResponseError(err)
  } finally {
    categoriesLoading.value = false
  }
}

onMounted(() => {
  fetchCategories()
})
</script>

<template>
  <div class="space-y-4">
    <UFormField
      :label="label"
      :name="isCustomCategory ? undefined : name"
      :required="isCustomCategory ? false : required"
    >
      <USelectMenu
        :model-value="categoryOptions.find(opt => opt.value === selectedCategory)"
        :items="categoryOptions"
        :loading="categoriesLoading"
        :placeholder="placeholder"
        :size="size"
        class="w-full"
        @update:model-value="(selected) => selectedCategory = selected.value"
      />
    </UFormField>

    <UFormField
      v-if="isCustomCategory"
      label="Custom Category"
      :name="name"
      :required="required"
    >
      <UInput
        v-model="customCategory"
        placeholder="Enter custom category name"
        :size="size"
        class="w-full"
        @input="emit('update:modelValue', customCategory)"
      />
    </UFormField>
  </div>
</template>
