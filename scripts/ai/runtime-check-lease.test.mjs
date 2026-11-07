import test from "node:test";
import assert from "node:assert/strict";
import { mkdirSync, existsSync, writeFileSync, rmSync } from "node:fs";
import { spawn } from 'node:child_process';
import { pathToFileURL } from 'node:url';
import { randomUUID } from "node:crypto";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";
import { acquireRuntimeCheck } from "./runtime-check-lease.mjs";

test("concurrent browser, load or recovery work cannot invalidate the source snapshot", () => {
    const state = {
        id: randomUUID(),
        directory: join(repositoryRoot, ".artifacts/lease-test", randomUUID()),
    };
    mkdirSync(state.directory, { recursive: true });
    const release = acquireRuntimeCheck(state, "recovery");
    try {
        assert.throws(
            () => acquireRuntimeCheck(state, "browser"),
            /sequential/u,
        );
    } finally {
        release();
    }
    acquireRuntimeCheck(state, "load")();
});

test('stale metadata recovery cannot create two process owners', async () => {
    const directory=join(repositoryRoot,'.artifacts/lease-test',randomUUID());
    mkdirSync(directory,{recursive:true});
    const ownerId=randomUUID();
    writeFileSync(join(directory,'verification.lock'),JSON.stringify({ownerId,pid:99999999,identity:'dead-owner'}));
    const worker=join(directory,'worker.mjs');
    const entry=pathToFileURL(join(repositoryRoot,'scripts/ai/runtime-check-lease.mjs')).href;
    writeFileSync(worker,`
import fs from 'node:fs';
import {syncBuiltinESMExports} from 'node:module';
import {join} from 'node:path';
const [directory,label,ownerId]=process.argv.slice(2);
const waitFor=path=>{const start=performance.now();while(!fs.existsSync(path)){if(performance.now()-start>15000)throw new Error('Barrier deadline');Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0,5);}};
const original=fs.readFileSync;
let intercepted=false;
fs.readFileSync=function(path,...args){const value=original.call(this,path,...args);if(!intercepted&&path===join(directory,'verification.lock')){intercepted=true;fs.writeFileSync(join(directory,label+'.read'),'');waitFor(join(directory,label+'.go'));}return value;};
syncBuiltinESMExports();
const {acquireRuntimeCheck}=await import(${JSON.stringify(entry)});
let release;
try{release=acquireRuntimeCheck({id:ownerId,directory},'causal-test');}
catch(error){fs.writeFileSync(join(directory,label+'.rejected'),error.message);process.exit(0);}
try{fs.writeFileSync(join(directory,label+'.held'),'');waitFor(join(directory,'stop'));}finally{release();}
`);
    const children=[];
    const start=label=>{const child=spawn(process.execPath,[worker,directory,label,ownerId],{windowsHide:true,stdio:'ignore'});children.push(child);return child;};
    const wait=async names=>{const started=performance.now();while(!names.some(name=>existsSync(join(directory,name)))){if(performance.now()-started>12000)throw new Error('Missing owned worker marker: '+names.join(','));await new Promise(done=>setTimeout(done,10));}};
    try {
        start('a'); await wait(['a.read']);
        start('b'); await wait(['b.read','b.rejected']);
        writeFileSync(join(directory,'a.go'),''); await wait(['a.held']);
        if(existsSync(join(directory,'b.read'))){writeFileSync(join(directory,'b.go'),'');await wait(['b.held','b.rejected']);}
        assert.equal(existsSync(join(directory,'b.held')),false,'Two real processes acquired the runtime during stale-lock recovery');
        assert.equal(existsSync(join(directory,'b.rejected')),true);
    } finally {
        writeFileSync(join(directory,'stop'),'');
        writeFileSync(join(directory,'a.go'),'');writeFileSync(join(directory,'b.go'),'');
        await Promise.all(children.map(child=>new Promise(done=>{if(child.exitCode!==null)return done();const timer=setTimeout(()=>{child.kill();done();},5000);child.once('exit',()=>{clearTimeout(timer);done();});})));
        rmSync(directory,{recursive:true,force:true});
    }
});
