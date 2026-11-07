import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import { execFileSync, spawn } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync, rmSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { DatabaseSync } from 'node:sqlite';
import { sourceFingerprint, stopRuntime } from '../../scripts/ai/task-runtime.mjs';

const repository = resolve(import.meta.dirname, '../..');
test('projection metadata and payload use the same WAL snapshot', async () => {
const fixture = join(repository, '.artifacts/llm-wiki', `system-reader-race-${randomUUID()}`);
mkdirSync(fixture, {recursive:true});
try {
const database = join(fixture, 'projection.sqlite');
const marker = join(fixture, 'metadata-read');
const done = join(fixture, 'generation-published');
const source = readFileSync(join(repository, '.llm-wiki/generated/backend-contract-index.json'), 'utf8').replaceAll('\r\n', '\n');
const sourceHash = createHash('sha256').update(source).digest('hex');
const entry = join(repository, '.llm-wiki/tools/code-graph.mjs');
const queryArgs = [entry, 'backend-contract', `--database=${database}`, '--view=contracts', '--limit=2'];
// Let the actual entry point create its ordinary schema, strictly in this fixture.
execFileSync(process.execPath, queryArgs, { cwd: repository, encoding: 'utf8', timeout: 10000, stdio: ['ignore', 'pipe', 'pipe'] });
const db = new DatabaseSync(database);
db.prepare('INSERT OR REPLACE INTO metadata(key,value) VALUES (?,?)').run('query_source:contracts', sourceHash);
db.prepare('INSERT INTO query_documents(category,record_key,path,source_path,record_kind,source_ordinal,payload_json) VALUES (?,?,?,?,?,?,?)')
  .run('contracts', 'fixture', 'fixture.cs', '.llm-wiki/generated/backend-contract-index.json', 'contract', 0, JSON.stringify({name:'VERIFIED_GENERATION_A'}));
db.close();
const preload = join(fixture, 'schedule-window.mjs');
writeFileSync(preload, `
import { DatabaseSync } from 'node:sqlite';
import { existsSync, writeFileSync } from 'node:fs';
const original = DatabaseSync.prototype.prepare;
let paused = false;
DatabaseSync.prototype.prepare = function(sql) {
  const statement = original.call(this, sql);
  if (!sql.includes("key = 'query_source:contracts'")) return statement;
  const originalGet = statement.get.bind(statement);
  statement.get = (...args) => {
    const value = originalGet(...args);
    if (!paused) {
      paused = true;
      writeFileSync(${JSON.stringify(marker)}, 'metadata snapshot read');
      const clock = performance.now();
      while (!existsSync(${JSON.stringify(done)})) {
        if (performance.now() - clock > 10000) throw new Error('Writer scheduling deadline expired');
        Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 2);
      }
    }
    return value;
  };
  return statement;
};
`);
const writer = join(fixture, 'writer.mjs');
writeFileSync(writer, `
import { DatabaseSync } from 'node:sqlite';
import { existsSync, writeFileSync } from 'node:fs';
const clock=performance.now();
while(!existsSync(${JSON.stringify(marker)})) {
  if(performance.now()-clock>10000) throw new Error('Reader scheduling deadline expired');
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0,2);
}
const db=new DatabaseSync(${JSON.stringify(database)});
db.exec('PRAGMA busy_timeout=5000; BEGIN IMMEDIATE');
db.prepare('UPDATE query_documents SET payload_json=? WHERE category=?').run(JSON.stringify({name:'UNVERIFIED_GENERATION_B'}),'contracts');
db.prepare('UPDATE metadata SET value=? WHERE key=?').run('b'.repeat(64),'query_source:contracts');
db.exec('COMMIT');
db.close();
writeFileSync(${JSON.stringify(done)},'published');
`);
function run(args) {
  return new Promise((resolveRun, reject) => {
    const child=spawn(process.execPath,args,{cwd:repository,windowsHide:true,stdio:['ignore','pipe','pipe']});
    let output='',errors='';
    const timer=setTimeout(()=>{child.kill();reject(new Error('Owned probe deadline expired'));},15000);
    child.stdout.on('data',data=>output+=data);
    child.stderr.on('data',data=>errors+=data);
    child.on('error',error=>{clearTimeout(timer);reject(error);});
    child.on('close',code=>{clearTimeout(timer);if(code)reject(new Error(`Probe exit ${code}: ${errors}`));else resolveRun(output);});
  });
}
const runs = await Promise.allSettled([run(['--import',pathToFileURL(preload).href,...queryArgs]),run([writer])]);
for (const run of runs) if (run.status === 'rejected') throw run.reason;
const output=runs[0].value;
const result=JSON.parse(output);
assert.equal(result.ready, true);
assert.equal(result.sourceHash, sourceHash);
assert.notEqual(result.groups.contracts[0]?.name,'UNVERIFIED_GENERATION_B','Reader returned payload from a generation whose metadata/source hash was not verified');

assert.equal(result.groups.contracts[0]?.name, 'VERIFIED_GENERATION_A');
} finally { rmSync(fixture,{recursive:true,force:true}); }
});

