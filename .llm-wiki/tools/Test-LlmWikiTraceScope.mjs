import assert from 'node:assert/strict';
import { traceCandidateMatchesScope } from './code-graph-trace-scope.mjs';

for (const module of ['Example', 'OtherModule']) {
  const filters = { module, layer: 'Backend' };
  for (const path of [
    `Modules/${module}/Application/Commands/Save/SaveCommandHandler.cs`,
    `Modules\\${module}\\Infrastructure\\Repository.cs`,
    `FoodDiary.Application.${module}/Queries/Read/ReadQuery.cs`,
    `FoodDiary.Application/${module}/Queries/ReadQuery.cs`,
    `FoodDiary.Presentation.Api/Features/${module}/Controller.cs`,
  ]) assert.equal(traceCandidateMatchesScope({ path, language: 'csharp' }, filters), true, path);
  for (const path of [
    `FoodDiary.Application.Abstractions/Favorite${module}/Repository.cs`,
    `Modules/${module}Extra/Application/Handler.cs`,
    `FoodDiary.Application.${module}Extra/Handler.cs`,
    `Modules/Unrelated/Application/${module}.cs`,
  ]) assert.equal(traceCandidateMatchesScope({ path, language: 'csharp' }, filters), false, path);
  const frontend = { path: `FoodDiary.Web.Client/src/app/features/${module}/service.ts`, language: 'typescript' };
  assert.equal(traceCandidateMatchesScope(frontend, filters), false);
  assert.equal(traceCandidateMatchesScope(frontend, { ...filters, layer: 'Frontend' }), true);
  assert.equal(traceCandidateMatchesScope(frontend, { ...filters, layer: 'Auto' }), true);
  assert.equal(traceCandidateMatchesScope({ path: `Modules/${module}/Domain/Entity.cs`, language: 'csharp' }, { ...filters, pathPrefix: `Modules/${module}/Application/` }), false);
}
console.log('Trace candidate scope regression passed: backend/frontend, exact logical/legacy module and path prefixes.');
