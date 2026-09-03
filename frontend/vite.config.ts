import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const configuredPort = Number(env.FRONTEND_PORT || 5173)
  const port = Number.isInteger(configuredPort) && configuredPort > 0
    ? configuredPort
    : 5173

  return {
    plugins: [react(), tailwindcss()],
    server: {
      host: '0.0.0.0',
      port,
    },
    preview: {
      host: '0.0.0.0',
      port,
      strictPort: true,
    },
  }
})
