import { readFileSync } from 'node:fs'

export function getHttpsOptions(env) {
  if (env.HTTPS !== 'true') return undefined
  if (!env.HTTPS_CERT_PATH) throw new Error('HTTPS_CERT_PATH is required when HTTPS=true')
  if (!env.HTTPS_CERT_PASSWORD) throw new Error('HTTPS_CERT_PASSWORD is required when HTTPS=true')

  return {
    pfx: readFileSync(env.HTTPS_CERT_PATH),
    passphrase: env.HTTPS_CERT_PASSWORD,
  }
}
