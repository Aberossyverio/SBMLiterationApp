<script setup lang="ts">
import { $authedFetch, handleResponseError } from '~/apis/api'
import type { EditorToolbarItem } from '@nuxt/ui'
import type { EditorView } from '@tiptap/pm/view'

const props = defineProps<{
  modelValue: string
  placeholder?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const content = computed({
  get: () => props.modelValue,
  set: value => emit('update:modelValue', value)
})

const contentImageUploading = ref(false)
const toast = useToast()

const toolbarItems: EditorToolbarItem[][] = [
  [
    {
      icon: 'i-lucide-heading',
      tooltip: { text: 'Headings' },
      content: {
        align: 'start'
      },
      items: [
        {
          kind: 'heading',
          level: 1,
          icon: 'i-lucide-heading-1',
          label: 'Heading 1'
        },
        {
          kind: 'heading',
          level: 2,
          icon: 'i-lucide-heading-2',
          label: 'Heading 2'
        },
        {
          kind: 'heading',
          level: 3,
          icon: 'i-lucide-heading-3',
          label: 'Heading 3'
        }
      ]
    }
  ],
  [
    {
      kind: 'mark',
      mark: 'bold',
      icon: 'i-lucide-bold',
      tooltip: { text: 'Bold' }
    },
    {
      kind: 'mark',
      mark: 'italic',
      icon: 'i-lucide-italic',
      tooltip: { text: 'Italic' }
    },
    {
      kind: 'mark',
      mark: 'underline',
      icon: 'i-lucide-underline',
      tooltip: { text: 'Underline' }
    },
    {
      kind: 'mark',
      mark: 'strike',
      icon: 'i-lucide-strikethrough',
      tooltip: { text: 'Strikethrough' }
    },
    {
      kind: 'mark',
      mark: 'code',
      icon: 'i-lucide-code',
      tooltip: { text: 'Code' }
    }
  ],
  [
    {
      kind: 'bulletList',
      icon: 'i-lucide-list',
      tooltip: { text: 'Bullet List' }
    },
    {
      kind: 'orderedList',
      icon: 'i-lucide-list-ordered',
      tooltip: { text: 'Numbered List' }
    }
  ],
  [
    {
      kind: 'blockquote',
      icon: 'i-lucide-quote',
      tooltip: { text: 'Blockquote' }
    },
    {
      kind: 'codeBlock',
      icon: 'i-lucide-square-code',
      tooltip: { text: 'Code Block' }
    },
    {
      kind: 'link',
      icon: 'i-lucide-link',
      tooltip: { text: 'Link' }
    },
    {
      kind: 'image',
      icon: 'i-lucide-image',
      tooltip: { text: 'Image' }
    }
  ]
]

function handleContentImagePaste(view: EditorView, event: ClipboardEvent) {
  const items = event.clipboardData?.items
  if (!items) return false

  for (const item of Array.from(items)) {
    if (item.type.startsWith('image/')) {
      event.preventDefault()

      const file = item.getAsFile()
      if (!file) continue

      const formData = new FormData()
      formData.append('file', file)

      ;(async () => {
        try {
          contentImageUploading.value = true
          const response = await $authedFetch<{
            message: string
            data: {
              url: string
              fileName: string
              fileSize: number
              contentType: string
            }
            errorCode?: string
            errorDescription?: string
            errors?: string[]
          }>('/files/upload', {
            method: 'POST',
            body: formData
          })

          if (response.data?.url) {
          // Insert the image at the current cursor position
            const { schema } = view.state
            const imageNode = schema.nodes.image?.create({ src: response.data.url })

            if (!imageNode) return
            const transaction = view.state.tr.replaceSelectionWith(imageNode)
            view.dispatch(transaction)

            toast.add({
              title: 'Image uploaded and inserted',
              color: 'success'
            })
          }
        } catch (error) {
          handleResponseError(error)
        } finally {
          contentImageUploading.value = false
        }
      })()

      return true
    }
  }

  return false
}
</script>

<template>
  <div class="relative">
    <UEditor
      v-slot="{ editor }"
      v-model="content"
      :placeholder="placeholder || 'Start writing...'"
      config="starter-kit"
      class="w-full min-h-64 border border-gray-300 dark:border-gray-700 rounded-md"
      :ui="{
        content: 'py-4'
      }"
      :editor-props="{
        handlePaste: handleContentImagePaste
      }"
      editable
      content-type="markdown"
    >
      <UEditorToolbar
        :editor="editor"
        :items="toolbarItems"
        layout="fixed"
        class="border-b border-gray-200 dark:border-gray-700"
      />
      <UEditorDragHandle :editor="editor" />
    </UEditor>
    <div
      v-if="contentImageUploading"
      class="absolute inset-0 flex items-center justify-center bg-black/10 rounded-md pointer-events-none"
    >
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-4 pointer-events-auto">
        <div class="flex items-center gap-3">
          <UIcon
            name="i-lucide-loader-circle"
            class="animate-spin text-2xl text-primary"
          />
          <span class="text-sm text-gray-600 dark:text-gray-400">Uploading image...</span>
        </div>
      </div>
    </div>
  </div>
</template>
