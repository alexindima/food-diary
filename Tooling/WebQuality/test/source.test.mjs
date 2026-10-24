import test from 'node:test';
import assert from 'node:assert/strict';
import { createTemplateLinter, inspectStyles, inlineTemplates } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/source.mjs';

test('installed Angular parser reports invalid native and FoodDiary templates while valid templates pass', async () => {
    const linter = await createTemplateLinter([
        'template:alt-text',
        'template:fd-ui-button-accessible-name',
        'template:image-priority',
        'template:deprecated-image-attributes',
    ]);
    const bad = '<img src="hero.jpg" fetchpriority="high" loading="lazy" importance="high"><fd-ui-button icon="add" />';
    const errors = (await linter.lintText(bad, { filePath: 'bad.html' }))[0].messages;
    for (const rule of ['alt-text', 'fd-ui-button-accessible-name', 'image-priority', 'deprecated-image-attributes'])
        assert.ok(
            errors.some(error => error.ruleId?.endsWith(`/${rule}`)),
            rule,
        );
    const good = '<img src="hero.jpg" alt="" fetchpriority="high"><fd-ui-button icon="add" ariaLabel="Add" />';
    assert.deepEqual((await linter.lintText(good, { filePath: 'good.html' }))[0].messages, []);
});

test('Angular static priority is checked while explicitly false and dynamic priority are not guessed', async () => {
    const linter = await createTemplateLinter(['template:image-priority']);
    for (const value of ['priority', '[priority]="true"'])
        assert.equal((await linter.lintText(`<img ${value} loading="lazy">`, { filePath: 'bad.html' }))[0].errorCount, 1);
    for (const value of ['[priority]="false"', '[priority]="isImportant()"'])
        assert.equal((await linter.lintText(`<img ${value} loading="lazy">`, { filePath: 'good.html' }))[0].errorCount, 0);
});

test('literal image attributes respect HTML casing and Angular boolean coercion', async () => {
    const linter = await createTemplateLinter(['template:image-priority', 'template:deprecated-image-attributes']);
    for (const template of [
        '<IMG SRC=x FETCHPRIORITY=HIGH LOADING=LAZY>',
        '<img priority=true loading=lazy>',
        '<img priority=False loading=lazy>',
    ]) {
        const messages = (await linter.lintText(template, { filePath: 'literal.html' }))[0].messages;
        assert.ok(
            messages.some(message => message.ruleId === 'guidance/image-priority'),
            'Mixed-case native images and true priority inputs must report lazy-loading conflicts.',
        );
    }
    for (const value of ['priority=false', '[priority]="false"', '[priority]="null"', '[priority]="isImportant()"'])
        assert.equal((await linter.lintText(`<img ${value} loading=lazy>`, { filePath: 'safe.html' }))[0].errorCount, 0);
    assert.equal((await linter.lintText('<IMG LOADING=AUTO>', { filePath: 'deprecated.html' }))[0].errorCount, 1);
});

test('CSS literal checks support case-insensitive syntax and percentage line heights', async () => {
    const findings = await inspectStyles('* {FONT-SIZE: 12PX; LINE-HEIGHT: 120%; TEXT-WRAP: BALANCE}', 'literal.scss');
    assert.deepEqual(
        findings.map(finding => finding.check),
        ['css:relative-font-size', 'css:unitless-line-height', 'css:bounded-text-wrap'],
    );
    assert.equal((await inspectStyles('.body {line-height: 1.4REM}', 'unit.scss'))[0]?.check, 'css:unitless-line-height');
});

test('every reused Angular template rule reports a representative violation with the installed compiler', async () => {
    const invalid = {
        'alt-text': '<img src="x">',
        'elements-content': '<button type="button"></button>',
        'click-events-have-key-events': '<div (click)="act()">Action</div>',
        'interactive-supports-focus': '<div (click)="act()">Action</div>',
        'valid-aria': '<span aria-hidden="invalid">Text</span>',
        'role-has-required-aria': '<div role="checkbox"></div>',
        'no-positive-tabindex': '<div tabindex="1">Text</div>',
        'label-has-associated-control': '<label>Name</label>',
        'button-has-type': '<button>Save</button>',
        'table-scope': '<table><tr><td scope="col">Value</td></tr></table>',
    };
    const linter = await createTemplateLinter(Object.keys(invalid).map(rule => `template:${rule}`));
    for (const [rule, fixture] of Object.entries(invalid)) {
        const result = (await linter.lintText(fixture, { filePath: `${rule}.html` }))[0];
        assert.ok(
            result.messages.some(message => message.ruleId === `@angular-eslint/template/${rule}`),
            rule,
        );
    }
});

test('inline Angular templates include aliased Component decorators but ignore arbitrary string properties', async () => {
    const source = `import { Component as View } from '@angular/core';\n@View({template: '<img src="x">'}) class Demo {}\nconst other = {template: '<div>unrelated</div>'};`;
    const templates = await inlineTemplates(source, 'sample.ts');
    assert.equal(templates.length, 1);
    assert.equal(templates[0].text, '<img src="x">');
    assert.equal(templates[0].line, 1);
});

test('namespace imports and literal constants are inspected; unsupported expressions and broken TS fail explicitly', async () => {
    const templates = await inlineTemplates(
        "import * as ng from '@angular/core'; const html = '<img src=\"x\">'; @ng.Component({'template': html}) class Demo {}",
        'namespace.ts',
    );
    assert.equal(templates.length, 1);
    assert.equal(templates[0].text, '<img src="x">');
    await assert.rejects(
        inlineTemplates("import { Component } from '@angular/core'; @Component({template: buildTemplate()}) class Demo {}", 'dynamic.ts'),
        /incomplete/u,
    );
    await assert.rejects(inlineTemplates('const broken = {', 'broken.ts'), /parse failed/u);
});

test('PostCSS checks inspect declarations, ignore comments and distinguish bounded wrapping', async () => {
    const bad = await inspectStyles(
        '/* font-size: 12px */ .body {font-size: 12px; line-height: 1.4rem} * {text-wrap: balance}',
        'bad.scss',
    );
    assert.deepEqual(
        bad.map(item => item.check),
        ['css:relative-font-size', 'css:unitless-line-height', 'css:bounded-text-wrap'],
    );
    assert.deepEqual(await inspectStyles('.body {font-size: var(--fd-font); line-height: 1.5} h1 {text-wrap: balance}', 'good.scss'), []);
    await assert.rejects(inspectStyles('.broken {', 'broken.scss'));
});
