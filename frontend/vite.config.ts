import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ command, mode, isPreview }) => {
  const env = loadEnv(mode, '.', 'VITE_API_PROXY_')
  const target = env.VITE_API_PROXY_TARGET
  const development = command === 'serve' && !isPreview
  if (development && !target) {
    throw new Error('VITE_API_PROXY_TARGET is required. Copy frontend/.env.example to frontend/.env.local.')
  }
  if (development && target) {
    const url = new URL(target)
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password) {
      throw new Error('VITE_API_PROXY_TARGET must be an HTTP(S) URL without credentials.')
    }
    if (env.VITE_API_PROXY_ALLOW_SELF_SIGNED === 'true' &&
        (url.protocol !== 'https:' || !['localhost', '127.0.0.1', '[::1]'].includes(url.hostname))) {
      throw new Error('Self-signed certificate bypass is allowed only for local HTTPS development.')
    }
  }

  return {
    server: {
      port: 5173,
      proxy: development ? {
        '/api': {
          target,
          changeOrigin: true,
          // Opt in only for the self-signed ASP.NET Core localhost development certificate.
          secure: env.VITE_API_PROXY_ALLOW_SELF_SIGNED !== 'true',
        },
      } : undefined,
    },
    preview: { port: 4173 },
  }
})
