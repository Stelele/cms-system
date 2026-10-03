// Browser check: loads real routes in headless Chromium and fails on any
// uncaught page error.
//
// It exists because nothing else in this repo could see a runtime fault.
// vue-tsc and vite build both pass on a component that throws during setup, and
// the original /write crash - a temporal dead zone - was invisible to both. A
// later bug, "Maximum recursive updates exceeded in <Write>", was only ever
// going to be caught by actually running the page.
//
// It found that second bug, and it also caught the year field silently showing
// its default instead of the value loaded from the CMS.
//
// Requires a local backend and the Auth0-stubbed dev server:
//
//   dotnet run --project backend/Host/Host.csproj
//   VITE_BROWSER_CHECK_TOKEN=$(cat ~/.m2m) npm run dev -- --config vite.browser-check.config.ts
//   BLOG_ID=… POST_ID=… STD_BLOG_ID=… npm run test:browser
//
// BLOG_ID must be a project blog (kind=Project) and POST_ID one of its projects.
import { chromium } from 'playwright-core'

const base = process.env.BASE_URL ?? 'http://localhost:5173/cms-system-frontend-build'
const projectBlog = process.env.BLOG_ID
const projectPost = process.env.POST_ID
const standardBlog = process.env.STD_BLOG_ID

if (!projectBlog || !projectPost) {
  console.error('BLOG_ID and POST_ID are required: a project blog and one of its projects.')
  process.exit(2)
}

const routes = [
  ['home', '/'],
  ['blogs index', '/blogs'],
  ['write new (project blog)', `/write?blogId=${projectBlog}`, true],
  ['write edit (project)', `/write?blogId=${projectBlog}&edit=${projectPost}`, true],
  ...(standardBlog ? [['write (standard blog)', `/write?blogId=${standardBlog}`, false]] : []),
]

const executablePath =
  process.env.CHROME_PATH ??
  `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`

const browser = await chromium.launch({ executablePath, args: ['--no-sandbox', '--disable-dev-shm-usage'] })
let failures = 0

for (const [label, path, wantProjectFields] of routes) {
  const page = await browser.newPage()
  const errors = []
  page.on('pageerror', (e) => errors.push(e.message.split('\n')[0]))
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`console: ${m.text().split('\n')[0]}`) })

  await page.goto(base + path, { waitUntil: 'networkidle', timeout: 45_000 })
  await page.waitForTimeout(1_800)

  const text = (await page.locator('body').innerText()).replace(/\s+/g, ' ').trim()
  // A route that never rendered makes "no errors" meaningless - the first
  // version of this script passed against a base-URL notice page.
  if (text.length < 20 || /public base URL/.test(text)) {
    errors.push('the app never rendered, so this result proves nothing')
  }

  let fields = null
  if (wantProjectFields !== undefined) {
    fields = (await page.locator('text=Last pushed').count()) > 0
    if (fields !== wantProjectFields) {
      errors.push(`project fields ${fields ? 'shown' : 'hidden'}, expected ${wantProjectFields ? 'shown' : 'hidden'}`)
    }
    if (wantProjectFields) {
      // Editing a field is what tripped the recursive-update loop, so it has to
      // be exercised rather than merely rendered.
      const year = page.locator('input[type=number]').first()
      if (await year.count()) {
        await year.fill('2025')
        await page.waitForTimeout(700)
        const after = await year.inputValue()
        if (after !== '2025') errors.push(`year edit did not stick (got "${after}")`)
      }
    }
  }

  if (errors.length) { failures++; console.log(`FAIL  ${label}\n        ${errors.join('\n        ')}`) }
  else console.log(`PASS  ${label}`)
  await page.close()
}

await browser.close()
console.log(failures ? `\n${failures} route(s) failed` : `\nall ${routes.length} routes clean`)
process.exit(failures ? 1 : 0)
