<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-100 px-4">
    <div class="bg-white shadow-lg rounded-xl p-8 w-full max-w-md">
      <h1 class="text-2xl font-semibold text-center mb-6 text-gray-800">
        Async Download Panel
      </h1>

      <input
        v-model="url"
        type="text"
        placeholder="Enter file URL..."
        class="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 mb-4"
      />

      <button
        @click="enqueueDownload"
        class="w-full bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 transition"
      >
        Send to Queue
      </button>

      <p v-if="status" class="mt-4 text-sm text-center font-medium text-green-700">
        {{ status }}
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const url = ref('')
const status = ref('')

const enqueueDownload = async () => {
  if (!url.value.trim()) {
    status.value = 'URL is required.'
    return
  }

  try {
    const response = await axios.post('http://localhost:5119/api/download/enqueue', url.value, {
      headers: {
        'Content-Type': 'application/json'
      }
    })

    status.value = response.data
    url.value = ''
  } catch (error) {
    status.value = 'Failed to enqueue URL.'
    console.error(error)
  }
}
</script>

<style scoped>
body {
  font-family: sans-serif;
}
</style>
