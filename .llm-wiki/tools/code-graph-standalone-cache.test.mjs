import assert from 'node:assert/strict';
import { test } from 'node:test';
import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, utimesSync, writeFileSync } from 'node:fs';
import { resolve, sep } from 'node:path';
import { queryStandaloneIndexCache } from './code-graph-standalone-cache.mjs';

test('isolated standalone cache selects only its source, reuses content and recovers corruption without creating a code graph', async () => {
  const parent = resolve(import.meta.dirname,'../../.artifacts/llm-wiki/standalone-cache-fixtures');
  mkdirSync(parent,{recursive:true});
  const root=mkdtempSync(resolve(parent,'cache-'));
  const generated=resolve(root,'.llm-wiki/generated');
  mkdirSync(generated,{recursive:true});
  const source=resolve(generated,'csharp-symbol-index.json');
  const options={index:'symbols',query:'Alpha',limit:2};
  const write=name=>writeFileSync(source,JSON.stringify({entries:[{name}],empty:[]}));
  try {
    write('Alpha');
    writeFileSync(resolve(generated,'quality-index.json'),'invalid unrelated source');
    const first=queryStandaloneIndexCache(root,options);
    assert.equal(first.ready,true);
    assert.deepEqual(first.index.sections[0],{name:'entries',count:1,items:[{name:'Alpha'}]});
    assert.equal(existsSync(resolve(root,'.artifacts/llm-wiki/code-graph/code-graph.sqlite')),false);
    const cacheTime=statSync(first.cacheDatabasePath).mtimeMs;
    assert.deepEqual(queryStandaloneIndexCache(root,options),first);
    assert.equal(statSync(first.cacheDatabasePath).mtimeMs,cacheTime);
    const sourceTime=statSync(source);
    write('Gamma');utimesSync(source,sourceTime.atime,sourceTime.mtime);
    const changed=queryStandaloneIndexCache(root,{...options,query:'Gamma'});
    assert.equal(changed.index.sections[0].count,1);
    assert.notEqual(changed.cacheDatabasePath,first.cacheDatabasePath);
    const invalidQueryBytes=readFileSync(changed.cacheDatabasePath);
    assert.throws(()=>queryStandaloneIndexCache(root,{...options,query:'Gamma',limit:0}),/between 1 and 50/);
    assert.deepEqual(readFileSync(changed.cacheDatabasePath),invalidQueryBytes);
    writeFileSync(changed.cacheDatabasePath,'corrupt derived cache');
    const repaired=queryStandaloneIndexCache(root,{...options,query:'Gamma'});
    assert.equal(repaired.ready,true);
    assert.equal(repaired.cacheDatabasePath,changed.cacheDatabasePath);
    assert.equal(readdirSync(resolve(root,'.artifacts/llm-wiki/standalone-index-cache')).filter(name=>name.includes('.corrupt-')).length,1);
    write('Concurrent');
    const workers=Array.from({length:4},()=>new Promise((fulfill,reject)=>{
      const child=spawn(process.execPath,[resolve(import.meta.dirname,'read-standalone-index.mjs'),root,'--index=symbols','--query=Concurrent','--limit=2','--prepare-cache=true']);
      let out='',err='';child.stdout.on('data',data=>out+=data);child.stderr.on('data',data=>err+=data);
      child.on('error',reject);child.on('close',code=>code===0?fulfill(JSON.parse(out)):reject(new Error(err)));
    }));
    const settled=await Promise.allSettled(workers);
    for(const result of settled){assert.equal(result.status,'fulfilled',String(result.reason));assert.equal(result.value.ready,true);assert.equal(result.value.index.sections[0].count,1);}
    assert.equal(new Set(settled.map(result=>result.value.cacheDatabasePath)).size,1);
    const directory=resolve(root,'.artifacts/llm-wiki/standalone-index-cache');
    assert.equal(readdirSync(directory).some(name=>name.endsWith('.tmp')),false);
    for (let version=0;version<5;version++) {
      write(`Generation${version}`);
      assert.equal(queryStandaloneIndexCache(root,{...options,query:'Generation'}).ready,true);
    }
    assert.equal(readdirSync(directory).filter(name=>name.endsWith('.sqlite')).length,2);
    writeFileSync(source,'malformed source');
    assert.throws(()=>queryStandaloneIndexCache(root,options),SyntaxError);
    assert.equal(readdirSync(directory).some(name=>name.endsWith('.tmp')),false);
    rmSync(source);
    assert.equal(queryStandaloneIndexCache(root,options).unavailableReason,'compiled-source-missing');
    assert.equal(existsSync(resolve(root,'.artifacts/llm-wiki/code-graph/code-graph.sqlite')),false);
  } finally {
    assert.ok(resolve(root).startsWith(parent+sep));
    rmSync(root,{recursive:true,force:true});
  }
});

test('standalone cache works with SQLite filenames beyond the Windows legacy path limit', () => {
  const parent = resolve(import.meta.dirname,'../../.artifacts/llm-wiki/standalone-cache-fixtures');
  mkdirSync(parent,{recursive:true});
  const owner=mkdtempSync(resolve(parent,'long-'));
  const root=resolve(owner,'nested-source-root-'.repeat(8),'repository');
  const generated=resolve(root,'.llm-wiki/generated');
  mkdirSync(generated,{recursive:true});
  writeFileSync(resolve(generated,'csharp-symbol-index.json'),JSON.stringify({entries:[{name:'LongPath'}]}));
  try {
    const result=queryStandaloneIndexCache(root,{index:'symbols',query:'LongPath',limit:2});
    assert.equal(result.ready,true);
    assert.ok(result.cacheDatabasePath.length>260);
    assert.equal(queryStandaloneIndexCache(root,{index:'symbols',query:'LongPath',limit:2}).ready,true);
  } finally {
    assert.ok(resolve(owner).startsWith(parent+sep));
    rmSync(owner,{recursive:true,force:true});
  }
});
