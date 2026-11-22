import assert from 'node:assert/strict';
import test from 'node:test';

import { parseAstAsync } from 'vitest/node';

import { createAngularMetadataCoverageFilter } from './angular-metadata-coverage.mjs';

const ngDevModeGuard = '(typeof ngDevMode === "undefined" || ngDevMode)';

function unwrap(node) {
    return node.type === 'ParenthesizedExpression' ? node.expression : node;
}

async function parseAsyncMetadata({
    loader = '() => [import("./content").then(m => m.Content), import("./footer").then(m => m.Footer)]',
    additionalOptions = '',
    changeDetection = 'ChangeDetectionStrategy.OnPush',
    innerOwner = 'Owner',
    declarationOwner = 'Owner',
    guard = ngDevModeGuard,
} = {}) {
    const ast = await parseAstAsync(`
        var Owner = class {
            static ɵcmp = compiler.ɵɵdefineComponent({ type: ${declarationOwner}, selectors: [["fd-example"]] });
            authored = () => [];
        };
        (() => {
            ${guard} && compiler.ɵsetClassMetadataAsync(Owner, ${loader}, (Content, Footer) => {
                compiler.ɵsetClassMetadata(${innerOwner}, [{ type: Component, args: [{
                    selector: "fd-example", imports: [External, Content, Footer],
                    changeDetection: ${changeDetection}, template: "<fd-content />", styles: [""], ${additionalOptions}
                }] }], null, null);
            });
        })();
    `);
    const component = ast.body[0].declarations[0];
    const outer = unwrap(ast.body[1].expression.callee).body.body[0];
    const [, loading, callback] = outer.expression.right.arguments;
    return { component, outer, loading, callback, inner: callback.body.body[0], authored: component.init.body.body[1].value };
}

test('counts only authored statements outside static async metadata registration', async () => {
    for (const options of [
        {},
        { loader: '() => [__vite_ssr_dynamic_import__("./content").then(m => m.Content), import("./footer").then(m => m.Footer)]' },
    ]) {
        const { component, outer, loading, callback, inner, authored } = await parseAsyncMetadata(options);
        const ignore = createAngularMetadataCoverageFilter();
        assert.notEqual(ignore(component, 'statement'), true);
        assert.equal(ignore(outer, 'statement'), true);
        assert.equal(ignore(loading, 'statement'), true);
        assert.equal(ignore(inner, 'statement'), true);
        for (const node of [outer, loading, inner, callback, authored]) {
            assert.notEqual(ignore(node, 'function'), true);
            assert.notEqual(ignore(node, 'branch'), true);
        }
        assert.notEqual(ignore(authored, 'statement'), true);
        assert.notEqual(ignore(loading.body.elements[0].arguments[0], 'statement'), true);
    }
});

test('preserves async metadata when an owner, dependency, or static decorator cannot be proven', async () => {
    for (const options of [
        { guard: 'authoredGuard' },
        { innerOwner: 'Different' },
        { declarationOwner: 'Different' },
        { loader: '() => [import("./content").then(m => m.Content), import("./footer").then(m => m.Different)]' },
        { loader: '() => [import("./content").then(selectContent), import("./footer").then(m => m.Footer)]' },
        { loader: '() => [import(resolveContent()).then(m => m.Content), import("./footer").then(m => m.Footer)]' },
        { loader: '() => [import("./content").then(m => m["Content"]), import("./footer").then(m => m.Footer)]' },
        { additionalOptions: 'providers: [createProvider()]' },
        { additionalOptions: '...makeOptions()' },
        { changeDetection: 'chooseDetection()' },
    ]) {
        const { component, outer, loading, inner } = await parseAsyncMetadata(options);
        const ignore = createAngularMetadataCoverageFilter();
        ignore(component, 'statement');
        assert.notEqual(ignore(outer, 'statement'), true);
        assert.notEqual(ignore(loading, 'statement'), true);
        assert.notEqual(ignore(inner, 'statement'), true);
    }
});

