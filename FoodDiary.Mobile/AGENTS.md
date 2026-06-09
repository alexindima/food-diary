# Mobile Shell Guidelines

## Scope
Rules for `FoodDiary.Mobile/`.

## Responsibilities
- Capacitor mobile shell for packaging the FoodDiary Angular client as Android and iOS apps.
- Native platform bridges for mobile-only health providers such as Android Health Connect and iOS HealthKit.
- Mobile build configuration, platform manifests, and native permission declarations.

## Rules
- Keep Angular UI changes in `FoodDiary.Web.Client/`.
- Keep backend sync contracts in application/presentation layers, not in this mobile shell.
- Keep provider-specific native code behind Capacitor plugin interfaces.
- Normalize Health Connect and HealthKit payloads before sending them to the FoodDiary backend.
- Do not store long-lived FoodDiary auth tokens in plain WebView storage when native secure storage is available.

## Commands
- Install: `npm install`
- Build web bundle: `npm run build:web`
- Sync Android project: `npm run sync:android`
- Build Android debug APK: `npm run build:android:debug`
- Open Android project: `npm run open:android`
