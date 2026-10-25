// Feature check: runs the CI checks for the areas that changed, runs the Playwright flows when the stack is up, and writes a report
// (report.md and report.html) saying what passed, what failed and what was not run. Nothing here changes source code.
//
//   node scripts/feature-check/run.mjs                    checks for the changed areas (from git), plus the browser flows if the stack is up
//   node scripts/feature-check/run.mjs --areas backend,web,mobile,catering,e2e
//   node scripts/feature-check/run.mjs --name planning     a label for the report folder and title
//   node scripts/feature-check/run.mjs --report <folder>   rebuild the report from results.json (after adding screenshots to <folder>/shots)
//   add --pdf to also write report.pdf (uses the Playwright library in apps/web/e2e and the installed Chrome or Chromium)
//
// Reports go to reports/feature-check/<stamp>-<name>/ (ignored by git). Needs Node 20 or newer, no packages.

import { spawn, execSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..')
const args = process.argv.slice(2)
const flag = (name) => {
  const i = args.indexOf(`--${name}`)
  return i >= 0 ? (args[i + 1] ?? '') : undefined
}

const STEP_TIMEOUT_MS = 25 * 60 * 1000

/** What each area runs: the same commands CI runs (see CLAUDE.md). `cwd` is relative to the repository root. */
const AREAS = {
  backend: [
    { name: 'Backend build (warnings are errors)', cwd: '.', cmd: 'dotnet build SmartRail.slnx -c Release -warnaserror' },
    { name: 'Backend tests', cwd: '.', cmd: 'dotnet test SmartRail.slnx -c Release --no-build' },
  ],
  web: [
    { name: 'Console build', cwd: 'apps/web', cmd: 'npm run build --workspace=console' },
    { name: 'Terminal (kiosk) build', cwd: 'apps/web', cmd: 'npm run build --workspace=kiosk' },
    { name: 'Console tests', cwd: 'apps/web/console', cmd: 'npm test' },
    { name: 'Terminal (kiosk) tests', cwd: 'apps/web/kiosk', cmd: 'npm test' },
  ],
  catering: [{ name: 'Catering type-check', cwd: 'services/catering', cmd: 'npm run typecheck' }],
  mobile: [
    { name: 'Flutter analyze', cwd: 'apps/mobile', cmd: 'flutter analyze apps/passenger apps/driver packages/core packages/design_system packages/api_client' },
    { name: 'Driver app tests', cwd: 'apps/mobile/apps/driver', cmd: 'flutter test' },
    { name: 'Passenger app tests', cwd: 'apps/mobile/apps/passenger', cmd: 'flutter test' },
  ],
  e2e: [{ name: 'Browser flows (Playwright)', cwd: 'apps/web/e2e', cmd: 'npx playwright test --reporter=list', needs: ['http://localhost:3000', 'http://localhost:5080/api/network/stations'] }],
}

const escapeHtml = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

function changedAreas() {
  let files = []
  try {
    files = execSync('git status --porcelain', { cwd: root, encoding: 'utf8' })
      .split('\n')
      .map((l) => l.slice(3).trim().replace(/^"|"$/g, ''))
      .filter(Boolean)
  } catch {
    return Object.keys(AREAS).filter((a) => a !== 'e2e')
  }
  const hit = new Set()
  for (const f of files) {
    if (f.startsWith('src/') || f.startsWith('tests/') || f.endsWith('.slnx') || f === 'Directory.Build.props') hit.add('backend')
    if (f.startsWith('apps/web/e2e')) hit.add('e2e')
    else if (f.startsWith('apps/web')) hit.add('web')
    if (f.startsWith('apps/mobile')) hit.add('mobile')
    if (f.startsWith('services/catering')) hit.add('catering')
  }
  return [...hit]
}

async function reachable(url) {
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(4000) })
    return response.status < 500
  } catch {
    return false
  }
}

function runStep(step) {
  return new Promise((done) => {
    const started = Date.now()
    const child = spawn(step.cmd, { cwd: join(root, step.cwd), shell: true, env: { ...process.env, CI: '1' } })
    let out = ''
    const take = (chunk) => {
      out += chunk.toString()
      if (out.length > 400_000) out = out.slice(-200_000)
    }
    child.stdout.on('data', take)
    child.stderr.on('data', take)
    const timer = setTimeout(() => child.kill(), STEP_TIMEOUT_MS)
    child.on('close', (code) => {
      clearTimeout(timer)
      done({ ...step, status: code === 0 ? 'PASS' : 'FAIL', code, seconds: Math.round((Date.now() - started) / 1000), tail: out.trim().split('\n').slice(-30).join('\n'), summary: summarise(out) })
    })
  })
}