async function parseFixture({ initializer, token = 'Token', owner = 'Owner', guard = ngDevModeGuard, deferred = false, angular = true }) {
    const metadata = `compiler.ɵsetClassMetadata(${owner}, [], () => [], {
        query: [{ type: compiler.ViewChild, args: [compiler.forwardRef(() => ${token}), { isSignal: true }] }]
    })`;
    const invocation = deferred ? `compiler.ɵsetClassMetadataAsync(${owner}, () => [], (Token) => { ${metadata}; })` : metadata;
    const ast = await parseAstAsync(`
        var Owner = class {
            query = ${initializer};
            options = () => [];
            ${angular ? 'static ɵcmp = compiler.ɵɵdefineComponent({ type: Owner });' : ''}
        };
        (() => { ${guard} && ${invocation}; })();
    `);
    const component = ast.body[0].declarations[0];
    const expression = unwrap(ast.body[1].expression.callee).body.body[0];
    const call = expression.expression.right;
    const metadataCall = deferred ? call.arguments[2].body.body[0].expression : call;
    const ctor = metadataCall.arguments[2];
    const decorator = metadataCall.arguments[3].properties[0].value.elements[0];
    const queryThunk = decorator.properties[1].value.elements[0].arguments[0];
    return { component, expression, call, ctor, queryThunk };
}

function assertGenerated(ignore, node) {
    assert.equal(ignore(node, 'function'), true);
    assert.equal(ignore(node, 'statement'), true);
    assert.notEqual(ignore(node, 'branch'), true);
}

test('omits only generated metadata for a bare signal query reference', async () => {
    const { component, expression, ctor, queryThunk } = await parseFixture({
        initializer: 'author.viewChild.required((0, tokens.Token))',
        token: 'tokens.Token',
    });
    const ignore = createAngularMetadataCoverageFilter();
    assert.notEqual(ignore(component, 'statement'), true);
    assert.notEqual(ignore(expression, 'statement'), true);
    assertGenerated(ignore, ctor);
    assertGenerated(ignore, queryThunk);
    const authoredEmptyArrow = component.init.body.body[1].value;
    assert.notEqual(ignore(authoredEmptyArrow, 'function'), true);
    assert.notEqual(ignore(authoredEmptyArrow, 'statement'), true);
});

test('preserves an authored forwardRef copied into class metadata', async () => {
    const { component, expression, ctor, queryThunk } = await parseFixture({
        initializer: '(0, compiler.viewChild)(compiler.forwardRef(() => Token))',
    });
    const ignore = createAngularMetadataCoverageFilter();
    ignore(component, 'statement');
    ignore(expression, 'statement');
    assertGenerated(ignore, ctor);
    const authored = component.init.body.body[0].value.arguments[0].arguments[0];
    for (const arrow of [authored, queryThunk]) {
        assert.notEqual(ignore(arrow, 'function'), true);
        assert.notEqual(ignore(arrow, 'statement'), true);
    }
});

test('keeps metadata query thunks without a matching owner and token', async () => {
    for (const fixture of [{ owner: 'AnotherOwner' }, { token: 'AnotherToken' }, { guard: 'enabled' }, { angular: false }]) {
        const { component, expression, queryThunk } = await parseFixture({ initializer: 'author.viewChild(Token)', ...fixture });
        const ignore = createAngularMetadataCoverageFilter();
        ignore(component, 'statement');
        ignore(expression, 'statement');
        assert.notEqual(ignore(queryThunk, 'function'), true);
        assert.notEqual(ignore(queryThunk, 'statement'), true);
    }
});

test('keeps empty callbacks in a lookalike metadata call outside of a compiled Angular class', async () => {
    const { component, expression, ctor } = await parseFixture({ initializer: 'author.viewChild(Token)', angular: false });
    const ignore = createAngularMetadataCoverageFilter();
    ignore(component, 'statement');
    ignore(expression, 'statement');
    assert.notEqual(ignore(ctor, 'function'), true);
    assert.notEqual(ignore(ctor, 'statement'), true);
});

