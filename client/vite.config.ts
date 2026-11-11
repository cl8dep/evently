import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: { proxy: { '/api': 'http://localhost:5158', '/mcp': 'http://localhost:5158' } },
  build: { outDir: '../src/Evently.Api/wwwroot', emptyOutDir: true },
})
