import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

import ts from 'typescript';

const clientRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
// Share immutable library declarations across negative fixtures, never application sources.
const declarationCache = new Map();
const roots = ['src/app', 'projects/fooddiary-admin/src/app'];
const generatedRoots = roots.map(root => `${root}/shared/api/sdk/generated/`);
const httpMethods = new Set(['get', 'post', 'put', 'patch', 'delete', 'request', 'head', 'options', 'jsonp']);
const forbiddenLibraries = /^(?:axios|ky|superagent|node:https?)(?:\/|$)/u;
const interceptors = new Map([
    ['src/app/interceptor/auth.interceptor.ts', new Set(['AuthInterceptor.intercept', 'AuthInterceptor.refreshRequest'])],
    ['src/app/interceptor/frontend-observability.interceptor.ts', new Set(['FrontendObservabilityInterceptor.intercept'])],
    ['src/app/interceptor/global-loading.interceptor.ts', new Set(['GlobalLoadingInterceptor.intercept'])],
    ['src/app/interceptor/retry.interceptor.ts', new Set(['RetryInterceptor.intercept'])],
]);

export function findApiClientUsageViolations(files) {
    const configPath = join(clientRoot, 'tsconfig.json');
    const config = ts.readConfigFile(configPath, ts.sys.readFile);
    if (config.error !== undefined) throw new Error(ts.flattenDiagnosticMessageText(config.error.messageText, '\n'));
    const parsed = ts.parseJsonConfigFileContent(config.config, ts.sys, clientRoot);
    if (parsed.errors.length !== 0)
        throw new Error(ts.formatDiagnosticsWithColorAndContext(parsed.errors, ts.createCompilerHost(parsed.options)));
    const contents = new Map(files.map(file => [normalize(resolve(clientRoot, file.path)), file.content]));
    const host = ts.createCompilerHost(parsed.options);
    const originalSource = host.getSourceFile;
    host.getSourceFile = (file, languageVersion, onError, shouldCreateNewSourceFile) => {
        const content = contents.get(normalize(file));
        if (content !== undefined) return ts.createSourceFile(file, content, languageVersion, true);
        if (!file.endsWith('.d.ts') || !normalize(file).includes('/node_modules/')) {
            return originalSource(file, languageVersion, onError, shouldCreateNewSourceFile);
        }
        if (!declarationCache.has(file))
            declarationCache.set(file, originalSource(file, languageVersion, onError, shouldCreateNewSourceFile));
        return declarationCache.get(file);
    };
    const originalRead = host.readFile;
    host.readFile = file => contents.get(normalize(file)) ?? originalRead(file);
    const originalExists = host.fileExists;
    host.fileExists = file => contents.has(normalize(file)) || originalExists(file);
    const program = ts.createProgram([...contents.keys()], { ...parsed.options, noEmit: true }, host);
    const checker = program.getTypeChecker();
    const violations = [];

    for (const file of files) {
        const path = normalize(file.path);
        if (isGenerated(path) || /\.(?:spec|test)\.ts$/u.test(path)) continue;
        const source = program.getSourceFile(resolve(clientRoot, file.path));
        if (source === undefined) throw new Error(`Source was not loaded: ${path}`);
        const report = (node, reason) =>
            violations.push(`${path}:${source.getLineAndCharacterOfPosition(node.getStart(source)).line + 1}: ${reason}`);
        function visit(node) {
            if (
                (ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) &&
                node.moduleSpecifier !== undefined &&
                ts.isStringLiteral(node.moduleSpecifier) &&
                forbiddenLibraries.test(node.moduleSpecifier.text)
            ) {
                report(node, 'HTTP libraries must not bypass the generated FoodDiary API client.');
            }
            if (ts.isPropertyAccessExpression(node) || ts.isElementAccessExpression(node)) {
                const owner = checker.getNonNullableType(checker.getTypeAtLocation(node.expression)).getSymbol()?.getName();
                const method = memberName(node, checker);
                const signatureOwner = checker
                    .getTypeAtLocation(node)
                    .getCallSignatures()
                    .map(signature => signature.getDeclaration()?.parent)
                    .find(parent => parent !== undefined && ts.isClassDeclaration(parent))?.name?.text;
                if ((owner === 'HttpClient' || signatureOwner === 'HttpClient') && (httpMethods.has(method) || method === undefined)) {
                    if (!allowedHttpMember(path, node, method, checker))
                        report(node, 'Use a generated API method instead of a direct HttpClient request.');
                } else if (owner === 'HttpBackend' && method === 'handle') {
                    report(node, 'HttpBackend must not bypass the generated API client.');
                } else if (owner === 'HttpHandler' && method === 'handle' && !allowedInterceptor(path, node)) {
                    report(node, 'Raw HttpHandler dispatch is only allowed in existing interceptor forwarding.');
                }
                const symbol = ts.isPropertyAccessExpression(node)
                    ? checker.getSymbolAtLocation(node.name)
                    : checker.getTypeAtLocation(node.expression).getProperty(method ?? '');
                if (isBrowserTransport(symbol)) report(node, 'Use a generated API method instead of native browser HTTP.');
                if (isAngularResource(symbol, checker)) report(node, 'httpResource must not replace a generated API method.');
            } else if (ts.isIdentifier(node) && isReference(node) && isBrowserTransport(checker.getSymbolAtLocation(node))) {
                report(node, 'Use a generated API method instead of native browser HTTP.');
            }
            if (ts.isBindingElement(node) && ts.isObjectBindingPattern(node.parent) && ts.isVariableDeclaration(node.parent.parent)) {
                const initializer = node.parent.parent.initializer;
                if (initializer !== undefined) {
                    const type = checker.getNonNullableType(checker.getTypeAtLocation(initializer));
                    const method = (node.propertyName ?? node.name).getText(source).replaceAll(/['"]/gu, '');
                    if (
                        (type.getSymbol()?.getName() === 'HttpClient' && httpMethods.has(method)) ||
                        isBrowserTransport(type.getProperty(method))
                    ) {
                        report(node, 'HTTP method aliases must not bypass the generated API client.');
                    }
                }
            }
            if (ts.isNewExpression(node) && checker.getTypeAtLocation(node).getSymbol()?.getName() === 'HttpRequest') {
                report(node, 'Handwritten HttpRequest construction must not bypass generated API methods.');
            }
            if (ts.isIdentifier(node) && isReference(node)) {
                if (isAngularResource(checker.getSymbolAtLocation(node), checker)) {
                    report(node, 'httpResource must not replace a generated API method.');
                }
            }
            if (
                ts.isCallExpression(node) &&
                (node.expression.kind === ts.SyntaxKind.ImportKeyword ||
                    (ts.isIdentifier(node.expression) && node.expression.text === 'require')) &&
                node.arguments[0] !== undefined &&
                ts.isStringLiteral(node.arguments[0]) &&
                forbiddenLibraries.test(node.arguments[0].text)
            ) {
                report(node, 'HTTP libraries must not bypass the generated FoodDiary API client.');
            }
            if (ts.isCallExpression(node) && ts.isIdentifier(node.expression)) {
                const parent = checker.getResolvedSignature(node)?.getDeclaration()?.parent;
                if (parent !== undefined && ts.isClassDeclaration(parent) && parent.name?.text === 'HttpClient') {
                    report(node, 'HTTP method aliases must not bypass the generated API client.');
                }
            }
            ts.forEachChild(node, visit);
        }
        visit(source);
    }
    return [...new Set(violations)].sort();
}

function normalize(path) {
    return path.replaceAll('\\', '/');
}

function isGenerated(path) {
    return generatedRoots.some(root => path.startsWith(root));
}

function memberName(node, checker) {
    if (ts.isPropertyAccessExpression(node)) return node.name.text;
    const type = checker.getTypeAtLocation(node.argumentExpression);
    return type.isStringLiteral() ? type.value : undefined;
}

function isReference(node) {
    return !(
        ts.isImportSpecifier(node.parent) ||
        (ts.isPropertyAccessExpression(node.parent) && node.parent.name === node) ||
        ts.isBindingElement(node.parent)
    );
}

function isBrowserTransport(symbol) {
    return (
        symbol !== undefined &&
        ['fetch', 'XMLHttpRequest', 'sendBeacon'].includes(symbol.getName()) &&
        symbol.declarations?.some(declaration =>
            /\/lib\.(?:dom|webworker).*\.d\.ts$/u.test(normalize(declaration.getSourceFile().fileName)),
        )
    );
}

function isAngularResource(symbol, checker) {
    const target = symbol !== undefined && symbol.flags & ts.SymbolFlags.Alias ? checker.getAliasedSymbol(symbol) : symbol;
    return (
        target?.getName() === 'httpResource' &&
        target.declarations?.some(declaration => normalize(declaration.getSourceFile().fileName).includes('/@angular/common/'))
    );
}

function owner(node) {
    let method;
    let type;
    let functionName;
    for (let current = node.parent; current !== undefined; current = current.parent) {
        if (method === undefined && ts.isMethodDeclaration(current)) method = current.name.getText();
        if (ts.isClassDeclaration(current)) type ??= current.name?.text;
        if (ts.isFunctionDeclaration(current)) functionName ??= current.name?.text;
    }
    return { method, type, functionName };
}

function allowedInterceptor(path, node) {
    const context = owner(node);
    return (
        interceptors.get(path)?.has(`${context.type}.${context.method}`) === true &&
        node.expression.getText() === 'next' &&
        ts.isCallExpression(node.parent) &&
        node.parent.arguments.length === 1 &&
        ts.isIdentifier(node.parent.arguments[0])
    );
}

function allowedHttpMember(path, node, method, checker) {
    const call = node.parent;
    if (!ts.isCallExpression(call) || call.expression !== node) return false;
    const context = owner(node);
    const argument = call.arguments[0];
    if (
        roots.some(root => path === `${root}/shared/api/sdk/sdk-connection.ts`) &&
        context.functionName === 'createSdkConnection' &&
        method === 'request'
    ) {
        return (
            call.arguments.length === 1 &&
            argument !== undefined &&
            ts.isCallExpression(argument) &&
            ts.isPropertyAccessExpression(argument.expression) &&
            argument.expression.expression.getText() === 'request' &&
            argument.expression.name.text === 'clone' &&
            argument.arguments.length === 1 &&
            ts.isObjectLiteralExpression(argument.arguments[0]) &&
            argument.arguments[0].properties.every(
                property =>
                    (ts.isShorthandPropertyAssignment(property) || ts.isPropertyAssignment(property)) &&
                    ['headers', 'params'].includes(property.name.getText()),
            )
        );
    }
    if (
        path === 'src/app/shared/api/image-upload.service.ts' &&
        context.type === 'ImageUploadService' &&
        context.method === 'uploadToPresignedUrl' &&
        method === 'put'
    ) {
        return argument !== undefined && ts.isIdentifier(argument) && argument.text === 'uploadUrl';
    }
    if (
        path === 'projects/fooddiary-admin/src/app/features/admin-ai-prompts/api/admin-ai-prompts.service.ts' &&
        context.type === 'AdminAiPromptsService' &&
        context.method === 'uploadImage' &&
        method === 'put'
    ) {
        return (
            argument !== undefined &&
            ts.isPropertyAccessExpression(argument) &&
            argument.expression.getText() === 'upload' &&
            argument.name.text === 'uploadUrl'
        );
    }
    if (
        method === 'get' &&
        ((path === 'src/app/shared/i18n/food-diary-translation.loader.ts' &&
            context.type === 'FoodDiaryTranslationLoader' &&
            context.method === 'loadBundle') ||
            (path === 'projects/fooddiary-admin/src/app/shared/i18n/admin-translation.loader.ts' &&
                context.type === 'AdminTranslationLoader' &&
                context.method === 'getTranslation'))
    ) {
        return isTranslationAsset(argument, checker);
    }
    return false;
}

function isTranslationAsset(node, checker, seen = new Set()) {
    if (node === undefined || seen.has(node)) return false;
    seen.add(node);
    if (ts.isIdentifier(node)) {
        const declaration = checker.getSymbolAtLocation(node)?.valueDeclaration;
        return (
            declaration !== undefined && ts.isVariableDeclaration(declaration) && isTranslationAsset(declaration.initializer, checker, seen)
        );
    }
    const prefix = ts.isTemplateExpression(node) ? node.head.text : ts.isStringLiteralLike(node) ? node.text : '';
    const suffix = ts.isTemplateExpression(node) ? node.templateSpans.map(span => span.literal.text).join('') : prefix;
    return prefix.startsWith('./assets/i18n/') && /\.json(?:\?|$)/u.test(suffix);
}

function loadSources() {
    function walk(directory) {
        return readdirSync(join(clientRoot, directory), { withFileTypes: true }).flatMap(entry => {
            const path = `${directory}/${entry.name}`;
            return entry.isDirectory()
                ? isGenerated(`${path}/`)
                    ? []
                    : walk(path)
                : path.endsWith('.ts') && !/\.(?:spec|test)\.ts$/u.test(path)
                  ? [{ path, content: readFileSync(join(clientRoot, path), 'utf8') }]
                  : [];
        });
    }
    return roots.flatMap(root => {
        const files = walk(root);
        if (files.length === 0) throw new Error(`No production TypeScript sources discovered in ${root}.`);
        return files;
    });
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
    const files = loadSources();
    const violations = findApiClientUsageViolations(files);
    if (violations.length !== 0) {
        console.error(`Generated API client guard failed:\n${violations.map(item => `- ${item}`).join('\n')}`);
        process.exitCode = 1;
    } else {
        console.log(`Generated API client guard passed (${files.length} production TypeScript files; explicit transport exceptions only).`);
    }
}
