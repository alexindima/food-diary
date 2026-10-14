import assert from 'node:assert/strict';
import { test } from 'node:test';
import { spawn } from 'node:child_process';
import fs from 'node:fs';
import { syncBuiltinESMExports } from 'node:module';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, utimesSync, writeFileSync } from 'node:fs';
import { resolve, sep } from 'node:path';
import { queryStandaloneIndexCache } from './code-graph-standalone-cache.mjs';

test('cold projection and cache reuse each read the source twice and reject an edit before final validation', () => {
  const parent = resolve(import.meta.dirname,'../../.artifacts/llm-wiki/standalone-cache-fixtures');
  mkdirSync(parent,{recursive:true});
  const root=mkdtempSync(resolve(parent,'captured-'));
  const generated=resolve(root,'.llm-wiki/generated');
  mkdirSync(generated,{recursive:true});
  const source=resolve(generated,'csharp-symbol-index.json');
  const originalRead=fs.readFileSync;
  let sourceReads=0,changeBeforeFinalRead=false;
  fs.readFileSync=(path,...argumentsList)=>{
    if (path===source) {
      sourceReads++;
      if (changeBeforeFinalRead && sourceReads===2) {
        const timestamp=statSync(source);
        writeFileSync(source,JSON.stringify({entries:[{name:'Gamma'}]}));
        utimesSync(source,timestamp.atime,timestamp.mtime);
      }
    }
    return originalRead(path,...argumentsList);
  };
  syncBuiltinESMExports();
  try {
    writeFileSync(source,JSON.stringify({entries:[{name:'Alpha'}]}));
    const options={index:'symbols',query:'Alpha',limit:2};
    const cold=queryStandaloneIndexCache(root,options);
    assert.equal(cold.ready,true);
    assert.equal(sourceReads,2);
    const coldCacheBytes=originalRead(cold.cacheDatabasePath);
    sourceReads=0;
    assert.deepEqual(queryStandaloneIndexCache(root,options),cold);
    assert.equal(sourceReads,2);
    writeFileSync(source,JSON.stringify({entries:[{name:'Delta'}]}));
    sourceReads=0;changeBeforeFinalRead=true;
    const raced=queryStandaloneIndexCache(root,{...options,query:'Delta'});
    assert.equal(raced.ready,false);
    assert.equal(raced.unavailableReason,'standalone-index-source-changed');
    assert.equal(sourceReads,2);
    const files=readdirSync(resolve(root,'.artifacts/llm-wiki/standalone-index-cache'));
    assert.equal(files.filter(name=>name.endsWith('.sqlite')).length,1);
    assert.equal(files.some(name=>name.endsWith('.tmp')),false);
    assert.deepEqual(originalRead(cold.cacheDatabasePath),coldCacheBytes);
  } finally {
    fs.readFileSync=originalRead;syncBuiltinESMExports();
    assert.ok(resolve(root).startsWith(parent+sep));
    rmSync(root,{recursive:true,force:true});
  }
});

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
