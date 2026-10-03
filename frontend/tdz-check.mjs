// Guards against declaration-order bugs (temporal dead zones).
//
// A temporal dead zone throws only when the code runs, so neither vue-tsc nor
// vite build can see it - both pass on code that crashes the page on setup. This
// compiles each SFC, stubs every import, and calls setup() for real.
//
// Two details make it faithful rather than decorative:
//   * computed is lazy-but-real, invoking its getter on .value. projectFields is
//     initialised with an eager defaultProjectFields() call, and that eager call
//     is what reaches `state`.
//   * blogs is non-empty. selectedBlog's getter is
//     blogs.find(b => b.id === state.blogId.id); with an empty array find never
//     invokes the callback, so `state` is never read and the real crash stays
//     hidden. An empty stub passes on the very code this is meant to catch.
//
// Usage: node tdz-check.mjs [file.vue ...]   (no arguments checks all of src/)
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { parse, compileScript } from '@vue/compiler-sfc'
import { transform } from 'esbuild'

function allSfc(dir = 'src', acc = []) {
  for (const entry of readdirSync(dir)) {
    const p = join(dir, entry)
    if (statSync(p).isDirectory()) allSfc(p, acc)
    else if (p.endsWith('.vue')) acc.push(p)
  }
  return acc
}

// Globals the app gets from Nuxt UI auto-imports, so they never appear in an
// import statement for the scanner to find.
const AUTO_IMPORTED = {
  useToast: '() => ({ add: () => {} })',
  useRoute: '() => ({ query: {}, params: {} })',
  useRouter: '() => ({ push: () => {}, replace: () => {} })',
  navigateTo: '(to) => to',
  ref: '(v) => ({ value: v })',
  computed: '(fn) => ({ get value() { return fn() } })',
  reactive: '(v) => v',
  watch: '() => {}',
  watchEffect: '() => {}',
  onMounted: '() => {}',
  onUnmounted: '() => {}',
  computedAsync: '(fn) => ({ get value() { return fn() } })',
  useBlogStore: '() => ({ blogs: [{ id: "b1", slug: "game-dev", kind: "Project", name: "Game Dev" }] })',
  useArticleStore: '() => ({ isPublishing: () => false })',
  useProjectStore: '() => ({ isProjectBlog: () => false })',
  useSettingsStore: '() => ({})',
  useSideBarStore: '() => ({ init: () => {} })',
  _defineComponent: '(c) => c',
  defineComponent: '(c) => c',
  useImageInsert: '() => ({})',
  useImageUpload: '() => ({})',
  useAudioInsert: '() => ({})',
  useArticleSummarizer: '() => ({ summarize: async () => null })',
  associateFileWithPost: 'async () => {}',
  categoryForBlogSlug: '(s) => (s === "game-dev" ? "GameDev" : null)',
  AudioExtension: '{}',
  PROJECT_BLOGS: '[]',
}
const NOOP = '() => ({})'
const CHAINABLE = '(() => { const c = new Proxy(function(){}, { get: (t, p) => p === "then" ? undefined : c, apply: () => c, construct: () => c }); return c; })()'
AUTO_IMPORTED.z = CHAINABLE

async function check(filename) {
  const { descriptor } = parse(readFileSync(filename, 'utf8'), { filename })
  const compiled = compileScript(descriptor, { id: 'tdz' }).content

  const names = new Set()
  const lines = compiled.split('\n')
  const out = []
  for (let i = 0; i < lines.length; i++) {
    if (!/^\s*import\b/.test(lines[i])) { out.push(lines[i]); continue }
    let clause = lines[i].replace(/^\s*import\s+/, '')
    while (!/from\s*["'][^"']+["']\s*;?\s*$/.test(clause)) {
      if (++i >= lines.length) break
      clause += '\n' + lines[i]
    }
    const def = clause.match(/^(\w+)\s*(,|from)/)
    if (def) names.add(def[1])
    const ns = clause.match(/\*\s+as\s+(\w+)/)
    if (ns) names.add(ns[1])
    const braces = clause.match(/\{([\s\S]*?)\}/)
    if (braces) for (const part of braces[1].split(',')) {
      const t = part.trim().replace(/^type\s+/, '').split(/\s+as\s+/).pop().trim()
      if (t && /^[A-Za-z_$][\w$]*$/.test(t)) names.add(t)
    }
  }

  const all = new Set([...names, ...Object.keys(AUTO_IMPORTED)])
  const prelude = [...all].map((n) => `const ${n} = ${AUTO_IMPORTED[n] ?? NOOP};`).join('\n')
  const { code } = await transform(out.join('\n'), { loader: 'ts', target: 'esnext' })
  const mod = await import('data:text/javascript;base64,' + Buffer.from(prelude + code).toString('base64'))

  // Props a component might read during setup; a blank object would make a
  // legitimate `props.modelValue.links` throw for the wrong reason.
  const props = {
    modelValue: { category: 'GameDev', year: 2024, stack: [], lastPushedAt: null, links: [], status: 'Active' },
    blogCategory: null, blogSlug: 'game-dev', mode: 'create', initialData: null,
  }
  try {
    mod.default.setup(props, { expose: () => {}, emit: () => {}, slots: {}, attrs: {} })
    return null
  } catch (e) {
    return e
  }
}

const targets = process.argv.slice(2).length ? process.argv.slice(2) : allSfc()
let failed = 0
for (const f of targets) {
  const e = await check(f)
  if (e) { failed++; console.log(`FAIL  ${f}  ${e.constructor.name}: ${e.message}`) }
  else console.log(`PASS  ${f}`)
}
console.log(failed ? `\n${failed} of ${targets.length} failed` : `\nall ${targets.length} components ran setup() clean`)
process.exit(failed ? 1 : 0)
