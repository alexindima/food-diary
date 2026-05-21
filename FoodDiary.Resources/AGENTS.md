# Resources Guidelines

## Scope
Rules for `FoodDiary.Resources/`.

## Role
- Own resource-backed text providers, notification/report copy, and localization resources used by backend flows.
- Depend on application abstractions where needed, not on concrete application handlers, domain entities, infrastructure, presentation, or hosts.

## Rules
- Keep this project free of business orchestration and persistence.
- Keep text keys stable; changing generated user-visible text can be contract-sensitive for tests and snapshots.
- Preserve Russian text encoding. Check edited Cyrillic for mojibake or replacement characters.
- If copy is mirrored in the Angular app, update both frontend locale sets as well.
- Keep namespaces aligned with folders.

## Commands
- Build: `dotnet build FoodDiary.Resources/FoodDiary.Resources.csproj`
