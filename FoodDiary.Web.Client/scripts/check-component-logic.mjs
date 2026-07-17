import { readdirSync, readFileSync } from 'node:fs';
import { extname, join, relative } from 'node:path';

import ts from 'typescript';

const COMPONENT_ROOTS = ['src/app', 'projects/fooddiary-admin/src/app'];
const MAX_COMPONENT_METHOD_LINES = 40;
const violations = [];

for (const root of COMPONENT_ROOTS) {
    for (const file of walk(root)) {
        if (extname(file) !== '.ts' || file.endsWith('.spec.ts')) {
            continue;
        }

        const sourceText = readFileSync(file, 'utf8');
        if (!sourceText.includes('@Component')) {
            continue;
        }

        const source = ts.createSourceFile(file, sourceText, ts.ScriptTarget.Latest, true);
        inspectImports(source, file);
        inspectMethods(source, file);
    }
}

if (violations.length > 0) {
    console.error('Component logic guard failed:');
    for (const violation of violations) {
        console.error(`- ${violation}`);
    }
    process.exitCode = 1;
} else {
    console.log(`Component logic guard passed (methods <= ${MAX_COMPONENT_METHOD_LINES} lines; no direct API imports).`);
}

function inspectImports(source, file) {
    for (const statement of source.statements) {
        if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier)) {
            continue;
        }

        const moduleName = statement.moduleSpecifier.text;
        const importsHttpClient =
            moduleName === '@angular/common/http' && statement.importClause?.namedBindings?.getText(source).includes('HttpClient') === true;
        if (/(^|\/)api(\/|$)/u.test(moduleName) || importsHttpClient) {
            violations.push(`${displayPath(file)} imports API transport directly from "${moduleName}"; use a facade.`);
        }
    }
}

function inspectMethods(source, file) {
    visit(source);

    function visit(node) {
        if (ts.isClassDeclaration(node)) {
            for (const member of node.members) {
                if (!isMethodLike(member) || member.body === undefined) {
                    continue;
                }

                const start = source.getLineAndCharacterOfPosition(member.getStart(source)).line;
                const end = source.getLineAndCharacterOfPosition(member.end).line;
                const lineCount = end - start + 1;
                if (lineCount > MAX_COMPONENT_METHOD_LINES) {
                    const name = member.name?.getText(source) ?? 'anonymous';
                    violations.push(
                        `${displayPath(file)}:${start + 1} method ${name} is ${lineCount} lines; move orchestration or business rules to a facade/service.`,
                    );
                }
            }
        }
        ts.forEachChild(node, visit);
    }
}

function isMethodLike(member) {
    return ts.isMethodDeclaration(member) || ts.isGetAccessorDeclaration(member) || ts.isSetAccessorDeclaration(member);
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
