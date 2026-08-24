import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.resolve(projectRoot, '..');
const packageJson = JSON.parse(fs.readFileSync(path.join(repoRoot, 'FoodDiary.Web.Client/package.json'), 'utf8'));
const packages = fs.readFileSync(path.join(repoRoot, 'Directory.Packages.props'), 'utf8');
const version = (name) => packages.match(new RegExp(`PackageVersion Include="${name}" Version="([^"]+)"`))?.[1];
const inventory = {
  generatedAt: new Date().toISOString(),
  technologies: {
    dotnet: { version: packages.match(/Microsoft\.Extensions\.Hosting" Version="([^"]+)/)?.[1], source: 'Directory.Packages.props' },
    angular: { version: packageJson.dependencies?.['@angular/core'], source: 'FoodDiary.Web.Client/package.json' },
    typescript: { version: packageJson.devDependencies?.typescript, source: 'FoodDiary.Web.Client/package.json' },
    postgresql: { version: version('Npgsql.EntityFrameworkCore.PostgreSQL'), source: 'Directory.Packages.props' },
    opentelemetry: { version: version('OpenTelemetry.Extensions.Hosting'), source: 'Directory.Packages.props' },
    playwright: { version: packageJson.devDependencies?.['@playwright/test'], source: 'FoodDiary.Web.Client/package.json' }
  }
};
fs.mkdirSync(path.join(projectRoot, 'generated'), { recursive: true });
fs.writeFileSync(path.join(projectRoot, 'generated/inventory.json'), `${JSON.stringify(inventory, null, 2)}\n`);
console.log(`Discovered ${Object.keys(inventory.technologies).length} versioned technologies.`);
