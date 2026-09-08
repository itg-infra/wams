import assert from 'node:assert/strict'
import { rmSync, writeFileSync } from 'node:fs'
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
