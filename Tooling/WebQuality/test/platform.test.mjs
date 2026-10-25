import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { createTemplateLinter, inspectStyles } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/source.mjs';
import { inspectDocument } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/documents.mjs';
import { searchableSource, inspectApplicability, inspectScript } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/inventory.mjs';

const templates = {
    'media-semantics': [
        ['<video src="a.mp4"></video>', '<video src="a.mp4" width="640" height="360" controls></video>'],
        ['<audio loading="lazy"></audio>', '<audio loading="lazy" controls></audio>'],
        ['<iframe src="/help"></iframe>', '<iframe src="/help" title="Help"></iframe>'],
        ['<img srcset="a.webp 400w, b.webp 800w">', '<img srcset="a.webp 400w, b.webp 800w" sizes="100vw">'],
        ['<video width="640" height="360" autoplay></video>', '<video width="640" height="360" autoplay muted></video>'],
    ],
    'resource-hints': [
        ['<link rel="preload" href="a">', '<link rel="preload" as="style" href="a">'],
        ['<link rel="preload" as="font" href="a.woff2">', '<link rel="preload" as="font" href="a.woff2" crossorigin>'],
        ['<link rel="preload" as="image" importance="high">', '<link rel="preload" as="image" fetchpriority="high">'],
    ],
    'form-transport': [
        ['<form method="get"><input type="password"></form>', '<form method="POST"><input type="password"></form>'],
        ['<form><account-register-fields /></form>', '<form method="post"><account-register-fields /></form>'],
    ],
    'native-disclosure': [
        ['<details><p>Text</p><summary>More</summary></details>', '<details><summary>More</summary><p>Text</p></details>'],
        ['<details><summary><button>More</button></summary></details>', '<details><summary>More</summary><button>Action</button></details>'],
    ],
    'ime-enter': [['<input (keydown.enter)="submit()">', '<input (fdUiTextEnter)="submit()">']],
};
for (const [name, cases] of Object.entries(templates)) test(`platform template ${name} rejects invalid and accepts valid fixtures`, async () => {
    const linter = await createTemplateLinter([`template:${name}`]);
    for (const [bad, good] of cases) {
        assert.ok((await linter.lintText(bad, { filePath: 'bad.html' }))[0].messages.some(item => item.ruleId === `guidance/${name}`), bad);
        assert.deepEqual((await linter.lintText(good, { filePath: 'good.html' }))[0].messages, [], good);
    }
});

const styles = {
    loading: ['@import url("font.css");', '.text {font: inherit}'],
    'scalable-text': ['.text {font-size: 5vw}', '.text {font-size: clamp(1rem, 1rem + 2vw, 2rem)}'],
    'render-containment': ['.below {content-visibility: auto}', '.below {content-visibility: auto; contain-intrinsic-size: auto 40rem}'],
    'container-syntax': ['.card {container-type: style}', '.card {container-type: inline-size}'],
    'motion-safety': ['.card {animation-timeline: view(); animation: reveal 1s}', '.card {animation: reveal 1s; animation-timeline: view()}'],
    'scrollbar-safety': ['.scroll {transition: scrollbar-color 1s}', '.scroll {scrollbar-color: auto}'],
    'feature-fallback': ['.card {width: calc-size(auto, size + 1rem)}', '.card {width: auto; width: calc-size(auto, size + 1rem)}'],
    'form-states': ['input:auto-fill {color: blue}', 'input:autofill {color: blue}'],
};
for (const [name, [bad, good]] of Object.entries(styles)) test(`platform CSS ${name} rejects invalid and accepts valid fixtures`, async () => {
    assert.ok((await inspectStyles(bad, 'bad.scss')).some(item => item.check === `css:${name}`));
    assert.deepEqual(await inspectStyles(good, 'good.scss'), []);
});
test('platform CSS handles comments, safe feature guards and vendor pseudo-elements', async () => {
    assert.deepEqual(await inspectStyles('/* @import "ignored"; */ .card {width: auto} @supports (width: calc-size(auto, size)) {.card {width: calc-size(auto, size)}}', 'safe.scss'), []);
    assert.ok((await inspectStyles('input::-webkit-slider-thumb, input::-moz-range-thumb {color: red}', 'bad.scss')).some(item => item.check === 'css:form-states'));
});