/** One line of real numbers from the output (test counts, analyzer result), when the tool prints them. */
function summarise(out) {
  const lines = out.split('\n').map((l) => l.trim())
  const picks = lines.filter((l) => /^(Passed!|Failed!)|tests? passed|Tests\s+\d|passed \(|\d+ passed|\d+ failed|All tests passed|No issues found|issues? found|error TS|Build succeeded|\d+ Error\(s\)/i.test(l))
  return [...new Set(picks)].slice(-4).join(' | ')
}

function build(results, meta) {
  const counts = { PASS: 0, FAIL: 0, 'NOT RUN': 0 }
  for (const r of results) counts[r.status]++
  const verdict = counts.FAIL > 0 ? 'FAILED' : counts['NOT RUN'] > 0 ? 'PASSED, with checks not run' : 'PASSED'
  const shotsDir = join(meta.dir, 'shots')
  const shots = existsSync(shotsDir) ? readdirSync(shotsDir).filter((f) => /\.(png|jpe?g)$/i.test(f)).sort() : []

  const md = [
    `# Feature check: ${meta.name}`,
    '',
    `Run: ${meta.startedAt} | Branch: ${meta.branch} | Result: **${verdict}**`,
    '',
    `Passed ${counts.PASS}, failed ${counts.FAIL}, not run ${counts['NOT RUN']}.`,
    '',
    '| Check | Result | Time | Detail |',
    '| --- | --- | --- | --- |',
    ...results.map((r) => `| ${r.name} | ${r.status} | ${r.seconds ?? '-'} s | ${(r.status === 'NOT RUN' ? r.reason : r.summary || (r.status === 'FAIL' ? `exit code ${r.code}` : '')).replace(/\|/g, '/')} |`),
    '',
    ...(results.some((r) => r.status === 'NOT RUN') ? ['## Not verified', '', ...results.filter((r) => r.status === 'NOT RUN').map((r) => `- ${r.name}: ${r.reason}`), ''] : []),
    ...(results.some((r) => r.status === 'FAIL') ? ['## Failures', '', ...results.filter((r) => r.status === 'FAIL').flatMap((r) => [`### ${r.name}`, '', '```', r.tail, '```', ''])] : []),
    ...(shots.length ? ['## Screenshots', '', ...shots.map((s) => `![${s}](shots/${s})`), ''] : []),
  ].join('\n')

  const rows = results
    .map((r) => `<tr class="${r.status.replace(' ', '-').toLowerCase()}"><td>${escapeHtml(r.name)}</td><td><b>${r.status}</b></td><td>${r.seconds ?? '-'} s</td><td>${escapeHtml(r.status === 'NOT RUN' ? r.reason : r.summary || (r.status === 'FAIL' ? `exit code ${r.code}` : ''))}</td></tr>`)
    .join('\n')
  const failures = results.filter((r) => r.status === 'FAIL').map((r) => `<h3>${escapeHtml(r.name)}</h3><pre>${escapeHtml(r.tail)}</pre>`).join('\n')
  const notRun = results.filter((r) => r.status === 'NOT RUN').map((r) => `<li>${escapeHtml(r.name)}: ${escapeHtml(r.reason)}</li>`).join('')
  const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Feature check: ${escapeHtml(meta.name)}</title>
<style>body{font:15px/1.5 system-ui,sans-serif;margin:32px auto;max-width:960px;color:#111}h1{margin-bottom:4px}table{border-collapse:collapse;width:100%}
td,th{border:1px solid #d6d9dc;padding:8px 10px;text-align:left;vertical-align:top}th{background:#f1f3f4}tr.pass b{color:#0a7a3d}tr.fail b{color:#b3261e}tr.not-run b{color:#8a6100}
pre{background:#f6f7f8;padding:12px;overflow:auto;font-size:12px}.v{font-size:18px;margin:8px 0 16px}img{max-width:100%;border:1px solid #d6d9dc;margin:8px 0}</style></head><body>
<h1>Feature check: ${escapeHtml(meta.name)}</h1><div>Run ${escapeHtml(meta.startedAt)} | Branch ${escapeHtml(meta.branch)}</div>
<div class="v">Result: <b>${verdict}</b> (passed ${counts.PASS}, failed ${counts.FAIL}, not run ${counts['NOT RUN']})</div>
<table><tr><th>Check</th><th>Result</th><th>Time</th><th>Detail</th></tr>${rows}</table>
${notRun ? `<h2>Not verified</h2><ul>${notRun}</ul>` : ''}${failures ? `<h2>Failures</h2>${failures}` : ''}
${shots.length ? `<h2>Screenshots</h2>${shots.map((s) => `<div><div>${escapeHtml(s)}</div><img src="shots/${encodeURIComponent(s)}" alt="${escapeHtml(s)}"></div>`).join('')}` : ''}
</body></html>`
  writeFileSync(join(meta.dir, 'report.md'), md)
  writeFileSync(join(meta.dir, 'report.html'), html)
  return verdict
}

/** Writes report.pdf from report.html. Returns a message saying what happened; never throws. */
async function makePdf(dir) {
  try {
    const entry = join(root, 'apps', 'web', 'e2e', 'node_modules', '@playwright', 'test', 'index.mjs')
    if (!existsSync(entry)) return 'PDF not made: run npm install in apps/web/e2e first'
    const { chromium } = await import(pathToFileURL(entry).href)
    let browser
    try {
      browser = await chromium.launch({ channel: 'chrome' })
    } catch {
      browser = await chromium.launch()
    }
    const page = await browser.newPage()
    await page.goto(pathToFileURL(join(dir, 'report.html')).href)
    await page.pdf({ path: join(dir, 'report.pdf'), format: 'A4', printBackground: true, margin: { top: '16mm', bottom: '16mm', left: '12mm', right: '12mm' } })
    await browser.close()
    return `PDF: ${join(dir, 'report.pdf')}`
  } catch (error) {
    return `PDF not made: ${String(error.message ?? error).split(String.fromCharCode(10))[0]}`
  }
}

// ---- entry ----
const rebuild = flag('report')
if (rebuild !== undefined) {
  const dir = resolve(rebuild)
  const saved = JSON.parse(readFileSync(join(dir, 'results.json'), 'utf8'))
  const verdict = build(saved.results, { ...saved.meta, dir })
  console.log(`Report rebuilt: ${join(dir, 'report.html')} (${verdict})`)
  if (args.includes('--pdf')) console.log(await makePdf(dir))
  process.exit(0)
}

const wanted = flag('areas') ? flag('areas').split(',').map((s) => s.trim()).filter(Boolean) : changedAreas()
const unknown = wanted.filter((a) => !AREAS[a])
if (unknown.length) {
  console.error(`Unknown area: ${unknown.join(', ')}. Choose from ${Object.keys(AREAS).join(', ')}.`)
  process.exit(2)
}
// The browser flows run when the stack answers, unless the caller named the areas.
if (!flag('areas') && !wanted.includes('e2e') && (await reachable('http://localhost:3000')) && (await reachable('http://localhost:5080/api/network/stations'))) wanted.push('e2e')

const name = (flag('name') || 'check').replace(/[^a-z0-9-]+/gi, '-').toLowerCase()
const stamp = new Date().toISOString().replace(/[-:]/g, '').replace(/\..+/, '').replace('T', '-')
const dir = join(root, 'reports', 'feature-check', `${stamp}-${name}`)
mkdirSync(join(dir, 'shots'), { recursive: true })
const branch = (() => { try { return execSync('git branch --show-current', { cwd: root, encoding: 'utf8' }).trim() } catch { return 'unknown' } })()

console.log(`Feature check "${name}": ${wanted.length ? wanted.join(', ') : 'no changed area found'}`)
const results = []
for (const area of wanted) {
  let skipReason = null
  for (const step of AREAS[area]) {
    if (skipReason) {
      results.push({ name: step.name, status: 'NOT RUN', reason: skipReason })
      continue
    }
    const down = step.needs ? (await Promise.all(step.needs.map(async (u) => ((await reachable(u)) ? null : u)))).filter(Boolean) : []
    if (down.length) {
      results.push({ name: step.name, status: 'NOT RUN', reason: `the stack is not answering at ${down.join(', ')}` })
      continue
    }
    console.log(`  running: ${step.name}`)
    const result = await runStep(step)
    console.log(`    ${result.status} (${result.seconds} s) ${result.summary}`)
    results.push(result)
    // A failed build makes the tests behind it meaningless, so say that rather than run them on stale output.
    if (result.status === 'FAIL' && /build/i.test(step.name) && area === 'backend') skipReason = `skipped because "${step.name}" failed`
  }
}
if (results.length === 0) results.push({ name: 'Any check', status: 'NOT RUN', reason: 'no changed area was found; pass --areas to choose' })

const meta = { name, branch, startedAt: new Date().toISOString().replace('T', ' ').slice(0, 19) + ' UTC', dir }
writeFileSync(join(dir, 'results.json'), JSON.stringify({ meta: { name, branch, startedAt: meta.startedAt }, results }, null, 2))
const verdict = build(results, meta)
console.log(`\nResult: ${verdict}\nReport: ${join(dir, 'report.html')}\nAdd screenshots to ${join(dir, 'shots')} then run: node scripts/feature-check/run.mjs --report "${dir}"`)
process.exit(results.some((r) => r.status === 'FAIL') ? 1 : 0)
