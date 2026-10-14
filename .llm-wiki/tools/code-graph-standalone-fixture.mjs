import assert from 'node:assert/strict';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { refreshStandaloneIndexes, queryStandaloneIndexSnapshot } from './code-graph-index-query.mjs';

const [action, root] = process.argv.slice(2);
if (action === 'compare') {
  const expected = JSON.parse(readFileSync(resolve(root, 'expected.json'), 'utf8'));
  const actual = JSON.parse(readFileSync(resolve(root, 'actual.json'), 'utf8')).map(text => JSON.parse(text));
  assert.deepEqual(actual, expected.results);
  console.log(`Standalone cross-runtime parity passed: ${actual.length} cases.`);
} else {
  const generated = resolve(root, '.llm-wiki/generated');
  const databasePath = resolve(root, '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
  mkdirSync(resolve(databasePath, '..'), { recursive: true });
  const db = new DatabaseSync(databasePath);
  try {
    if (action === 'schema-stale') db.prepare("UPDATE metadata SET value='old' WHERE key='standalone_index_schema_version'").run();
    else if (action === 'refresh') { db.exec('BEGIN'); refreshStandaloneIndexes(db, root); db.exec('COMMIT'); }
    else if (action === 'prepare') {
      mkdirSync(resolve(generated, 'modules'), { recursive: true });
      const entries = [
        { name:'Alpha_ literal-value', path:'Папка с пробелом/Тип.cs', nested:{name:'User'}, value:9007199254740991 },
        { name:'Beta', path:'second.cs', nested:null, value:0 },
        { name:'İstanbul ΟΣ 𐐀', path:'Unicode.cs', value:1.25 },
      ];
      const sources = { catalog:'repository-catalog.json', symbols:'csharp-symbol-index.json', frontend:'frontend-index.json',
        'frontend-contract':'frontend-contract-index.json','backend-contract':'backend-contract-index.json',
        'architecture-health':'architecture-health-index.json','domain-data':'domain-data-index.json',configuration:'configuration-index.json',
        quality:'quality-index.json',runtime:'runtime-topology.json','sensitive-data':'sensitive-data-index.json' };
      for (const [index, file] of Object.entries(sources)) {
        const document = { generatedAtUtc:null, summary:{index,description:'Кириллица <&>'}, entries, empty:[], nested:{section:entries} };
        if (index === 'configuration') Object.assign(document,{optionTypes:entries,configurationFiles:[{path:'appsettings.json',keys:['Mail:Host','Alpha_']}],environmentFiles:[{path:'.env',variables:['MAIL_HOST']}]});
        writeFileSync(resolve(generated,file), JSON.stringify(document,null,2).replaceAll('\n','\r\n'));
      }
      for (const name of ['alpha.md','user.md','Тип.md','𐐀.md']) writeFileSync(resolve(generated,'modules',name),'module');
      db.exec('CREATE TABLE metadata(key TEXT PRIMARY KEY,value TEXT NOT NULL); BEGIN');
      assert.equal(refreshStandaloneIndexes(db,root),12);
      db.exec('COMMIT');
      const cases = [...Object.keys(sources),'modules'].flatMap(index=>['','Alpha_','USER','a','literal-value','missing % _','Name Name',"' OR 1=1 --",'Папка Тип','İstanbul','ΟΣ','𐐀'].map(query=>({index,query,limit:2})));
      cases.push({index:'configuration',query:'Mail',limit:50});
      writeFileSync(resolve(root,'expected.json'),JSON.stringify({cases,results:cases.map(options=>queryStandaloneIndexSnapshot(db,root,options))}));
      console.log(`Prepared ${cases.length} parity cases.`);
    } else throw new Error(`Unsupported fixture action: ${action}`);
  } finally { db.close(); }
}
