import { createHash } from 'node:crypto';
import { spawn } from 'node:child_process';
import { mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { canonicalJson } from './api-sdk-contract.mjs';
import { botApiContract } from './bot-api-contract.mjs';

const clientRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = resolve(clientRoot, '..');
const apiRoot = join(repoRoot, 'FoodDiary.Telegram.Bot/Api');
const outputRoot = join(apiRoot, 'Generated');
const artifactRoot = join(repoRoot, '.artifacts/sdk');
const config = JSON.parse(await readFile(join(clientRoot, 'api-sdk/generator.json'), 'utf8'));
const settings = JSON.parse(await readFile(join(apiRoot, 'generator.json'), 'utf8'));
const scopes = JSON.parse(await readFile(join(apiRoot, 'scopes.json'), 'utf8'));
const check = process.argv.includes('--check');
const exportApi = process.argv.includes('--export');
if (process.argv.slice(2).some(argument => !['--check', '--export'].includes(argument))) throw new Error('Unknown bot SDK argument.');

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

const contractPath = join(apiRoot, 'bot.openapi.json');
if (exportApi) {
    const rawPath = join(artifactRoot, 'bot-openapi.json');
    await mkdir(artifactRoot, { recursive: true });
    await rm(rawPath, { force: true });
    await run(
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
    const source = JSON.parse(await readFile(rawPath, 'utf8'));
    const contract = canonicalJson(botApiContract(source, scopes));
    if (check) {
        if (contract !== (await readFile(contractPath, 'utf8'))) throw new Error('Bot API changed; review and run npm run sdk:bot:update.');
    } else await writeFile(contractPath, contract);
}

const jar = join(artifactRoot, 'tools', `openapi-generator-cli-${config.version}.jar`);
let bytes;
try {
    bytes = await readFile(jar);
} catch (error) {
    if (error.code !== 'ENOENT') throw error;
    const response = await fetch(
        `https://repo.maven.apache.org/maven2/org/openapitools/openapi-generator-cli/${config.version}/openapi-generator-cli-${config.version}.jar`,
    );
    if (!response.ok) throw new Error(`Generator download failed: ${response.status}`);
    bytes = Buffer.from(await response.arrayBuffer());
    if (createHash('sha256').update(bytes).digest('hex') !== config.sha256) throw new Error('Bot generator checksum mismatch.');
    await mkdir(dirname(jar), { recursive: true });
    await writeFile(jar, bytes);
}
if (createHash('sha256').update(bytes).digest('hex') !== config.sha256) throw new Error('Bot generator checksum mismatch.');
const staging = await mkdtemp(join(tmpdir(), 'fooddiary-bot-sdk-'));
async function csFiles(root) {
    try {
        return (await readdir(root, { recursive: true, withFileTypes: true }))
            .filter(entry => entry.isFile() && entry.name.endsWith('.cs'))
            .map(entry => relative(root, join(entry.parentPath, entry.name)))
            .sort();
    } catch (error) {
        if (error.code === 'ENOENT') return [];
        throw error;
    }
}
try {
    const generated = join(staging, 'source');
    await run('java', [
        '-jar',
        jar,
        'generate',
        '-g',
        'csharp',
        '-i',
        contractPath,
        '-o',
        generated,
        '-t',
        join(apiRoot, 'Templates'),
        '-c',
        join(apiRoot, 'generator.json'),
        '--global-property',
        'apiTests=false,modelTests=false,apiDocs=false,modelDocs=false,apis,models',
    ]);
    const sourceRoot = join(generated, 'src', settings.packageName);
    const files = await csFiles(sourceRoot);
    if (!files.length) throw new Error('C# generator produced no files.');
    const existing = await csFiles(outputRoot);
    const differences = existing.filter(file => !files.includes(file));
    for (const file of files) {
        const content = (await readFile(join(sourceRoot, file), 'utf8')).replaceAll('\r\n', '\n').trimEnd() + '\n';
        const target = join(outputRoot, file);
        let current;
        try {
            current = (await readFile(target, 'utf8')).replaceAll('\r\n', '\n');
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
    if (check && differences.length) throw new Error(`Bot SDK drift: ${differences.join(', ')}`);
    if (!check) for (const file of differences) await rm(join(outputRoot, file));
    console.log(`Bot API client ${check ? 'verified' : 'generated'}: ${scopes.length} operations, ${files.length} C# files.`);
} finally {
    await rm(staging, { recursive: true, force: true });
}
