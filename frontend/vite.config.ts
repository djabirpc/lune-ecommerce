import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true,
    port: 5173,
    // Docker Desktop on Windows doesn't forward native filesystem change events through the
    // bind-mounted volume, so chokidar's default watcher never sees edits made on the host.
    // Polling works regardless of how the volume is mounted.
    watch: {
      usePolling: true,
      interval: 300,
    },
  },
})
