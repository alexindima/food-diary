import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve } from 'node:path';

const repositoryRoot = resolve(import.meta.dirname, '../..');
const requireFromClient = createRequire(resolve(repositoryRoot, 'FoodDiary.Web.Client/package.json'));
const ts = requireFromClient('typescript');
const input = JSON.parse(readFileSync(0, 'utf8').replace(/^\uFEFF/, ''));
const paths = Array.isArray(input) ? input : (input.paths ?? []);

function lineOf(sourceFile, node) {
  return sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile)).line + 1;
}

function textOf(sourceFile, node) {
  return node.getText(sourceFile).replace(/\s+/g, ' ').slice(0, 320);
}

function propertyName(node) {
  if (!node) return null;
  if (ts.isIdentifier(node) || ts.isPrivateIdentifier(node)) return node.text;
  if (ts.isStringLiteralLike(node)) return node.text;
  return null;
}

function declaration(node) {
  if (ts.isClassDeclaration(node)) return ['class', node.name];
  if (ts.isInterfaceDeclaration(node)) return ['interface', node.name];
  if (ts.isTypeAliasDeclaration(node)) return ['type', node.name];
  if (ts.isEnumDeclaration(node)) return ['enum', node.name];
  if (ts.isFunctionDeclaration(node)) return ['function', node.name];
  if (ts.isMethodDeclaration(node)) return ['method', node.name];
  return null;
}

function extract(path) {
  const absolutePath = resolve(repositoryRoot, path);
  const text = readFileSync(absolutePath, 'utf8');
  const sourceFile = ts.createSourceFile(path, text, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS);
  const symbols = [];
  const edges = [];
  const tokens = new Set();
  const addEdge = (kind, target, node, confidence = 'high') => {
    if (!target) return;
    edges.push({ kind, target, line: lineOf(sourceFile, node), evidence: textOf(sourceFile, node), confidence });
  };

  function visit(node) {
    const declared = declaration(node);
    if (declared?.[1]) symbols.push({ kind: declared[0], name: propertyName(declared[1]), line: lineOf(sourceFile, declared[1]) });

    if (ts.isVariableDeclaration(node) && ts.isIdentifier(node.name)) {
      const statement = node.parent?.parent;
      const exported = statement?.modifiers?.some((modifier) => modifier.kind === ts.SyntaxKind.ExportKeyword);
      if (exported) symbols.push({ kind: 'variable', name: node.name.text, line: lineOf(sourceFile, node.name) });
    }
    if (ts.isImportDeclaration(node) && ts.isStringLiteralLike(node.moduleSpecifier)) {
      addEdge('module-import', node.moduleSpecifier.text, node.moduleSpecifier);
    }
    if (ts.isHeritageClause(node)) {
      for (const type of node.types) addEdge(node.token === ts.SyntaxKind.ImplementsKeyword ? 'type-implementation' : 'type-inheritance', type.expression.getText(sourceFile), type);
    }
    if (ts.isNewExpression(node)) addEdge('type-construction', node.expression.getText(sourceFile), node.expression);
    if (ts.isCallExpression(node)) {
      if (node.expression.kind === ts.SyntaxKind.ImportKeyword && ts.isStringLiteralLike(node.arguments[0])) {
        addEdge('angular-lazy-route', node.arguments[0].text, node);
      }
      const callName = ts.isIdentifier(node.expression)
        ? node.expression.text
        : ts.isPropertyAccessExpression(node.expression) ? node.expression.name.text : null;
      if (callName === 'inject' && node.arguments[0]) addEdge('di-service', node.arguments[0].getText(sourceFile), node);
      if (['get', 'post', 'put', 'patch', 'delete'].includes(callName) && node.arguments[0] && ts.isStringLiteralLike(node.arguments[0])) {
        addEdge('http-client', node.arguments[0].text, node, 'medium');
      }
    }
    if (ts.isPropertyAssignment(node)) {
      const name = propertyName(node.name);
      if (name === 'selector' && ts.isStringLiteralLike(node.initializer)) addEdge('component-selector', node.initializer.text, node);
      if (name === 'templateUrl' && ts.isStringLiteralLike(node.initializer)) addEdge('component-resource', node.initializer.text, node);
      if (name === 'path' && ts.isStringLiteralLike(node.initializer)) addEdge('angular-route', node.initializer.text, node);
    }
    if (ts.isParameter(node) && node.type && node.modifiers?.some((modifier) => [ts.SyntaxKind.PrivateKeyword, ts.SyntaxKind.ProtectedKeyword, ts.SyntaxKind.PublicKeyword].includes(modifier.kind))) {
      addEdge('constructor-dependency', node.type.getText(sourceFile), node.type);
    }
    if (ts.isIdentifier(node)) {
      const token = node.text;
      if (token.length >= 3 && (/^[A-ZI]/.test(token) || token.endsWith('Async'))) tokens.add(token);
    }
    ts.forEachChild(node, visit);
  }
  visit(sourceFile);
  if (/(^|\/)(?:tests?|__tests__)(\/|$)|\.(?:spec|test)\.ts$/.test(path)) {
    edges.push({ kind: 'test-ownership', target: path, line: 1, evidence: path, confidence: 'high' });
  }
  return { path, language: 'typescript', symbols, tokens: [...tokens], projectReferences: [], edges };
}

process.stdout.write(JSON.stringify(paths.map(extract)));
