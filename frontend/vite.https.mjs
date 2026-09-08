import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'

export function getHttpsOptions(env) {
  if (env.HTTPS !== 'true') return undefined
  if (!env.HTTPS_CERT_PATH) throw new Error('HTTPS_CERT_PATH is required when HTTPS=true')
  if (!env.HTTPS_CERT_PASSWORD) throw new Error('HTTPS_CERT_PASSWORD is required when HTTPS=true')

  const certificatePath = resolveCertificatePath(env.HTTPS_CERT_PATH)

  return {
    pfx: readFileSync(certificatePath),
    passphrase: env.HTTPS_CERT_PASSWORD,
  }
}

function resolveCertificatePath(path) {
  if (!existsSync(path)) throw new Error(`HTTPS certificate path does not exist: ${path}`)
  if (!statSync(path).isDirectory()) return path

  const certificates = readdirSync(path, { withFileTypes: true })
    .filter(entry => entry.isFile() && entry.name.toLowerCase().endsWith('.pfx'))

  if (certificates.length === 0)
    throw new Error(`HTTPS certificate directory contains no .pfx files: ${path}`)
  if (certificates.length > 1)
    throw new Error(`HTTPS certificate directory contains ${certificates.length} .pfx files: ${path}. Set HTTPS_CERT_PATH to the exact .pfx file to use.`)

  return join(path, certificates[0].name)
}
