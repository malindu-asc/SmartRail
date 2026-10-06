#!/usr/bin/env node
/**
 * SmartRail plan tool — zero dependencies.
 *
 * tasks.yaml is the single source of truth. Everything else is generated.
 *
 *   node plan.mjs build                 regenerate Gantt + PROGRESS.md + AI report
 *   node plan.mjs today                 what is on today, plus anything slipped
 *   node plan.mjs status                one-screen summary
 *   node plan.mjs start  <id>
 *   node plan.mjs done   <id> [--actual 2.5] [--verdict accepted-with-edits]
 *                             [--roles implementer,reviewer] [--saved 1.5] [--note "..."]
 *   node plan.mjs block  <id> --note "waiting on X"
 *   node plan.mjs defer  <id> --note "why"
 *   node plan.mjs note   <id> --note "..."
 *   node plan.mjs add    <day> <WS> "title" [--est 2]
 *
 * Every mutating command rewrites tasks.yaml surgically (comments preserved)
 * and then runs build, so the generated docs are never stale.
 */

import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '..');
const TASKS = path.join(HERE, 'tasks.yaml');

const OUT_GANTT = path.join(ROOT, 'docs', 'plan', 'gantt.md');
const OUT_PROGRESS = path.join(ROOT, 'PROGRESS.md');
const OUT_AI = path.join(ROOT, 'docs', '05-ai-usage', 'ai-usage-report.md');

/* ------------------------------------------------------------------ *
 * Tiny YAML reader — handles exactly the subset tasks.yaml uses.
 * ------------------------------------------------------------------ */

function tokenize(text) {
  return text.split(/\r?\n/)
    .map((raw, i) => ({ raw, no: i + 1 }))
    .filter(t => t.raw.trim() !== '' && !t.raw.trim().startsWith('#'))
    .map(t => ({ indent: t.raw.match(/^ */)[0].length, content: t.raw.trim(), no: t.no }));
}

function scalar(v) {
  if (v === '' || v === 'null' || v === '~') return null;
  if (v === 'true') return true;
  if (v === 'false') return false;
  if (/^\[.*\]$/.test(v)) {
    const inner = v.slice(1, -1).trim();
    if (!inner) return [];
    return inner.split(',').map(s => scalar(s.trim()));
  }
  if (/^-?\d+(\.\d+)?$/.test(v)) return Number(v);
  if (/^".*"$/.test(v) || /^'.*'$/.test(v)) return v.slice(1, -1);
  return v;
}

const KV = /^([A-Za-z0-9_.-]+):\s*(.*)$/;

function parseBlock(toks, indent) {
  if (!toks.length) return null;
  if (toks[0].content.startsWith('- ')) return parseList(toks, indent);
  return parseMap(toks, indent);
}

function parseList(toks, indent) {
  const arr = [];
  let i = 0;
  while (i < toks.length) {
    if (toks[i].indent !== indent || !toks[i].content.startsWith('- ')) { i++; continue; }
    const inner = toks[i].content.slice(2).trim();
    const m = inner.match(KV);
    if (!m) { arr.push(scalar(inner)); i++; continue; }
    const obj = {};
    if (m[2] !== '') obj[m[1]] = scalar(m[2]);
    i++;
    const sub = [];
    while (i < toks.length && toks[i].indent > indent) { sub.push(toks[i]); i++; }
    if (sub.length) Object.assign(obj, parseMap(sub, sub[0].indent));
    arr.push(obj);
  }
  return arr;
}

function parseMap(toks, indent) {
  const obj = {};
  let i = 0;
  while (i < toks.length) {
    if (toks[i].indent !== indent) { i++; continue; }
    const m = toks[i].content.match(KV);
    if (!m) { i++; continue; }
    const [, key, val] = m;
    if (val !== '') { obj[key] = scalar(val); i++; continue; }
    i++;
    const sub = [];
    while (i < toks.length && toks[i].indent > indent) { sub.push(toks[i]); i++; }
    obj[key] = sub.length ? parseBlock(sub, sub[0].indent) : null;
  }
  return obj;
}

