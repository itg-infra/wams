import assert from 'node:assert/strict'
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import test from 'node:test'
import { getHttpsOptions } from './vite.https.mjs'

test('HTTPS disabled leaves Vite on HTTP', () => {
  assert.equal(getHttpsOptions({}), undefined)
})

test('HTTPS enabled requires certificate path', () => {
  assert.throws(
    () => getHttpsOptions({ HTTPS: 'true', HTTPS_CERT_PASSWORD: 'secret' }),
    /HTTPS_CERT_PATH is required when HTTPS=true/,
  )
})

test('HTTPS enabled requires certificate password', () => {
  assert.throws(
    () => getHttpsOptions({ HTTPS: 'true', HTTPS_CERT_PATH: 'missing.pfx' }),
    /HTTPS_CERT_PASSWORD is required when HTTPS=true/,
  )
})

test('HTTPS enabled loads PFX and passphrase', (context) => {
  const certificatePath = join(tmpdir(), `wams-${process.pid}.pfx`)
  context.after(() => rmSync(certificatePath, { force: true }))
  writeFileSync(certificatePath, 'certificate')

  const result = getHttpsOptions({
    HTTPS: 'true',
    HTTPS_CERT_PATH: certificatePath,
    HTTPS_CERT_PASSWORD: 'secret',
  })

  assert.ok(result?.pfx)
  assert.equal(result.pfx.toString(), 'certificate')
  assert.equal(result.passphrase, 'secret')
})

test('HTTPS enabled loads the only PFX from a directory', (context) => {
  const certificateDirectory = mkdtempSync(join(tmpdir(), 'wams-cert-'))
  context.after(() => rmSync(certificateDirectory, { recursive: true, force: true }))
  writeFileSync(join(certificateDirectory, 'wams.pfx'), 'certificate')

  const result = getHttpsOptions({
    HTTPS: 'true',
    HTTPS_CERT_PATH: certificateDirectory,
    HTTPS_CERT_PASSWORD: 'secret',
  })

  assert.equal(result?.pfx.toString(), 'certificate')
})

test('HTTPS enabled reports a missing certificate path clearly', () => {
  const certificatePath = join(tmpdir(), `missing-wams-${process.pid}.pfx`)

  assert.throws(
    () => getHttpsOptions({
      HTTPS: 'true',
      HTTPS_CERT_PATH: certificatePath,
      HTTPS_CERT_PASSWORD: 'secret',
    }),
    new Error(`HTTPS certificate path does not exist: ${certificatePath}`),
  )
})

test('HTTPS enabled reports a directory without PFX files clearly', (context) => {
  const certificateDirectory = mkdtempSync(join(tmpdir(), 'wams-cert-'))
  context.after(() => rmSync(certificateDirectory, { recursive: true, force: true }))

  assert.throws(
    () => getHttpsOptions({
      HTTPS: 'true',
      HTTPS_CERT_PATH: certificateDirectory,
      HTTPS_CERT_PASSWORD: 'secret',
    }),
    new Error(`HTTPS certificate directory contains no .pfx files: ${certificateDirectory}`),
  )
})

test('HTTPS enabled reports multiple PFX files clearly', (context) => {
  const certificateDirectory = mkdtempSync(join(tmpdir(), 'wams-cert-'))
  context.after(() => rmSync(certificateDirectory, { recursive: true, force: true }))
  writeFileSync(join(certificateDirectory, 'first.pfx'), 'first')
  writeFileSync(join(certificateDirectory, 'second.PFX'), 'second')

  assert.throws(
    () => getHttpsOptions({
      HTTPS: 'true',
      HTTPS_CERT_PATH: certificateDirectory,
      HTTPS_CERT_PASSWORD: 'secret',
    }),
    new Error(`HTTPS certificate directory contains 2 .pfx files: ${certificateDirectory}. Set HTTPS_CERT_PATH to the exact .pfx file to use.`),
  )
})
