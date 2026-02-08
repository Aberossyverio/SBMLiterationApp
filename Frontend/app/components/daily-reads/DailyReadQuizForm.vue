<script setup lang="ts">
import { $authedFetch, handleResponseError, type ApiResponse } from '~/apis/api'

interface QuizChoice {
  id?: number
  choice: string
  answer: string
}

interface QuizQuestion {
  id?: number
  questionSeq: number
  question: string
  correctAnswer: string
  choices: QuizChoice[]
}

interface EditFormState {
  question: string
  correctAnswer: string
  choices: QuizChoice[]
}

const props = defineProps<{
  dailyReadId: number | null
}>()

const quiz = ref<QuizQuestion[]>([])
const pending = ref(false)
const toast = useToast()
const quizFile = ref<File | null>(null)
const editingQuestionSeq = ref<number | null>(null)
const editForm = reactive<EditFormState>({
  question: '',
  correctAnswer: '',
  choices: []
})
const bulkPasteText = ref('')
const showBulkPaste = ref(false)
const saving = ref(false)

async function handleQuizFileUpload(files: File[]) {
  if (!files || !files[0] || files.length === 0) return
  quizFile.value = files[0]
  toast.add({
    title: 'Quiz file selected',
    description: `${files[0].name} ready to upload`,
    color: 'success'
  })
}

function getQuizFile() {
  return quizFile.value
}

function clearQuizFile() {
  quizFile.value = null
}