function load() {
  if (!fs.existsSync(TASKS)) die(`tasks.yaml not found at ${TASKS}`);
  const toks = tokenize(fs.readFileSync(TASKS, 'utf8'));
  const doc = parseMap(toks, 0);
  if (!doc.tasks || !doc.tasks.length) die('Parsed tasks.yaml but found no tasks — check indentation (2 spaces, no tabs).');
  return doc;
}

/* ------------------------------------------------------------------ *
 * Dates
 * ------------------------------------------------------------------ */

const DAY_MS = 86400000;
const iso = d => d.toISOString().slice(0, 10);
const parseISO = s => new Date(`${s}T00:00:00Z`);

function dayToDate(meta, day) {
  return new Date(parseISO(meta.startDate).getTime() + (day - 1) * DAY_MS);
}
function todayDayNumber(meta) {
  const now = parseISO(iso(new Date()));
  return Math.floor((now - parseISO(meta.startDate)) / DAY_MS) + 1;
}
const dow = d => ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d.getUTCDay()];

/* ------------------------------------------------------------------ *
 * Helpers
 * ------------------------------------------------------------------ */

const STATUSES = ['todo', 'doing', 'done', 'blocked', 'deferred'];
const MARK = { done: 'x', doing: '>', todo: ' ', blocked: '!', deferred: '-' };

