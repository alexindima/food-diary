import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';
import { mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { format, resolveConfig } from 'prettier';
import ts from 'typescript';

import { canonicalJson, userApiContract } from './api-sdk-contract.mjs';
import { normalizeBinaryResponseTypes } from './api-sdk-normalization.mjs';

const clientRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = resolve(clientRoot, '..');
const sdkRoot = join(clientRoot, 'api-sdk');
const outputRoot = join(clientRoot, 'src/app/shared/api/sdk/generated');
const artifactRoot = join(repoRoot, '.artifacts/sdk');
const config = JSON.parse(await readFile(join(sdkRoot, 'generator.json'), 'utf8'));
const scopes = JSON.parse(await readFile(join(sdkRoot, 'scopes.json'), 'utf8'));
const prettierConfig = await resolveConfig(join(clientRoot, 'package.json'));
const check = process.argv.includes('--check');
const exportApi = process.argv.includes('--export');
const unknownArguments = process.argv.slice(2).filter(argument => !['--check', '--export'].includes(argument));
if (unknownArguments.length) throw new Error(`Unknown SDK arguments: ${unknownArguments.join(', ')}`);

async function run(command, args, env = process.env) {
    return new Promise((accept, reject) => {
        const child = spawn(command, args, { cwd: clientRoot, env, windowsHide: true });
        let output = '';
        child.stdout.on('data', chunk => {
            output = `${output}${chunk}`.slice(-12000);
        });
        child.stderr.on('data', chunk => {
            output = `${output}${chunk}`.slice(-12000);
        });
        child.on('error', reject);
        child.on('close', code => (code === 0 ? accept(output) : reject(new Error(`${command} failed (${code}).\n${output}`))));
    });
}

async function generatorJar() {
    const name = `openapi-generator-cli-${config.version}.jar`;
    const path = join(artifactRoot, 'tools', name);
    let bytes;
    try {
        bytes = await readFile(path);
    } catch (error) {
        if (error.code !== 'ENOENT') throw error;
        console.log(`Downloading pinned OpenAPI Generator ${config.version}...`);
        const response = await fetch(
            `https://repo.maven.apache.org/maven2/org/openapitools/openapi-generator-cli/${config.version}/${name}`,
        );
        if (!response.ok) throw new Error(`Generator download failed: HTTP ${response.status}`);
        bytes = Buffer.from(await response.arrayBuffer());
    }
    const hash = createHash('sha256').update(bytes).digest('hex');
    if (hash !== config.sha256) throw new Error('OpenAPI Generator checksum mismatch; refusing to execute it.');
    await mkdir(dirname(path), { recursive: true });
    await writeFile(path, bytes);
    return path;
}

async function typescriptFiles(root) {
    try {
        const entries = await readdir(root, { recursive: true, withFileTypes: true });
        return entries
            .filter(entry => entry.isFile() && entry.name.endsWith('.ts'))
            .map(entry => relative(root, join(entry.parentPath, entry.name)))
            .sort();
    } catch (error) {
        if (error.code === 'ENOENT') return [];
        throw error;
    }
}

// The upstream Angular templates include unused imports. Keep the repository's
// strict compiler settings and normalize imports through TypeScript itself.
function organizeImports(content, nativeFileName) {
    const fileName = nativeFileName.replaceAll('\\', '/');
    const service = ts.createLanguageService({
        getScriptFileNames: () => [fileName],
        getScriptVersion: () => '0',
        getScriptSnapshot: path => (path === fileName ? ts.ScriptSnapshot.fromString(content) : undefined),
        getCurrentDirectory: () => clientRoot,
        getCompilationSettings: () => ({ noResolve: true, noLib: true }),
        getDefaultLibFileName: () => '',
        fileExists: path => path === fileName,
        readFile: path => (path === fileName ? content : undefined),
    });
    try {
        const changes = service.organizeImports({ type: 'file', fileName }, {}, {});
        for (const change of changes) {
            if (change.fileName !== fileName) throw new Error('Import normalization escaped the generated file.');
            for (const edit of [...change.textChanges].sort((a, b) => b.span.start - a.span.start)) {
                content = content.slice(0, edit.span.start) + edit.newText + content.slice(edit.span.start + edit.span.length);
            }
        }
        return content;
    } finally {
        service.dispose();
    }
}

const contractPath = join(sdkRoot, 'user.openapi.json');
if (exportApi) {
    console.log('Exporting OpenAPI from the real API host (workers and provider I/O disabled)...');
    const rawPath = join(artifactRoot, 'openapi.json');
    await mkdir(artifactRoot, { recursive: true });
    await rm(rawPath, { force: true });
    const output = await run(
        'dotnet',
        [
            'test',
            join(repoRoot, 'Hosts/tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj'),
            '--artifacts-path',
            join(artifactRoot, 'dotnet'),
            '--filter',
            'FullyQualifiedName~OpenApiSdkExportTests',
            '-p:UseSharedCompilation=false',
            '-v',
            'quiet',
        ],
        { ...process.env, FOODDIARY_SDK_OPENAPI_PATH: rawPath },
    );
    let raw;
    try {
        raw = JSON.parse(await readFile(rawPath, 'utf8'));
    } catch (error) {
        throw new Error(`API export did not produce a document.\n${output}`, { cause: error });
    }
    const contract = await format(canonicalJson(userApiContract(raw, scopes)), { ...prettierConfig, filepath: contractPath });
    if (check) {
        if (contract !== (await readFile(contractPath, 'utf8'))) {
            throw new Error('User API changed. Review the contract and run npm run sdk:update.');
        }
    } else {
        await writeFile(contractPath, contract);
    }
}

const stagingRoot = await mkdtemp(join(tmpdir(), 'fooddiary-sdk-'));
const currentContract = JSON.parse(await readFile(contractPath, 'utf8'));
const noContentOperations = new Set();
const binaryOperations = new Set();
for (const path of Object.values(currentContract.paths)) {
    for (const operation of Object.values(path)) {
        const successCodes = Object.keys(operation.responses ?? {}).filter(code => /^2\d\d$/u.test(code));
        if (successCodes.length > 0 && successCodes.every(code => code === '204')) noContentOperations.add(operation.operationId);
        if (
            successCodes.some(code =>
                Object.values(operation.responses[code].content ?? {}).some(media => media.schema?.format === 'binary'),
            )
        ) {
            binaryOperations.add(operation.operationId);
        }
    }
}

// Empty 204 responses have no schema. The upstream generator emits `any` for
// them; expose Angular's usual void contract while preserving the null body.
function normalizeNoContentReturns(content, fileName) {
    const source = ts.createSourceFile(fileName, content, ts.ScriptTarget.Latest, true);
    const edits = [];
    const visit = node => {
        if (ts.isMethodDeclaration(node) && node.body === undefined && noContentOperations.has(node.name.getText(source)) && node.type) {
            const visitType = type => {
                if (type.kind === ts.SyntaxKind.AnyKeyword) edits.push({ start: type.getStart(source), end: type.end });
                ts.forEachChild(type, visitType);
            };
            visitType(node.type);
        }
        ts.forEachChild(node, visit);
    };
    visit(source);
    for (const edit of edits.sort((a, b) => b.start - a.start)) content = content.slice(0, edit.start) + 'void' + content.slice(edit.end);
    return content;
}

try {
    const jar = await generatorJar();
    const generatorConfigPath = join(stagingRoot, 'generator.json');
    await writeFile(generatorConfigPath, JSON.stringify(config.additionalProperties));
    const generatedRoot = join(stagingRoot, 'generated');
    console.log(`Generating user API SDK with OpenAPI Generator ${config.version}...`);
    await run('java', [
        '-jar',
        jar,
        'generate',
        '-g',
        'typescript-angular',
        '-i',
        contractPath,
        '-o',
        generatedRoot,
        '-c',
        generatorConfigPath,
        '--global-property',
        'apiTests=false,modelTests=false,apiDocs=false,modelDocs=false',
    ]);
    const files = await typescriptFiles(generatedRoot);
    if (!files.length) throw new Error('Generator produced no TypeScript files.');
    const existing = await typescriptFiles(outputRoot);
    const differences = existing.filter(file => !files.includes(file));
    for (const file of files) {
        const sourcePath = join(generatedRoot, file);
        const source = (await readFile(sourcePath, 'utf8')).replaceAll(/^\s*\/\/ @ts-ignore\s*$/gm, '');
        const normalized = organizeImports(
            normalizeBinaryResponseTypes(normalizeNoContentReturns(source, sourcePath), sourcePath, binaryOperations),
            sourcePath,
        );
        const content = await format(normalized, { ...prettierConfig, filepath: file });
        const target = join(outputRoot, file);
        let current;
        try {
            current = await readFile(target, 'utf8');
        } catch (error) {
            if (error.code !== 'ENOENT') throw error;
        }
        if (check) {
            if (current !== content) differences.push(file);
        } else if (current !== content) {
            await mkdir(dirname(target), { recursive: true });
            await writeFile(target, content);
        }
    }
    if (check && differences.length) throw new Error(`SDK drift: ${differences.join(', ')}. Run npm run sdk:generate.`);
    if (!check) for (const file of differences) await rm(join(outputRoot, file));
    console.log(`User API SDK ${check ? 'verified' : 'generated'}: ${scopes.length} groups, ${files.length} TypeScript files.`);
} finally {
    // This directory is owned by this invocation and was created by mkdtemp above.
    await rm(stagingRoot, { recursive: true, force: true });
}