async function downloadQuizTemplate() {
  try {
    const response = await $authedFetch('/daily-reads/quiz/template', {
      responseType: 'blob'
    })

    // Create download link
    const url = window.URL.createObjectURL(new Blob([response as Blob]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', 'quiz-template.xlsx')
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)

    toast.add({
      title: 'Template downloaded',
      color: 'success'
    })
  } catch (error) {
    handleResponseError(error)
  }
}

async function fetchQuiz() {
  if (!props.dailyReadId) {
    quiz.value = []
    return
  }

  try {
    pending.value = true
    const response = await $authedFetch<ApiResponse<QuizQuestion[]>>(
      `/daily-reads/${props.dailyReadId}/quiz/review`
    )

    if (response.data && Array.isArray(response.data)) {
      quiz.value = response.data.sort((a, b) => a.questionSeq - b.questionSeq)
    } else {
      quiz.value = []
    }
  } catch {
    // Quiz might not exist yet, that's okay
    quiz.value = []
  } finally {
    pending.value = false
  }
}

function startEdit(question: QuizQuestion) {
  if (editingQuestionSeq.value !== null) {
    saveQuestion()
  }

  editingQuestionSeq.value = question.questionSeq
  editForm.question = question.question
  editForm.correctAnswer = question.correctAnswer
  editForm.choices = JSON.parse(JSON.stringify(question.choices))
}

function cancelEdit() {
  editingQuestionSeq.value = null
  editForm.question = ''
  editForm.correctAnswer = ''
  editForm.choices = []
}

async function saveQuestion() {
  if (editingQuestionSeq.value === null || !props.dailyReadId) return

  if (!editForm.question.trim()) {
    toast.add({ title: 'Question cannot be empty', color: 'error' })
    return
  }

  if (editForm.choices.length === 0) {
    toast.add({ title: 'At least one choice is required', color: 'error' })
    return
  }

  if (!editForm.correctAnswer) {
    toast.add({ title: 'Please select a correct answer', color: 'error' })
    return
  }

  try {
    saving.value = true
    const questionSeq = editingQuestionSeq.value
    const existingQuestion = quiz.value.find(q => q.questionSeq === questionSeq)

    const payload = {
      questionSeq,
      question: editForm.question,
      correctAnswer: editForm.correctAnswer,
      choices: editForm.choices.map(c => ({
        choice: c.choice,
        answer: c.answer
      }))
    }

    if (existingQuestion && existingQuestion.id) {
      // Update existing question
      await $authedFetch(
        `/daily-reads/${props.dailyReadId}/quiz/${questionSeq}`,
        {
          method: 'PUT',
          body: {
            question: payload.question,
            correctAnswer: payload.correctAnswer,
            choices: payload.choices
          }
        }
      )
    } else {
      // Create new question
      await $authedFetch(
        `/daily-reads/${props.dailyReadId}/quiz`,
        {
          method: 'POST',
          body: payload
        }
      )
    }

    toast.add({ title: 'Question saved', color: 'success' })
    await fetchQuiz()
    cancelEdit()
  } catch (error) {
    handleResponseError(error)
  } finally {
    saving.value = false
  }
}

function addChoice() {
  const nextLetter = String.fromCharCode(65 + editForm.choices.length) // A, B, C...
  editForm.choices.push({ choice: nextLetter, answer: '' })

  if (!editForm.correctAnswer && editForm.choices.length === 1) {
    editForm.correctAnswer = nextLetter
  }
}

function removeChoice(index: number) {
  const removedChoice = editForm.choices[index]
  editForm.choices.splice(index, 1)

  // Reassign choice letters
  editForm.choices.forEach((choice, idx) => {
    choice.choice = String.fromCharCode(65 + idx)
  })

  // Update correct answer if it was the removed choice
  if (editForm.correctAnswer === removedChoice?.choice) {
    editForm.correctAnswer = editForm.choices.length > 0 ? editForm.choices[0]!.choice : ''
  } else if (editForm.choices.length > 0 && removedChoice) {
    // Update correct answer letter if it changed
    const correctChoiceIndex = editForm.choices.findIndex(c => c.answer === removedChoice?.answer)
    if (correctChoiceIndex === -1) {
      const oldIndex = removedChoice.choice.charCodeAt(0) - 65
      if (oldIndex < editForm.choices.length) {
        editForm.correctAnswer = editForm.choices[oldIndex]?.choice || ''
      }
    }
  }
}

function addNewQuestion() {
  if (editingQuestionSeq.value !== null) {
    saveQuestion()
  }

  const nextSeq = quiz.value.length > 0
    ? Math.max(...quiz.value.map(q => q.questionSeq)) + 1
    : 1

  editingQuestionSeq.value = nextSeq
  editForm.question = ''
  editForm.correctAnswer = 'A'
  editForm.choices = [
    { choice: 'A', answer: '' }
  ]
}

function parseBulkPaste() {
  if (!bulkPasteText.value.trim()) {
    toast.add({ title: 'Please enter questions to paste', color: 'error' })
    return
  }

  const lines = bulkPasteText.value.trim().split('\n')
  const parsedQuestions: QuizQuestion[] = []

  for (const line of lines) {
    if (!line.trim()) continue

    const parts = line.split('\t')
    if (parts.length < 4) {
      toast.add({
        title: 'Invalid format',
        description: 'Each line should have at least: questionSeq, question, correctAnswer, and one answer',
        color: 'error'
      })
      return
    }

    const questionSeq = parseInt(parts[0] ?? '1')
    const question = parts[1]
    const correctAnswer = parts[2]
    const answers = parts.slice(3)

    if (isNaN(questionSeq)) {
      toast.add({
        title: 'Invalid question sequence',
        description: `"${parts[0]}" is not a valid number`,
        color: 'error'
      })
      return
    }

    const choices: QuizChoice[] = answers.map((answer, idx) => ({
      choice: String.fromCharCode(65 + idx),
      answer: answer.trim()
    }))

    parsedQuestions.push({
      questionSeq,
      question: question || '',
      correctAnswer: correctAnswer || 'A',
      choices
    })
  }

  // Add parsed questions to quiz
  quiz.value = [...quiz.value, ...parsedQuestions].sort((a, b) => a.questionSeq - b.questionSeq)

  toast.add({
    title: `${parsedQuestions.length} question(s) added`,
    description: 'Click edit to save each question',
    color: 'success'
  })

  bulkPasteText.value = ''
  showBulkPaste.value = false
}

async function deleteQuestion(questionSeq: number) {
  if (!props.dailyReadId) return

  try {
    await $authedFetch(
      `/daily-reads/${props.dailyReadId}/quiz/${questionSeq}`,
      { method: 'DELETE' }
    )

    toast.add({ title: 'Question deleted', color: 'success' })
    await fetchQuiz()
  } catch (error) {
    handleResponseError(error)
  }
}

watch(() => props.dailyReadId, () => {
  fetchQuiz()
}, { immediate: true })

defineExpose({
  refresh: fetchQuiz,
  getQuizFile,
  clearQuizFile
})
</script>

<template>
  <div class="flex flex-col">
    <div class="mb-4 space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-lg font-semibold">
          Quiz Questions
        </h3>
        <div class="flex items-center gap-2">
          <UButton
            icon="i-heroicons-document-duplicate"
            size="xs"
            color="neutral"
            variant="outline"
            @click="showBulkPaste = !showBulkPaste"
          >
            Bulk Paste
          </UButton>
          <UButton
            icon="i-heroicons-arrow-down-tray"
            size="xs"
            color="neutral"
            variant="outline"
            @click="downloadQuizTemplate"
          >
            Template
          </UButton>
        </div>
      </div>
      <p class="text-sm text-gray-500">
        {{ dailyReadId ? 'Edit questions inline or upload a file' : 'Save daily read first to add quiz' }}
      </p>

      <!-- Bulk Paste Section -->
      <div
        v-if="showBulkPaste"
        class="p-4 border border-gray-200 dark:border-gray-700 rounded-lg"
      >
        <div class="mb-2 flex items-center justify-between">
          <label class="text-sm font-medium">Bulk Paste Questions</label>
          <UButton
            icon="i-heroicons-x-mark"
            size="xs"
            color="neutral"
            variant="ghost"
            @click="showBulkPaste = false"
          />
        </div>
        <p class="text-xs text-gray-500 mb-3">
          Format: questionSeq[TAB]question[TAB]correctAnswer[TAB]answer1[TAB]answer2...
        </p>
        <UTextarea
          v-model="bulkPasteText"
          :rows="5"
          class="w-full"
          placeholder="1&#9;What is Vue?&#9;A&#9;A Vue.js framework&#9;B React framework&#9;C Angular framework"
        />
        <div class="mt-3 flex justify-end gap-2">
          <UButton
            size="xs"
            color="neutral"
            variant="outline"
            @click="bulkPasteText = ''; showBulkPaste = false"
          >
            Cancel
          </UButton>
          <UButton
            size="xs"
            @click="parseBulkPaste"
          >
            Parse & Add
          </UButton>
        </div>
      </div>

      <!-- File Upload Section -->
      <div class="space-y-3">
        <UInput
          type="file"
          accept=".xlsx,.xls"
          @change="(e) => handleQuizFileUpload(Array.from((e.target as HTMLInputElement).files || []))"
        />

        <div
          v-if="quizFile"
          class="text-sm text-gray-600 dark:text-gray-400"
        >
          Selected file: <span class="font-medium">{{ quizFile.name }}</span>
        </div>

        <p class="text-xs text-gray-500">
          Upload your quiz questions file. The quiz will be uploaded after saving the daily read.
        </p>
      </div>

      <!-- Add New Question Button -->
      <div
        v-if="dailyReadId"
        class="pt-2"
      >
        <UButton
          icon="i-heroicons-plus"
          size="sm"
          color="primary"
          variant="outline"
          block
          @click="addNewQuestion"
        >
          Add New Question
        </UButton>
      </div>
    </div>

    <div
      v-if="pending"
      class="flex items-center justify-center py-12 flex-1"
    >
      <UIcon
        name="i-heroicons-arrow-path"
        class="animate-spin text-4xl"
      />
    </div>

    <div
      v-else-if="!dailyReadId"
      class="flex flex-col items-center justify-center py-12 text-center flex-1"
    >
      <UIcon
        name="i-heroicons-document-text"
        class="size-16 text-gray-300 mb-4"
      />
      <p class="text-gray-500">
        Create the daily read first to add quiz questions
      </p>
    </div>

    <div
      v-else-if="quiz.length === 0"
      class="flex flex-col items-center justify-center py-12 text-center flex-1"
    >
      <UIcon
        name="i-heroicons-document-text"
        class="size-16 text-gray-300 mb-4"
      />
      <p class="text-gray-500">
        No quiz questions yet. Click "Add New Question" to start.
      </p>
    </div>

    <div
      v-else
      class="flex-1 overflow-y-auto space-y-4 min-h-0"
    >
      <div
        v-for="question in quiz"
        :key="`${question.questionSeq}-${question.id || 'new'}`"
        class="p-4 border border-gray-200 dark:border-gray-700 rounded-lg"
        :class="editingQuestionSeq === question.questionSeq ? 'border-primary-500 bg-primary-50 dark:bg-primary-950/20' : ''"
      >
        <!-- Edit Mode -->
        <div
          v-if="editingQuestionSeq === question.questionSeq"
          class="space-y-4"
        >
          <div class="flex items-start justify-between gap-2">
            <span class="text-sm font-medium text-gray-500 mt-2">Q{{ question.questionSeq }}</span>
            <div class="flex-1">
              <UTextarea
                v-model="editForm.question"
                :rows="2"
                placeholder="Enter question"
              />
            </div>
          </div>

          <div class="ml-8 space-y-3">
            <div
              v-for="(choice, index) in editForm.choices"
              :key="`choice-${index}`"
              class="flex items-start gap-2"
            >
              <div class="flex items-center gap-2 mt-2">
                <input
                  :id="`radio-${question.questionSeq}-${choice.choice}`"
                  v-model="editForm.correctAnswer"
                  type="radio"
                  :value="choice.choice"
                  class="size-4 text-primary-600"
                >
                <label
                  :for="`radio-${question.questionSeq}-${choice.choice}`"
                  class="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[20px]"
                >
                  {{ choice.choice }}
                </label>
              </div>
              <UInput
                v-model="choice.answer"
                placeholder="Enter answer"
                class="flex-1"
              />
              <UButton
                icon="i-heroicons-trash"
                size="xs"
                color="error"
                variant="ghost"
                :disabled="editForm.choices.length === 1"
                @click="removeChoice(index)"
              />
            </div>

            <UButton
              icon="i-heroicons-plus"
              size="xs"
              color="neutral"
              variant="outline"
              @click="addChoice"
            >
              Add Choice
            </UButton>
          </div>

          <div class="flex justify-end gap-2 pt-2 border-t border-gray-200 dark:border-gray-700">
            <UButton
              size="xs"
              color="neutral"
              variant="outline"
              :disabled="saving"
              @click="cancelEdit"
            >
              Cancel
            </UButton>
            <UButton
              size="xs"
              :loading="saving"
              @click="saveQuestion"
            >
              Save
            </UButton>
          </div>
        </div>

        <!-- View Mode -->
        <div
          v-else
          class="space-y-3"
        >
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1">
              <div class="font-medium mb-3">
                {{ question.questionSeq }}. {{ question.question }}
              </div>
              <div class="space-y-2 ml-4">
                <div
                  v-for="choice in question.choices"
                  :key="choice.id || choice.choice"
                  class="flex items-start gap-2"
                  :class="choice.choice === question.correctAnswer ? 'text-green-600 dark:text-green-400 font-medium' : 'text-gray-600 dark:text-gray-400'"
                >
                  <UIcon
                    :name="choice.choice === question.correctAnswer ? 'i-heroicons-check-circle' : 'i-heroicons-minus-circle'"
                    class="size-4 mt-0.5 flex-shrink-0"
                  />
                  <span class="font-medium mr-1">{{ choice.choice }}.</span>
                  <span>{{ choice.answer }}</span>
                </div>
              </div>
              <div class="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
                <span class="text-xs font-medium text-gray-500 dark:text-gray-400">Correct Answer:</span>
                <span class="text-xs font-semibold text-green-600 dark:text-green-400 ml-2">{{ question.correctAnswer }}</span>
              </div>
            </div>
            <div class="flex items-center gap-1 flex-shrink-0">
              <UButton
                icon="i-heroicons-pencil"
                size="xs"
                color="neutral"
                variant="ghost"
                @click="startEdit(question)"
              />
              <UButton
                v-if="question.id"
                icon="i-heroicons-trash"
                size="xs"
                color="error"
                variant="ghost"
                @click="deleteQuestion(question.questionSeq)"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
