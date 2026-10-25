import { readFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { listFiles, portable, sha256, within } from '../../../Tooling/WebQuality/lib/files.mjs';
import { platformTemplateRules, inspectPlatformStyles } from './platform-rules.mjs';
import { inspectDocument } from './documents.mjs';
import { searchableSource, inspectApplicability, inspectScript } from './inventory.mjs';

const require = createRequire(new URL('../../package.json', import.meta.url));
const dependency = async name => import(pathToFileURL(require.resolve(name)).href);

const nativeRules = new Set([
    'alt-text',
    'elements-content',
    'click-events-have-key-events',
    'interactive-supports-focus',
    'valid-aria',
    'role-has-required-aria',
    'no-positive-tabindex',
    'label-has-associated-control',
    'button-has-type',
    'table-scope',
]);
export const sourceChecks = new Set(
    [...nativeRules]
        .map(rule => `template:${rule}`)
        .concat(
            [
                'template:fd-ui-button-accessible-name',
                'template:image-priority',
                'template:deprecated-image-attributes',
                'css:relative-font-size',
                'css:unitless-line-height',
                'css:bounded-text-wrap',
            ],
            Object.keys(platformTemplateRules).map(rule => `template:${rule}`),
            [
                'loading',
                'scalable-text',
                'render-containment',
                'container-syntax',
                'motion-safety',
                'scrollbar-safety',
                'feature-fallback',
                'form-states',
            ].map(rule => `css:${rule}`),
            [
                'script:dom-sinks',
                'script:dynamic-code',
                'script:message-origin',
                'script:credential-storage',
                'document:platform',
                'source:guide-applicability',
            ],
        ),
);

const templateLocation = node => ({
    start: { line: node.sourceSpan.start.line + 1, column: node.sourceSpan.start.col },
    end: { line: node.sourceSpan.end.line + 1, column: node.sourceSpan.end.col },
});

function attribute(node, name) {
    const item = [...(node.attributes ?? []), ...(node.inputs ?? [])].find(value => value.name.toLowerCase() === name.toLowerCase());
    if (!item) return undefined;
    const value = typeof item.value === 'string' ? item.value : item.value?.ast?.value;
    return value === undefined ? { dynamic: true } : value;
}

const enumAttribute = (node, name) => {
    const value = attribute(node, name);
    return typeof value === 'string' ? value.toLowerCase() : value;
};
const staticBooleanAttribute = value =>
    value !== undefined && value !== null && value !== false && value !== 'false' && !(typeof value === 'object' && value.dynamic);

const imageRules = {
    ...platformTemplateRules,
    'image-priority': {
        meta: { type: 'problem', schema: [], messages: { conflict: 'A high-priority/LCP image must not use loading="lazy".' } },
        create(context) {
            return {
                Element(node) {
                    if (node.name.toLowerCase() !== 'img') return;
                    const priority = attribute(node, 'priority');
                    if (
                        enumAttribute(node, 'loading') === 'lazy' &&
                        (enumAttribute(node, 'fetchpriority') === 'high' || staticBooleanAttribute(priority))
                    ) {
                        context.report({ loc: templateLocation(node), messageId: 'conflict' });
                    }
                },
            };
        },
    },
    'deprecated-image-attributes': {
        meta: {
            type: 'problem',
            schema: [],
            messages: { invalid: 'Remove deprecated importance or invalid loading="auto" from this image.' },
        },
        create(context) {
            return {
                Element(node) {
                    if (
                        node.name.toLowerCase() === 'img' &&
                        (attribute(node, 'importance') !== undefined || enumAttribute(node, 'loading') === 'auto')
                    ) {
                        context.report({ loc: templateLocation(node), messageId: 'invalid' });
                    }
                },
            };
        },
    },
};

export async function createTemplateLinter(checks) {
    const [{ ESLint }, parserModule, pluginModule] = await Promise.all([
        dependency('eslint'),
        dependency('@angular-eslint/template-parser'),
        dependency('@angular-eslint/eslint-plugin-template'),
    ]);
    const parser = parserModule.default ?? parserModule;
    const plugin = pluginModule.default ?? pluginModule;
    const projectConfig = (await import('../../eslint.config.js')).default;
    const projectRule = projectConfig.find(config => config.files?.includes('**/*.html'))?.plugins?.local?.rules?.[
        'fd-ui-button-accessible-name'
    ];
    if (!projectRule) throw new Error('FoodDiary accessible-name rule is unavailable.');
    const rules = {};
    for (const check of checks) {
        if (!check.startsWith('template:')) continue;
        const rule = check.slice('template:'.length);
        if (!nativeRules.has(rule) && !imageRules[rule] && rule !== 'fd-ui-button-accessible-name')
            throw new Error(`Unknown template check: ${check}`);
        rules[
            `${rule === 'fd-ui-button-accessible-name' ? 'fooddiary' : imageRules[rule] ? 'guidance' : '@angular-eslint/template'}/${rule}`
        ] = 'error';
    }
    return new ESLint({
        overrideConfigFile: true,
        ignore: false,
        overrideConfig: [
            {
                files: ['**/*.html'],
                languageOptions: { parser },
                plugins: {
                    '@angular-eslint/template': plugin,
                    guidance: { rules: imageRules },
                    fooddiary: { rules: { 'fd-ui-button-accessible-name': projectRule } },
                },
                rules,
            },
        ],
    });
}

export async function inlineTemplates(text, file) {
    const tsModule = await dependency('typescript');
    const ts = tsModule.default ?? tsModule;
    const source = ts.createSourceFile(file, text, ts.ScriptTarget.Latest, true);
    if (source.parseDiagnostics.length)
        throw new Error(
            `TypeScript parse failed: ${file}: ${ts.flattenDiagnosticMessageText(source.parseDiagnostics[0].messageText, ' ')}`,
        );
    const names = new Set();
    const namespaces = new Set();
    const constants = new Map();
    for (const statement of source.statements) {
        if (ts.isImportDeclaration(statement) && statement.moduleSpecifier.text === '@angular/core') {
            for (const element of statement.importClause?.namedBindings?.elements ?? [])
                if ((element.propertyName?.text ?? element.name.text) === 'Component') names.add(element.name.text);
            if (statement.importClause?.namedBindings && ts.isNamespaceImport(statement.importClause.namedBindings))
                namespaces.add(statement.importClause.namedBindings.name.text);
        }
        if (ts.isVariableStatement(statement) && statement.declarationList.flags & ts.NodeFlags.Const)
            for (const declaration of statement.declarationList.declarations) {
                if (
                    declaration.initializer &&
                    (ts.isStringLiteral(declaration.initializer) || ts.isNoSubstitutionTemplateLiteral(declaration.initializer))
                )
                    constants.set(declaration.name.getText(source), declaration.initializer);
            }
    }
    const templates = [];
    function visit(node) {
        if (
            ts.isDecorator(node) &&
            ts.isCallExpression(node.expression) &&
            (names.has(node.expression.expression.getText(source)) ||
                (ts.isPropertyAccessExpression(node.expression.expression) &&
                    node.expression.expression.name.text === 'Component' &&
                    namespaces.has(node.expression.expression.expression.getText(source))))
        ) {
            const metadata = node.expression.arguments[0];
            if (!metadata || !ts.isObjectLiteralExpression(metadata))
                throw new Error(`Unsupported Component metadata expression in ${file}; source coverage would be incomplete.`);
            if (metadata && ts.isObjectLiteralExpression(metadata))
                for (const property of metadata.properties) {
                    if (ts.isPropertyAssignment(property) && (property.name.text ?? property.name.getText(source)) === 'template') {
                        const literal = ts.isIdentifier(property.initializer)
                            ? constants.get(property.initializer.text)
                            : property.initializer;
                        if (!literal || (!ts.isStringLiteral(literal) && !ts.isNoSubstitutionTemplateLiteral(literal)))
                            throw new Error(`Unsupported inline template expression in ${file}; source coverage would be incomplete.`);
                        templates.push({
                            text: literal.text,
                            line: source.getLineAndCharacterOfPosition(literal.getStart(source)).line,
                        });
                    }
                }
        }
        ts.forEachChild(node, visit);
    }
    visit(source);
    return templates;
}

export async function inspectStyles(text, file) {
    const parser = await dependency('postcss-scss');
    const root = (parser.default ?? parser).parse(text, { from: file });
    const findings = inspectPlatformStyles(root, file);
    root.walkDecls(declaration => {
        const { value } = declaration;
        const prop = declaration.prop.toLowerCase();
        let check;
        if (prop === 'font-size' && /(?:^|[^\w-])\d*\.?\d+px\b/iu.test(value)) check = 'css:relative-font-size';
        if (prop === 'line-height' && /^\s*\d*\.?\d+(?:(?:px|rem|em)\b|%)/iu.test(value)) check = 'css:unitless-line-height';
        if (
            prop === 'text-wrap' &&
            /\b(?:balance|pretty)\b/iu.test(value) &&
            declaration.parent.type === 'rule' &&
            declaration.parent.selector.split(',').some(selector => /^(?:\*|html\s*\*|:root\s*\*)$/u.test(selector.trim()))
        )
            check = 'css:bounded-text-wrap';
        if (check)
            findings.push({
                check,
                file,
                line: declaration.source.start.line,
                column: declaration.source.start.column,
                message: `${prop}: ${value} conflicts with the reviewed ${check.slice(4)} recommendation.`,
            });
    });
    return findings;
}

export async function runSource({ repositoryRoot, policy }) {
    const requirements = policy.requirements.filter(requirement => requirement.engine === 'source');
    for (const requirement of requirements)
        if (!sourceChecks.has(requirement.check)) throw new Error(`Unknown source check: ${requirement.check}`);
    const linter = await createTemplateLinter(requirements.map(requirement => requirement.check));
    // Prove the installed parser/plugin pair can actually report invalid AST nodes.
    const probe = await createTemplateLinter(['template:alt-text']);
    const probeResult = await probe.lintText('<img src="probe.png">', { filePath: 'probe.html' });
    if (!probeResult.some(result => result.messages.some(message => message.ruleId === '@angular-eslint/template/alt-text')))
        throw new Error('Angular template parser/plugin self-test failed.');
    const records = [];
    const fingerprints = [];
    const inventory = [];
    for (const scope of policy.scopes) {
        const files = await listFiles(within(repositoryRoot, scope.path), new Set(['.ts', '.html', '.css', '.scss']));
        const scopeRequirements = requirements.filter(requirement => requirement.scopes.includes(scope.id));
        for (const file of files.filter(
            file => !/\.(?:spec|stories)\.ts$/u.test(file) && !file.includes(`${path.sep}generated${path.sep}`),
        )) {
            const relative = portable(path.relative(repositoryRoot, file));
            const text = await readFile(file, 'utf8');
            inventory.push({ file: relative, searchable: searchableSource(text, relative) });
            fingerprints.push(`${relative}:${sha256(text)}`);
            if (file.endsWith('.ts')) {
                const findings = inspectScript(text, relative);
                for (const requirement of scopeRequirements.filter(item => item.check.startsWith('script:')))
                    records.push({ requirement: requirement.id, findings: findings.filter(item => item.check === requirement.check) });
            }
            if (file.endsWith('.html') && /<html\b/iu.test(text)) {
                const findings = inspectDocument(text, relative);
                for (const requirement of scopeRequirements.filter(item => item.check === 'document:platform'))
                    records.push({ requirement: requirement.id, findings });
            }
            const templates = file.endsWith('.html') ? [{ text, line: 0 }] : file.endsWith('.ts') ? await inlineTemplates(text, file) : [];
            for (const template of templates) {
                const result = (await linter.lintText(template.text, { filePath: `${relative}.html` }))[0];
                if (result.messages.some(message => message.fatal))
                    throw new Error(`Template parse failed: ${relative}: ${result.messages.find(message => message.fatal).message}`);
                for (const requirement of scopeRequirements.filter(item => item.check.startsWith('template:')))
                    records.push({
                        requirement: requirement.id,
                        findings: result.messages
                            .filter(message => message.ruleId?.split('/').at(-1) === requirement.check.slice('template:'.length))
                            .map(message => ({
                                file: relative,
                                line: message.line + template.line,
                                column: message.column,
                                message: message.message,
                            })),
                    });
            }
            if (/\.(?:css|scss)$/u.test(file)) {
                const findings = await inspectStyles(text, relative);
                for (const requirement of scopeRequirements.filter(item => item.check.startsWith('css:')))
                    records.push({
                        requirement: requirement.id,
                        findings: findings.filter(finding => finding.check === requirement.check),
                    });
            }
        }
    }
    for (const relative of policy.additionalSources ?? []) {
        const text = await readFile(within(repositoryRoot, relative), 'utf8');
        inventory.push({ file: relative, searchable: searchableSource(text, relative) });
        fingerprints.push(`${relative}:${sha256(text)}`);
    }
    for (const requirement of requirements.filter(item => item.check === 'source:guide-applicability'))
        for (const review of (policy.guideReviews ?? []).filter(item => item.whenAbsent))
            records.push({ requirement: requirement.id, findings: inspectApplicability(inventory, [review]) });
    return {
        metadata: {
            fingerprint: sha256(fingerprints.sort().join('\n')),
            files: fingerprints.length,
            templates: records.filter(record => record.requirement === requirements.find(item => item.check.startsWith('template:'))?.id)
                .length,
        },
        results: requirements.map(requirement => {
            const matching = records.filter(record => record.requirement === requirement.id);
            const findings = matching.flatMap(record => record.findings);
            return {
                requirement: requirement.id,
                engine: 'source',
                status: findings.length ? 'fail' : matching.length ? 'pass' : 'error',
                observations: matching.length,
                findings,
                detail: matching.length
                    ? requirement.check === 'source:guide-applicability'
                        ? `Rechecked ${matching.length} conditional absence decisions against ${inventory.length} source files and configuration entries.`
                        : `Inspected ${matching.length} source files/templates in the declared scopes.`
                    : 'No applicable source files were inspected.',
            };
        }),
    };
}
