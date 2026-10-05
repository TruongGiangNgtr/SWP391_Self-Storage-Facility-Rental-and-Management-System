import assert from 'node:assert/strict'
import { execFileSync } from 'node:child_process'
import { readFileSync, readdirSync } from 'node:fs'
import { stripTypeScriptTypes, createRequire } from 'node:module'
import { createServer as createHttpServer } from 'node:http'
import { join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'
import test from 'node:test'

const root = fileURLToPath(new URL('../../', import.meta.url))
const frontend = join(root, 'frontend')
const require = createRequire(join(frontend, 'package.json'))
const { resolveConfig, createServer } = await import(pathToFileURL(require.resolve('vite')).href)

async function withProxyEnvironment(target, allowSelfSigned, run) {
  const previous = [process.env.VITE_API_PROXY_TARGET, process.env.VITE_API_PROXY_ALLOW_SELF_SIGNED]
  process.env.VITE_API_PROXY_TARGET = target
  process.env.VITE_API_PROXY_ALLOW_SELF_SIGNED = allowSelfSigned
  const previousDirectory = process.cwd()
  process.chdir(frontend)
  try { await run() }
  finally {
    process.chdir(previousDirectory)
    for (const [index, name] of ['VITE_API_PROXY_TARGET', 'VITE_API_PROXY_ALLOW_SELF_SIGNED'].entries()) {
      if (previous[index] === undefined) delete process.env[name]
      else process.env[name] = previous[index]
    }
  }
}

test('REG_CONFIG_001_API_client_calls_relative_api_v1', async () => {
  const source = readFileSync(join(frontend, 'src/api/apiClient.ts'), 'utf8')
  const javascript = stripTypeScriptTypes(source, { mode: 'transform' })
  const { apiRequest } = await import(`data:text/javascript;base64,${Buffer.from(javascript).toString('base64')}`)
  const originalFetch = globalThis.fetch
  let requestedUrl
  globalThis.fetch = async url => {
    requestedUrl = url
    return Response.json({ data: { userAccountId: 'fixture' } })
  }
  try {
    const response = await apiRequest('/auth/me')
    assert.equal(requestedUrl, '/api/v1/auth/me')
    assert.equal(response.data.userAccountId, 'fixture')
  } finally { globalThis.fetch = originalFetch }
})

test('REG_CONFIG_002_proxy_uses_environment_target_and_verifies_TLS_by_default', async () => {
  await withProxyEnvironment('https://localhost:7235', 'false', async () => {
    const config = await resolveConfig({}, 'serve')
    assert.equal(config.server.proxy['/api'].target, 'https://localhost:7235')
    assert.equal(config.server.proxy['/api'].changeOrigin, true)
    assert.equal(config.server.proxy['/api'].secure, true)
    assert.equal(config.server.proxy['/api'].rewrite, undefined)
  })
})

test('REG_CONFIG_003_self_signed_bypass_requires_local_HTTPS_opt_in', async () => {
  await withProxyEnvironment('https://localhost:7235', 'true', async () => {
    const config = await resolveConfig({}, 'serve')
    assert.equal(config.server.proxy['/api'].secure, false)
  })
  await withProxyEnvironment('https://example.invalid', 'true', async () => {
    await assert.rejects(resolveConfig({ logLevel: 'silent' }, 'serve'), /only for local HTTPS/)
  })
})

test('REG_CONFIG_004_production_build_has_no_development_proxy', async () => {
  await withProxyEnvironment('', 'false', async () => {
    const config = await resolveConfig({}, 'build')
    assert.equal(config.server.proxy, undefined)
  })
})

test('REG_CONFIG_006_Vite_forwards_api_path_and_changes_origin', async () => {
  let received
  const backend = createHttpServer((request, response) => {
    received = { path: request.url, host: request.headers.host }
    response.setHeader('Content-Type', 'application/json')
    response.end(JSON.stringify({ data: 'proxy-fixture' }))
  })
  await new Promise(resolve => backend.listen(0, '127.0.0.1', resolve))
  const target = `http://127.0.0.1:${backend.address().port}`
  try {
    await withProxyEnvironment(target, 'false', async () => {
      const vite = await createServer({ server: { host: '127.0.0.1', port: 0 }, logLevel: 'silent' })
      try {
        await vite.listen()
        const response = await fetch(`http://127.0.0.1:${vite.httpServer.address().port}/api/v1/auth/me?probe=1`)
        assert.equal(response.status, 200)
        assert.deepEqual(await response.json(), { data: 'proxy-fixture' })
        assert.deepEqual(received, { path: '/api/v1/auth/me?probe=1', host: new URL(target).host })
      } finally { await vite.close() }
    })
  } finally { await new Promise(resolve => backend.close(resolve)) }
})

test('REG_CONFIG_005_frontend_bundle_and_tracked_configuration_exclude_secrets', () => {
  const bundleFiles = readdirSync(join(frontend, 'dist/assets')).filter(name => /\.(js|css|map)$/.test(name))
  assert.ok(bundleFiles.some(name => name.endsWith('.js')), 'Build the frontend before checking its bundle.')
  const sensitive = /ConnectionStrings(?:__|:)|(?:Server|Data Source)=[^;\r\n]+;(?:Database|Initial Catalog)=|(?:Trusted_Connection|Integrated Security|TrustServerCertificate)\s*=\s*(?:True|False)|(?:Password|Pwd)=|Jwt__SigningKey|FRMS_CONNECTION_STRING|FRMS_SECRET_SENTINEL|https?:\/\/localhost:(?:5164|7235)/i
  for (const name of bundleFiles) {
    assert.equal(sensitive.test(readFileSync(join(frontend, 'dist/assets', name), 'utf8')), false, `Sensitive configuration in bundle file ${name}`)
  }
  const tracked = execFileSync('git', ['ls-files', '-z'], { cwd: root, encoding: 'utf8' }).split('\0').filter(Boolean)
  for (const name of new Set([...tracked, 'backend/Frms.Api/appsettings.Example.json', 'frontend/.env.example'])) {
    assert.equal(/(?:^|\/)\.env(?:\..*)?$/.test(name) && !name.endsWith('/.env.example'), false, `Local environment file is tracked: ${name}`)
    assert.equal(/(?:\.mdf|\.ldf|\.bak)$|appsettings\..*\.local\.json$/i.test(name), false, `Local data/configuration is tracked: ${name}`)
    if (/^backend\/Frms\.Api\/appsettings.*\.json$/.test(name)) {
      const configuration = JSON.parse(readFileSync(join(root, name), 'utf8'))
      assert.equal(configuration.Jwt?.SigningKey, undefined, `JWT key is tracked in ${name}`)
      for (const connection of Object.values(configuration.ConnectionStrings ?? {})) {
        assert.ok(connection.includes('<SERVER>') && connection.includes('<DATABASE>'), `Non-placeholder connection in ${name}`)
        assert.equal(/(?:Password|Pwd|User ID|UID)=/i.test(connection), false, `Credentials in ${name}`)
      }
    }
  }
})