function die(msg) { console.error(`\n  ✗ ${msg}\n`); process.exit(1); }
function safe(s) { return String(s).replace(/[:,#]/g, '').replace(/\s+/g, ' ').trim(); }
function bar(pct, width = 24) {
  const filled = Math.round((pct / 100) * width);
  return '█'.repeat(filled) + '·'.repeat(width - filled);
}
function pct(a, b) { return b === 0 ? 0 : Math.round((a / b) * 100); }

function args(argv) {
  const positional = [];
  const flags = {};
  for (let i = 0; i < argv.length; i++) {
    if (argv[i].startsWith('--')) flags[argv[i].slice(2)] = argv[i + 1] ?? true, i++;
    else positional.push(argv[i]);
  }
  return { positional, flags };
}

/* ------------------------------------------------------------------ *
 * Surgical writes — keeps comments and formatting intact
 * ------------------------------------------------------------------ */

function taskBlockRange(lines, id) {
  const start = lines.findIndex(l => new RegExp(`^\\s*-\\s+id:\\s*${id}\\s*$`).test(l));
  if (start === -1) return null;
  const baseIndent = lines[start].match(/^ */)[0].length;
  let end = lines.length;
  for (let i = start + 1; i < lines.length; i++) {
    const l = lines[i];
    if (l.trim() === '' || l.trim().startsWith('#')) continue;
    const ind = l.match(/^ */)[0].length;
    if (ind <= baseIndent) { end = i; break; }
  }
  return { start, end, baseIndent };
}

function setFields(id, updates) {
  const text = fs.readFileSync(TASKS, 'utf8');
  const lines = text.split(/\r?\n/);
  const range = taskBlockRange(lines, id);
  if (!range) die(`Task "${id}" not found in tasks.yaml`);
  const fieldIndent = ' '.repeat(range.baseIndent + 2);

  // Drop any existing nested ai: block if we are rewriting it.
  if (updates.ai) {
    for (let i = range.start + 1; i < range.end; i++) {
      if (/^\s*ai:\s*$/.test(lines[i])) {
        let j = i + 1;
        while (j < range.end && lines[j].match(/^ */)[0].length > range.baseIndent + 2) j++;
        lines.splice(i, j - i);
        range.end -= (j - i);
        break;
      }
    }
  }

  for (const [key, value] of Object.entries(updates)) {
    if (key === 'ai') continue;
    const re = new RegExp(`^${fieldIndent}${key}:`);
    let found = false;
    for (let i = range.start + 1; i < range.end; i++) {
      if (re.test(lines[i])) { lines[i] = `${fieldIndent}${key}: ${fmt(value)}`; found = true; break; }
    }
    if (!found) { lines.splice(range.start + 1, 0, `${fieldIndent}${key}: ${fmt(value)}`); range.end++; }
  }

  if (updates.ai) {
    const ai = updates.ai;
    const block = [`${fieldIndent}ai:`];
    if (ai.roles) block.push(`${fieldIndent}  roles: [${ai.roles.join(', ')}]`);
    if (ai.verdict) block.push(`${fieldIndent}  verdict: ${ai.verdict}`);
    if (ai.saved != null) block.push(`${fieldIndent}  saved: ${ai.saved}`);
    if (ai.note) block.push(`${fieldIndent}  note: ${fmt(ai.note)}`);
    lines.splice(range.end, 0, ...block);
  }

  fs.writeFileSync(TASKS, lines.join('\n'), 'utf8');
}

function fmt(v) {
  if (typeof v === 'number' || typeof v === 'boolean') return String(v);
  const s = String(v);
  return /[:#]/.test(s) ? `"${s.replace(/"/g, "'")}"` : s;
}

/* ------------------------------------------------------------------ *
 * Git commit trailers → AI evidence
 * ------------------------------------------------------------------ */

function gitAiTrailers() {
  try {
    const out = execSync(
      'git log --no-merges --date=short --pretty=format:%h%x1f%ad%x1f%s%x1f%b%x1e',
      { cwd: ROOT, stdio: ['ignore', 'pipe', 'ignore'] }
    ).toString();
    return out.split('\x1e').filter(Boolean).map(rec => {
      const [hash, date, subject, body = ''] = rec.split('\x1f');
      const get = k => (body.match(new RegExp(`^${k}:\\s*(.+)$`, 'im')) || [])[1]?.trim();
      return {
        hash: hash.trim(), date, subject,
        task: get('Task'), role: get('AI-Role'),
        verdict: get('AI-Verdict'), note: get('AI-Note'),
      };
    }).filter(c => c.role || c.verdict || c.note);
  } catch { return []; }
}

/* ------------------------------------------------------------------ *
 * Generators
 * ------------------------------------------------------------------ */

function buildGantt(doc) {
  const { meta, phases, workstreams, gates, tasks } = doc;
  const tag = t => t.status === 'done' ? 'done, ' : t.status === 'doing' ? 'active, ' : t.gate ? 'crit, ' : '';

  // Rollup: one bar per phase per workstream.
  const roll = new Map();
  for (const t of tasks) {
    if (t.status === 'deferred') continue;
    const ph = phases.find(p => t.day >= p.from && t.day <= p.to);
    if (!ph) continue;
    const key = `${ph.id}|${t.ws}`;
    const cur = roll.get(key) || { ph, ws: t.ws, from: t.day, to: t.day, all: [], };
    cur.from = Math.min(cur.from, t.day);
    cur.to = Math.max(cur.to, t.day);
    cur.all.push(t);
    roll.set(key, cur);
  }

  const L = [];
  L.push('<!-- GENERATED by plan/plan.mjs — do not edit. Edit plan/tasks.yaml then: npm run build -->');
  L.push('');
  L.push('# SmartRail — Gantt');
  L.push('');
  L.push(`Generated ${iso(new Date())} · D1 = ${meta.startDate} · ${meta.totalDays} days · owner ${meta.owner}`);
  L.push('');
  L.push('## 1. Phase rollup');
  L.push('');
  L.push('```mermaid');
  L.push('gantt');
  L.push('    dateFormat YYYY-MM-DD');
  L.push('    axisFormat %d %b');
  L.push('    title SmartRail delivery — phase rollup');

  for (const ph of phases) {
    const rows = [...roll.values()].filter(r => r.ph.id === ph.id);
    if (!rows.length) continue;
    L.push('');
    L.push(`    section ${safe(ph.name)}`);
    for (const r of rows.sort((a, b) => a.from - b.from)) {
      const allDone = r.all.every(t => t.status === 'done');
      const anyDoing = r.all.some(t => t.status === 'doing');
      const st = allDone ? 'done, ' : anyDoing ? 'active, ' : '';
      const dur = r.to - r.from + 1;
      L.push(`    ${safe(workstreams[r.ws] || r.ws)} :${st}${ph.id}_${r.ws}, ${iso(dayToDate(meta, r.from))}, ${dur}d`);
    }
  }
  L.push('');
  L.push('    section Gates');
  for (const g of gates) {
    L.push(`    ${safe(g.name)} :milestone, ${g.id}, ${iso(dayToDate(meta, g.day))}, 0d`);
  }
  L.push('```');
  L.push('');

  // Detail: every task, grouped by workstream.
  L.push('## 2. Task detail by workstream');
  L.push('');
  L.push('```mermaid');
  L.push('gantt');
  L.push('    dateFormat YYYY-MM-DD');
  L.push('    axisFormat %d %b');
  L.push('    title SmartRail delivery — task detail');
  for (const [code, name] of Object.entries(workstreams)) {
    const ts = tasks.filter(t => t.ws === code && t.status !== 'deferred').sort((a, b) => a.day - b.day);
    if (!ts.length) continue;
    L.push('');
    L.push(`    section ${safe(name)}`);
    for (const t of ts) {
      L.push(`    ${safe(t.title).slice(0, 62)} :${tag(t)}${t.id}, ${iso(dayToDate(meta, t.day))}, 1d`);
    }
  }
  L.push('```');
  L.push('');

  // Day calendar table — the thing you actually read each morning.
  L.push('## 3. Day calendar');
  L.push('');
  L.push('| Day | Date | Phase | Load | Tasks | Gate |');
  L.push('|---|---|---|---|---|---|');
  for (let d = 1; d <= meta.totalDays; d++) {
    const ts = tasks.filter(t => t.day === d);
    const ph = phases.find(p => d >= p.from && d <= p.to);
    const load = ts.reduce((s, t) => s + (t.est || 0), 0);
    const g = gates.find(x => x.day === d);
    const date = dayToDate(meta, d);
    L.push(`| D${d} | ${dow(date)} ${iso(date).slice(5)} | ${ph ? ph.name : '—'} | ${load}h | ${ts.length} | ${g ? `**${g.name}**` : ''} |`);
  }
  L.push('');
  return L.join('\n');
}

function buildProgress(doc) {
  const { meta, phases, workstreams, gates, tasks, deferred = [] } = doc;
  const today = todayDayNumber(meta);
  const active = tasks.filter(t => t.status !== 'deferred');
  const done = active.filter(t => t.status === 'done');
  const estTotal = active.reduce((s, t) => s + (t.est || 0), 0);
  const estDone = done.reduce((s, t) => s + (t.est || 0), 0);
  const actual = active.reduce((s, t) => s + (t.actual || 0), 0);
  const slipped = active.filter(t => t.day < today && t.status !== 'done');
  const blocked = active.filter(t => t.status === 'blocked');
  const todays = tasks.filter(t => t.day === today);

  const L = [];
  L.push('<!-- GENERATED by plan/plan.mjs — do not edit. Edit plan/tasks.yaml then: npm run build -->');
  L.push('');
  L.push('# SmartRail — Progress');
  L.push('');
  L.push(`**Day ${today} of ${meta.totalDays}** · ${iso(new Date())} · ${Math.max(0, meta.submitDay - today)} days to submission`);
  L.push('');
  L.push(`\`${bar(pct(done.length, active.length))}\` **${pct(done.length, active.length)}%** — ${done.length}/${active.length} tasks · ${estDone}/${estTotal}h estimated · ${actual}h logged`);
  L.push('');

  L.push('## Gates');
  L.push('');
  L.push('| Gate | Day | Date | Blocking tasks | Status |');
  L.push('|---|---|---|---|---|');
  for (const g of gates) {
    const upto = active.filter(t => t.day <= g.day);
    const open = upto.filter(t => t.status !== 'done');
    const date = iso(dayToDate(meta, g.day)).slice(5);
    const st = open.length === 0 ? '✅ cleared' : g.day < today ? `🔴 MISSED (${open.length} open)` : `${open.length} open`;
    L.push(`| ${g.name} | D${g.day} | ${date} | ${open.length} | ${st} |`);
  }
  L.push('');

  if (todays.length) {
    L.push(`## Today — D${today}`);
    L.push('');
    for (const t of todays) L.push(`- [${MARK[t.status] || ' '}] \`${t.id}\` **${t.ws}** ${t.title} _(${t.est || 0}h)_`);
    L.push('');
  }

  if (slipped.length) {
    L.push(`## ⚠ Slipped — ${slipped.length} task${slipped.length > 1 ? 's' : ''}`);
    L.push('');
    L.push('| Task | Was due | WS | Status | Title |');
    L.push('|---|---|---|---|---|');
    for (const t of slipped) L.push(`| \`${t.id}\` | D${t.day} | ${t.ws} | ${t.status} | ${t.title} |`);
    L.push('');
    L.push('> Rule from plan.md §3.3: slipped work is either re-dated, cut to the deferred register, or it quietly eats the D20–D21 buffer. Pick one explicitly.');
    L.push('');
  }

  if (blocked.length) {
    L.push('## 🚧 Blocked');
    L.push('');
    for (const t of blocked) L.push(`- \`${t.id}\` ${t.title} — ${t.note || 'no reason recorded'}`);
    L.push('');
  }

  L.push('## Phases');
  L.push('');
  L.push('| Phase | Days | Progress | Done |');
  L.push('|---|---|---|---|');
  for (const ph of phases) {
    const ts = active.filter(t => t.day >= ph.from && t.day <= ph.to);
    const d = ts.filter(t => t.status === 'done').length;
    L.push(`| ${ph.name} | D${ph.from}–D${ph.to} | \`${bar(pct(d, ts.length), 16)}\` ${pct(d, ts.length)}% | ${d}/${ts.length} |`);
  }
  L.push('');

  L.push('## Workstreams');
  L.push('');
  L.push('| Workstream | Progress | Done | Est | Logged |');
  L.push('|---|---|---|---|---|');
  for (const [code, name] of Object.entries(workstreams)) {
    const ts = active.filter(t => t.ws === code);
    if (!ts.length) continue;
    const d = ts.filter(t => t.status === 'done').length;
    const e = ts.reduce((s, t) => s + (t.est || 0), 0);
    const a = ts.reduce((s, t) => s + (t.actual || 0), 0);
    L.push(`| ${name} | \`${bar(pct(d, ts.length), 16)}\` ${pct(d, ts.length)}% | ${d}/${ts.length} | ${e}h | ${a}h |`);
  }
  L.push('');

  if (deferred.length) {
    L.push('## Deferred register');
    L.push('');
    L.push('| ID | Item | Reason |');
    L.push('|---|---|---|');
    for (const x of deferred) L.push(`| ${x.id} | ${x.title} | ${x.reason} |`);
    L.push('');
  }
  return L.join('\n');
}

function buildAiReport(doc) {
  const { tasks, workstreams } = doc;
  const withAi = tasks.filter(t => t.ai);
  const commits = gitAiTrailers();

  const verdicts = {};
  const roles = {};
  let saved = 0;
  for (const t of withAi) {
    const v = t.ai.verdict || 'unrecorded';
    verdicts[v] = (verdicts[v] || 0) + 1;
    for (const r of t.ai.roles || []) roles[r] = (roles[r] || 0) + 1;
    saved += t.ai.saved || 0;
  }

  const L = [];
  L.push('<!-- GENERATED by plan/plan.mjs — do not edit. Edit plan/tasks.yaml or use commit trailers, then: npm run build -->');
  L.push('');
  L.push('# AI Usage Report');
  L.push('');
  L.push(`Generated ${iso(new Date())}. Aggregated from \`plan/tasks.yaml\` task records and from \`AI-Role\` / \`AI-Verdict\` / \`AI-Note\` git commit trailers.`);
  L.push('');
  L.push('> This report is evidence, not narrative. The reasoned write-ups live in');
  L.push('> `ai-was-wrong.md`, `prompt-library.md` and `modified-implementations.md`.');
  L.push('');

  L.push('## Summary');
  L.push('');
  L.push(`- Tasks with a recorded AI contribution: **${withAi.length}** of ${tasks.length}`);
  L.push(`- Commits carrying AI trailers: **${commits.length}**`);
  L.push(`- Net hours saved (self-reported): **${saved}h**`);
  L.push('');
  L.push('| Verdict | Count |');
  L.push('|---|---|');
  for (const [v, n] of Object.entries(verdicts).sort((a, b) => b[1] - a[1])) L.push(`| ${v} | ${n} |`);
  L.push('');
  const rejected = withAi.filter(t => t.ai.verdict === 'rejected').length
    + withAi.filter(t => t.ai.verdict === 'accepted-with-edits').length;
  L.push(`**Challenge rate: ${pct(rejected, withAi.length)}%** of AI contributions were rejected or materially edited.`);
  L.push('');

  if (Object.keys(roles).length) {
    L.push('| AI role | Times used |');
    L.push('|---|---|');
    for (const [r, n] of Object.entries(roles).sort((a, b) => b[1] - a[1])) L.push(`| ${r} | ${n} |`);
    L.push('');
  }

  L.push('## Per-task record');
  L.push('');
  L.push('| Task | WS | Roles | Verdict | Saved | What I changed |');
  L.push('|---|---|---|---|---|---|');
  for (const t of withAi) {
    L.push(`| \`${t.id}\` | ${t.ws} | ${(t.ai.roles || []).join(', ')} | ${t.ai.verdict || '—'} | ${t.ai.saved ?? '—'}h | ${t.ai.note || ''} |`);
  }
  L.push('');

  L.push('## Commit-level evidence');
  L.push('');
  if (!commits.length) {
    L.push('_No AI trailers found yet. Use the commit template (`git config commit.template .gitmessage`) so every commit records its AI contribution at the moment it happens._');
  } else {
    L.push('| Commit | Date | Task | Role | Verdict | Note |');
    L.push('|---|---|---|---|---|---|');
    for (const c of commits) {
      L.push(`| \`${c.hash}\` | ${c.date} | ${c.task || '—'} | ${c.role || '—'} | ${c.verdict || '—'} | ${c.note || c.subject} |`);
    }
  }
  L.push('');
  return L.join('\n');
}

/* ------------------------------------------------------------------ *
 * Commands
 * ------------------------------------------------------------------ */

function cmdBuild() {
  const doc = load();
  for (const f of [OUT_GANTT, OUT_PROGRESS, OUT_AI]) fs.mkdirSync(path.dirname(f), { recursive: true });
  fs.writeFileSync(OUT_GANTT, buildGantt(doc), 'utf8');
  fs.writeFileSync(OUT_PROGRESS, buildProgress(doc), 'utf8');
  fs.writeFileSync(OUT_AI, buildAiReport(doc), 'utf8');
  console.log(`  ✓ docs/plan/gantt.md`);
  console.log(`  ✓ PROGRESS.md`);
  console.log(`  ✓ docs/05-ai-usage/ai-usage-report.md`);
}

function cmdToday() {
  const doc = load();
  const today = todayDayNumber(doc.meta);
  const date = dayToDate(doc.meta, today);
  console.log(`\n  D${today}/${doc.meta.totalDays} · ${dow(date)} ${iso(date)}\n`);
  const mine = doc.tasks.filter(t => t.day === today);
  if (!mine.length) console.log('  (nothing scheduled)');
  for (const t of mine) console.log(`  [${MARK[t.status] || ' '}] ${t.id.padEnd(7)} ${String(t.ws).padEnd(6)} ${t.title}  (${t.est || 0}h)`);
  const slipped = doc.tasks.filter(t => t.day < today && !['done', 'deferred'].includes(t.status));
  if (slipped.length) {
    console.log(`\n  ⚠ ${slipped.length} slipped:`);
    for (const t of slipped) console.log(`      ${t.id.padEnd(7)} D${String(t.day).padEnd(3)} ${t.title}`);
  }
  console.log('');
}

function cmdStatus() {
  const doc = load();
  const today = todayDayNumber(doc.meta);
  const active = doc.tasks.filter(t => t.status !== 'deferred');
  const done = active.filter(t => t.status === 'done').length;
  console.log(`\n  SmartRail · D${today}/${doc.meta.totalDays} · ${pct(done, active.length)}% (${done}/${active.length})`);
  console.log(`  ${bar(pct(done, active.length), 40)}\n`);
  for (const ph of doc.phases) {
    const ts = active.filter(t => t.day >= ph.from && t.day <= ph.to);
    const d = ts.filter(t => t.status === 'done').length;
    const cur = today >= ph.from && today <= ph.to ? '◀' : ' ';
    console.log(`  ${cur} ${ph.name.padEnd(24)} D${String(ph.from).padStart(2)}-${String(ph.to).padStart(2)}  ${bar(pct(d, ts.length), 14)} ${String(pct(d, ts.length)).padStart(3)}%`);
  }
  const next = doc.gates.find(g => active.some(t => t.day <= g.day && t.status !== 'done'));
  if (next) console.log(`\n  Next gate: ${next.name} (D${next.day}, ${iso(dayToDate(doc.meta, next.day))})`);
  console.log('');
}

function mutate(id, updates) {
  setFields(id, updates);
  console.log(`  ✓ ${id} → ${Object.entries(updates).filter(([k]) => k !== 'ai').map(([k, v]) => `${k}=${v}`).join(' ')}`);
  cmdBuild();
}

function main() {
  const [cmd, ...rest] = process.argv.slice(2);
  const { positional, flags } = args(rest);
  const id = positional[0];

  switch (cmd) {
    case 'build': return cmdBuild();
    case 'today': return cmdToday();
    case 'status': case undefined: return cmdStatus();

    case 'start': if (!id) die('usage: plan start <id>'); return mutate(id, { status: 'doing' });

    case 'done': {
      if (!id) die('usage: plan done <id> [--actual H] [--verdict V] [--roles a,b] [--saved H] [--note "..."]');
      const u = { status: 'done', completed: iso(new Date()) };
      if (flags.actual) u.actual = Number(flags.actual);
      if (flags.verdict || flags.roles || flags.saved || flags['ai-note']) {
        u.ai = {
          roles: flags.roles ? String(flags.roles).split(',').map(s => s.trim()) : undefined,
          verdict: flags.verdict,
          saved: flags.saved != null ? Number(flags.saved) : undefined,
          note: flags['ai-note'] || flags.note,
        };
      } else if (flags.note) u.note = flags.note;
      return mutate(id, u);
    }

    case 'block': if (!id) die('usage: plan block <id> --note "why"'); return mutate(id, { status: 'blocked', note: flags.note || 'blocked' });
    case 'defer': if (!id) die('usage: plan defer <id> --note "why"'); return mutate(id, { status: 'deferred', note: flags.note || 'deferred' });
    case 'note': if (!id) die('usage: plan note <id> --note "..."'); return mutate(id, { note: flags.note || '' });

    case 'ai': {
      if (!id) die('usage: plan ai <id> --roles a,b --verdict V [--saved H] [--note "..."]');
      return mutate(id, {
        ai: {
          roles: flags.roles ? String(flags.roles).split(',').map(s => s.trim()) : undefined,
          verdict: flags.verdict,
          saved: flags.saved != null ? Number(flags.saved) : undefined,
          note: flags.note,
        },
      });
    }

    case 'add': {
      const [day, ws, title] = positional;
      if (!day || !ws || !title) die('usage: plan add <day> <WS> "title" [--est H]');
      const text = fs.readFileSync(TASKS, 'utf8');
      const lines = text.split(/\r?\n/);
      const anchor = lines.findIndex(l => /^deferred:/.test(l));
      const n = load().tasks.filter(t => t.day === Number(day)).length + 1;
      const newId = `D${day}-${n}`;
      const block = [
        `  - id: ${newId}`,
        `    title: ${safe(title)}`,
        `    ws: ${ws}`,
        `    day: ${day}`,
        `    est: ${flags.est ?? 2}`,
        `    status: todo`,
        '',
      ];
      lines.splice(anchor === -1 ? lines.length : anchor, 0, ...block);
      fs.writeFileSync(TASKS, lines.join('\n'), 'utf8');
      console.log(`  ✓ added ${newId}`);
      return cmdBuild();
    }

    default: die(`unknown command "${cmd}". Try: build | today | status | start | done | block | defer | note | ai | add`);
  }
}

main();
