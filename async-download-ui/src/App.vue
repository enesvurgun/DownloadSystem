<template>
  <div class="page">
    <div class="panel">
      <h1 class="title">File Download</h1>

      <input
        v-model="url"
        type="text"
        placeholder="Enter file URL..."
        class="input"
      />

      <button @click="enqueueDownload" class="button">
        Download
      </button>

      <p v-if="status" class="status">
        {{ status }}
      </p>

      <div v-if="progress > 0" class="progress-wrapper">
        <div class="progress-track">
          <div class="progress-fill" :style="{ width: progress + '%' }"></div>
        </div>
        <div class="progress-text">{{ progress }}%</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const url = ref('')
const status = ref('')
const progress = ref(0)
const filename = ref('')

const enqueueDownload = async () => {
  if (!url.value.trim()) {
    status.value = 'URL is required.'
    return
  }

  const lastPart = url.value.trim().split('/').pop().split('?')[0]

  if (!lastPart.includes('.') || lastPart.length < 3) {
    status.value = 'URL must include a valid filename with extension.'
    return
  }

  filename.value = lastPart

  try {
    const response = await axios.post('http://localhost:5119/api/download/enqueue', url.value, {
      headers: { 'Content-Type': 'application/json' }
    })

    status.value = response.data
    url.value = ''
    pollDownloadProgress()
  } catch (error) {
    status.value = 'Failed to enqueue URL.'
  }
}

const pollDownloadProgress = () => {
  const interval = setInterval(async () => {
    if (!filename.value) return

    try {
      const res = await axios.get(`http://localhost:5119/api/download/status?filename=${filename.value}`)
      const text = res.data

      if (text.includes('downloading')) {
        const percent = parseInt(text.match(/\d+/)[0])
        progress.value = percent
      } else if (text === 'completed') {
        progress.value = 100
        clearInterval(interval)
        status.value = 'Download complete.'
      } else if (text === 'failed') {
        status.value = 'Download failed.'
        clearInterval(interval)
      }
    } catch (err) {
      status.value = 'Error checking progress.'
      clearInterval(interval)
    }
  }, 1000)
}
</script>

<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}
html, body {
  height: 100%;
  width: 100%;
  overflow-x: hidden;
}

.page {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
}

.panel {
  background-color: rgb(230, 219, 219);
  padding: 2rem;
  border-radius: 12px;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.08);
  max-width: 400px;
  width: 100%;
}

.title {
  font-size: 24px;
  font-weight: bold;
  color: #1f2937;
  text-align: center;
  margin-bottom: 1.5rem;
}

.input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 1rem;
  margin-bottom: 1rem;
  outline: none;
}

.input:focus {
  border-color: #d18227;
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.3);
}

.button {
  width: 100%;
  padding: 0.75rem;
  background-color: #eb7e25;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.button:hover {
  background-color: #c25410;
}

.status {
  margin-top: 1rem;
  text-align: center;
  color: #df2424;
  font-weight: 500;
  font-size: 0.95rem;
}

.progress-wrapper {
  margin-top: 2rem;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.progress-track {
  width: 100%;
  height: 16px;
  background-color: #e0e7ff;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: inset 0 1px 2px rgba(0,0,0,0.1);
}

.progress-fill {
  height: 100%;
  background: linear-gradient(to right, #6366f1, #ec4899);
  transition: width 0.4s ease-in-out;
  border-radius: 10px;
}

.progress-text {
  margin-top: 0.5rem;
  font-size: 0.85rem;
  color: #374151;
  font-weight: 500;
}
</style>
