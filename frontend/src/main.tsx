import { createRoot } from 'react-dom/client'
import './index.css'
import { BrowserRouter } from "react-router-dom";
import * as Sentry from "@sentry/react";
import App from './App.tsx'

Sentry.init({
  dsn: import.meta.env.VITE_SENTRY_DSN,
  environment: import.meta.env.MODE,
  debug: true,
  enableLogs: true,
  integrations: [
    Sentry.browserTracingIntegration(),
    Sentry.replayIntegration(),
    Sentry.consoleLoggingIntegration({ levels: ["log", "warn", "error"] }),
  ],
  tracesSampleRate: import.meta.env.PROD ? 0.2 : 1.0,
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,
  // enabled: import.meta.env.PROD,
});

createRoot(document.getElementById('root')!).render(
  <Sentry.ErrorBoundary fallback={<p>Terjadi kesalahan. Silakan reload halaman.</p>}>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </Sentry.ErrorBoundary>
)