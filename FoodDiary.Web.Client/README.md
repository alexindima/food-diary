# FoodDiary Web Client

Angular frontend for the main Food Diary app, shared UI kit, and admin app.

## Projects

- `src/`: main client application
- `projects/fd-ui-kit/`: shared design system and primitives
- `projects/fooddiary-admin/`: admin application

## Main Commands

- `npm run build`
- `npm run lint`
- `npm run stylelint`
- `npm run prettier`
- `npm run test:ci`
- `npm run test:e2e:client:smoke`
- `npm run build:storybook`

## Quality Gates

The frontend uses:

- ESLint for TypeScript and templates
- Stylelint for CSS/SCSS
- Prettier for formatting
- unit tests via Angular/Vitest
- smoke e2e via Playwright
- i18n consistency checks
- git hooks via Husky

## UX Rules

Async and state-handling conventions are documented in [UX_PLAYBOOK.md](C:/Users/alexi/OneDrive/Документы/GitHub/food-diary/FoodDiary.Web.Client/UX_PLAYBOOK.md).

Use that guide when implementing:

- page loading states
- button loading states
- empty/no-results states
- retry and error handling
- autosave feedback
- destructive actions and confirms
