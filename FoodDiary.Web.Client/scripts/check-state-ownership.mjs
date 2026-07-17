import { readdirSync, readFileSync } from 'node:fs';
import { extname, join, relative } from 'node:path';
import { pathToFileURL } from 'node:url';

import ts from 'typescript';

const SOURCE_ROOTS = ['src/app', 'projects/fooddiary-admin/src/app'];
const FEATURE_FACADE_PATH = /(?:^|\/)(?:src\/app\/(?:components\/shared|features)|projects\/fooddiary-admin\/src\/app\/features)\/.+\.facade\.ts$/u;

export function findStateOwnershipViolations(files) {
    const sources = files.map(file => ({
        ...file,
        source: ts.createSourceFile(file.path, file.content, ts.ScriptTarget.Latest, true),
    }));
    const providerNames = collectExplicitProviderNames(sources);
    const violations = [];

    for (const { path, source } of sources) {
        const normalizedPath = path.replaceAll('\\', '/');
        if (!FEATURE_FACADE_PATH.test(normalizedPath)) {
            continue;
        }

        for (const declaration of source.statements.filter(ts.isClassDeclaration)) {
            const className = declaration.name?.text;
            if (className === undefined || !className.endsWith('Facade') || !classUsesWritableSignal(declaration)) {
                continue;
            }

            const decorators = getDecoratorNames(declaration);
            if (!decorators.has('Injectable')) {
                violations.push(`${normalizedPath}: stateful feature facade ${className} must use @Injectable(), not a root @Service().`);
            }
            if (!providerNames.has(className)) {
                violations.push(
                    `${normalizedPath}: stateful feature facade ${className} needs an explicit route/page/dialog/component provider.`,
                );
            }
        }
    }

    return violations;
}

function collectExplicitProviderNames(sources) {
    const names = new Set();
    for (const { path, source } of sources) {
        if (path.endsWith('.spec.ts')) {
            continue;
        }
        visit(source, node => {
            if (!ts.isPropertyAssignment(node) || node.name.getText(source) !== 'providers' || !ts.isArrayLiteralExpression(node.initializer)) {
                return;
            }
            for (const provider of node.initializer.elements) {
                if (ts.isIdentifier(provider)) {
                    names.add(provider.text);
                } else if (ts.isObjectLiteralExpression(provider)) {
                    const provideProperty = provider.properties.find(
                        property => ts.isPropertyAssignment(property) && property.name.getText(source) === 'provide',
                    );
                    if (provideProperty !== undefined && ts.isPropertyAssignment(provideProperty) && ts.isIdentifier(provideProperty.initializer)) {
                        names.add(provideProperty.initializer.text);
                    }
                }
            }
        });
    }
    return names;
}

function classUsesWritableSignal(declaration) {
    let found = false;
    visit(declaration, node => {
        if (ts.isCallExpression(node) && ts.isIdentifier(node.expression) && node.expression.text === 'signal') {
            found = true;
        }
    });
    return found;
}

function getDecoratorNames(declaration) {
    const names = new Set();
    for (const modifier of declaration.modifiers ?? []) {
        if (!ts.isDecorator(modifier)) {
            continue;
        }
        const expression = ts.isCallExpression(modifier.expression) ? modifier.expression.expression : modifier.expression;
        if (ts.isIdentifier(expression)) {
            names.add(expression.text);
        }
    }
    return names;
}

function visit(root, callback) {
    walk(root);
    function walk(node) {
        callback(node);
        ts.forEachChild(node, walk);
    }
}

function loadWorkspaceFiles() {
    return SOURCE_ROOTS.flatMap(root =>
        [...walk(root)]
            .filter(file => extname(file) === '.ts')
            .map(file => ({ path: displayPath(file), content: readFileSync(file, 'utf8') })),
    );
}

function* walk(directory) {
    for (const entry of readdirSync(directory, { withFileTypes: true })) {
        const path = join(directory, entry.name);
        if (entry.isDirectory()) {
            yield* walk(path);
        } else {
            yield path;
        }
    }
}

function displayPath(file) {
    return relative(process.cwd(), file).replaceAll('\\', '/');
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
    const violations = findStateOwnershipViolations(loadWorkspaceFiles());
    if (violations.length > 0) {
        console.error('State ownership guard failed:');
        for (const violation of violations) {
            console.error(`- ${violation}`);
        }
        process.exitCode = 1;
    } else {
        console.log('State ownership guard passed (stateful feature facades are scoped explicitly).');
    }
}