test('omits the nested constructor metadata while keeping deferred metadata setup', async () => {
    const { component, expression, call, ctor } = await parseFixture({ initializer: 'author.viewChild(Token)', deferred: true });
    const ignore = createAngularMetadataCoverageFilter();
    ignore(component, 'statement');
    ignore(expression, 'statement');
    assertGenerated(ignore, ctor);
    assert.notEqual(ignore(call.arguments[1], 'function'), true);
    assert.notEqual(ignore(call.arguments[2], 'function'), true);
});

test('omits the compiler factory fallback and template creation gate while retaining authored code', async () => {
    const {
        body: [declaration],
    } = await parseAstAsync(`
        var Owner = class _Owner {
            static ɵfac = function Owner_Factory(__ngFactoryType__) {
                return new (__ngFactoryType__ || _Owner)();
            };
            static ɵcmp = compiler.ɵɵdefineComponent({
                type: _Owner,
                template: function Owner_Template(rf, ctx) {
                    if (rf & 1) {
                        if (ctx.enabled) ctx.render();
                    }
                    if (rf & 2) ctx.update();
                }
            });
            render(rf) {
                if (rf & 1) this.renderItem();
                return new (rf || _Owner)();
            }
        };
    `);
    const component = declaration.declarations[0];
    const [factory, definition, authored] = component.init.body.body;
    const [factoryReturn] = factory.value.body.body;
    const target = unwrap(factoryReturn.argument.callee);
    const template = definition.value.arguments[0].properties[1].value;
    const [creation, update] = template.body.body;
    const ignore = createAngularMetadataCoverageFilter();
    assert.notEqual(ignore(component, 'statement'), true);
    assert.notEqual(ignore(factoryReturn, 'statement'), true);
    assert.equal(ignore(target, 'branch'), true);
    assert.equal(ignore(creation, 'branch'), true);
    for (const branch of [creation.consequent.body[0], update, authored.value.body.body[0]]) {
        assert.notEqual(ignore(branch, 'branch'), true);
    }
    assert.notEqual(ignore(creation, 'statement'), true);
    assert.notEqual(ignore(authored.value.body.body[1], 'statement'), true);
});

test('omits the generated factory return only for a directive without a selector', async () => {
    for (const metadata of ['', ', selectors: [["", "owner", ""]]', ', ...options', ', [selectorKey]: selectorValue']) {
        const {
            body: [declaration],
        } = await parseAstAsync(`
            var Owner = class _Owner {
                static ɵfac = function Owner_Factory(__ngFactoryType__) {
                    return new (__ngFactoryType__ || _Owner)();
                };
                static ɵdir = compiler.ɵɵdefineDirective({ type: _Owner${metadata} });
            };
        `);
        const component = declaration.declarations[0];
        const [factory] = component.init.body.body;
        const [factoryReturn] = factory.value.body.body;
        const ignore = createAngularMetadataCoverageFilter();
        assert.notEqual(ignore(component, 'statement'), true);
        assert.equal(ignore(factoryReturn, 'statement') === true, metadata === '');
        assert.equal(ignore(unwrap(factoryReturn.argument.callee), 'branch'), true);
    }
});

test('preserves factory and template branches when the Angular definition names a different class', async () => {
    const {
        body: [declaration],
    } = await parseAstAsync(`
        var Owner = class _Owner {
            static ɵfac = function Owner_Factory(__ngFactoryType__) {
                return new (__ngFactoryType__ || _Owner)();
            };
            static ɵcmp = compiler.ɵɵdefineComponent({
                type: Foreign,
                template: function Owner_Template(rf, ctx) {
                    if (rf & 1) { ctx.render(); }
                }
            });
        };
    `);
    const component = declaration.declarations[0];
    const [factory, definition] = component.init.body.body;
    const [factoryReturn] = factory.value.body.body;
    const target = unwrap(factoryReturn.argument.callee);
    const creation = definition.value.arguments[0].properties[1].value.body.body[0];
    const ignore = createAngularMetadataCoverageFilter();
    ignore(component, 'statement');
    assert.notEqual(ignore(factoryReturn, 'statement'), true);
    assert.notEqual(ignore(target, 'branch'), true);
    assert.notEqual(ignore(creation, 'branch'), true);
});