function runtimeFixture(name) {
  const root = join(repository, '.artifacts/llm-wiki', `system-${name}-${randomUUID()}`);
  mkdirSync(root,{recursive:true});
  return root;
}
function gitFixture(root) {
  const git = (...args) => execFileSync('git',['-C',root,...args],{windowsHide:true,stdio:'pipe',timeout:10000});
  git('init','--quiet'); git('add','.');
  git('-c','user.name=WikiFixture','-c','user.email=wiki-fixture@example.invalid','commit','--quiet','-m','baseline');
}
test('runtime source fingerprint reads untracked Unicode paths byte-exactly', () => {
  const root=runtimeFixture('unicode');
  try {
    mkdirSync(join(root,'FoodDiary.Web.Api'));
    writeFileSync(join(root,'FoodDiary.Web.Api/Root.cs'),'class Root {}');
    gitFixture(root);
    const before=sourceFingerprint(root);
    const path=join(root,'FoodDiary.Web.Api/файл с пробелами.cs');
    writeFileSync(path,'class First {}');
    const added=sourceFingerprint(root);
    writeFileSync(path,'class Other {}');
    assert.notEqual(sourceFingerprint(root),added);
    assert.notEqual(added,before);
  } finally {rmSync(root,{recursive:true,force:true});}
});
test('runtime freshness includes direct and transitive API project references', () => {
  const root=runtimeFixture('closure');
  try {
    for(const name of ['FoodDiary.Web.Api','FoodDiary.Presentation.Api','External.Owner']) mkdirSync(join(root,name));
    writeFileSync(join(root,'FoodDiary.Web.Api/Api.csproj'),'<Project><ItemGroup><ProjectReference Include="../FoodDiary.Presentation.Api/Presentation.csproj" /></ItemGroup></Project>');
    writeFileSync(join(root,'FoodDiary.Presentation.Api/Presentation.csproj'),'<Project><ItemGroup><ProjectReference Include="../External.Owner/Owner.csproj" /></ItemGroup></Project>');
    writeFileSync(join(root,'External.Owner/Owner.csproj'),'<Project />');
    for(const name of ['FoodDiary.Presentation.Api','External.Owner'])writeFileSync(join(root,name,'Source.cs'),'class Before {}');
    gitFixture(root);
    const before=sourceFingerprint(root);
    writeFileSync(join(root,'FoodDiary.Presentation.Api/Source.cs'),'class Direct {}');
    const direct=sourceFingerprint(root);
    assert.notEqual(direct,before,'Direct API reference was omitted');
    writeFileSync(join(root,'External.Owner/Source.cs'),'class Transitive {}');
    assert.notEqual(sourceFingerprint(root),direct,'Transitive API reference was omitted');
  } finally {rmSync(root,{recursive:true,force:true});}
});
test('unavailable Docker inspection leaves an observable cleanup failure', () => {
  const root=runtimeFixture('cleanup');
  const directory=join(root,'.artifacts/task-runtime/owned-probe');
  mkdirSync(directory,{recursive:true});
  const path=join(directory,'runtime.json');
  writeFileSync(path,JSON.stringify({checkout:root,directory,id:randomUUID(),status:'ready',processes:[],containers:[{id:'owned-container',name:'owned-container'}]}));
  const previous=process.env.PATH;
  try {
    process.env.PATH='';
    assert.throws(()=>stopRuntime('owned-probe',root),/inspect|docker/iu);
    const state=JSON.parse(readFileSync(path,'utf8'));
    assert.equal(state.status,'cleanup-failed');
    assert.ok(state.failures.length>0);
  } finally {
    if(previous===undefined)delete process.env.PATH;else process.env.PATH=previous;
    rmSync(root,{recursive:true,force:true});
  }
});