const validDocument = '<!doctype html><html lang="en"><head><meta name="text-scale" content="scale"><meta name="color-scheme" content="light dark"><meta name="theme-color" content="#145b9c"></head><body></body></html>';
test('document parser sees scripts Angular deliberately excludes and verifies early platform metadata', () => {
    assert.deepEqual(inspectDocument(validDocument, 'index.html'), []);
    for (const [bad, message] of [
        [validDocument.replace('<!doctype html>', ''), 'doctype'],
        [validDocument.replace('lang="en"', ''), 'language'],
        [validDocument.replace('content="scale"', 'content="none"'), 'text scaling'],
        [validDocument.replace('name="color-scheme"', 'name="other"'), 'color schemes'],
        [validDocument.replace('#145b9c', 'var(--brand)'), 'concrete color'],
        [validDocument.replace('</head>', '<script src="a.js"></script></head>'), 'HTML parser'],
        [validDocument.replace('</head>', '<link rel="preload" as="font" href="a.woff2"></head>'), 'crossorigin'],
    ]) assert.ok(inspectDocument(bad, 'index.html').some(item => item.message.includes(message)), message);
    assert.deepEqual(inspectDocument(validDocument.replace('</head>', '<script type="module" src="a.js"></script></head>'), 'index.html'), []);
});

test('feature adoption invalidates absent decisions, including computed API names, but comments do not', () => {
    const reviews = [{ guide: 'translator', whenAbsent: { patterns: ['\\bTranslator\\b'], reason: 'No browser translation API is used.' } }];
    for (const text of ['Translator.create(options);', "globalThis['Translator'].create(options)", "if ('Translator' in self) {}"])
        assert.equal(inspectApplicability([{ file: 'app.ts', searchable: searchableSource(text, 'app.ts') }], reviews).length, 1);
    assert.deepEqual(inspectApplicability([{ file: 'app.ts', searchable: searchableSource('// Translator.create()\nconst name = "InstantTranslator";', 'app.ts') }], reviews), []);
});

test('every conditional guide has an active-feature fixture and ignores that fixture inside a source comment', async () => {
    const policy = JSON.parse(await readFile(new URL('../policy.json', import.meta.url), 'utf8'));
    for (const review of policy.guideReviews.filter(item => item.whenAbsent)) {
        assert.ok(review.whenAbsent.examples?.length, review.guide);
        for (const text of review.whenAbsent.examples) {
            assert.ok(inspectApplicability([{ file: 'feature.ts', searchable: searchableSource(text, 'feature.ts') }], [review]).length, review.guide);
            assert.deepEqual(inspectApplicability([{ file: 'feature.ts', searchable: searchableSource(`/* ${text} */`, 'feature.ts') }], [review]), [], review.guide);
        }
    }
});

for (const [check, bad, good] of [
    ['dom-sinks', 'element.innerHTML = value;', 'element.textContent = value;'],
    ['dom-sinks', 'renderer.setProperty(el, "innerHTML", value);', 'renderer.setProperty(el, "innerHTML", "");'],
    ['dom-sinks', 'document.write(value);', 'draft.write(value);'],
    ['dynamic-code', 'eval(value);', 'parse(value);'],
    ['dynamic-code', 'new Function(value);', 'new Map();'],
    ['dynamic-code', 'setTimeout("run()", 1);', 'setTimeout(() => run(), 1);'],
    ['message-origin', 'popup.postMessage(value, "*");', 'popup.postMessage(value, trustedOrigin);'],
    ['credential-storage', 'localStorage.setItem("authToken", value);', 'sessionStorage.setItem("authToken", value);'],
    ['credential-storage', 'this.localStorageRef?.setItem("accessToken", value);', 'this.localStorageRef?.setItem("theme", value);'],
    ['credential-storage', 'this.storage.setItem("local", "refreshToken", value);', 'this.storage.setItem("session", "authToken", value);'],
]) test(`script ${check}: ${bad}`, () => {
    assert.ok(inspectScript(bad, 'bad.ts').some(item => item.check === `script:${check}`));
    assert.deepEqual(inspectScript(good, 'good.ts'), []);
    assert.deepEqual(inspectScript(`// ${bad}\nconst example = ${JSON.stringify(bad)};`, 'comment.ts'), []);
});
