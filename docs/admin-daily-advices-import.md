# Daily advice groups and JSON import

The admin Daily advices page displays Russian and English translations in one card. Edit translations opens both texts and shared tag/weight together. Saving requires both translations and commits them atomically. Delete advice asks for confirmation and removes every translation in that group. A legacy group with a missing translation can be completed in the editor.

## Paired import (version 2)

Download the example JSON from the admin page, edit it, then choose Import JSON:

```json
{
  "version": 2,
  "advices": [
    {
      "id": "f431755e-3f79-44be-b4ba-d681c8c4b634",
      "ru": "Пейте воду в течение дня.",
      "en": "Drink water throughout the day.",
      "weight": 1,
      "tag": "hydration"
    }
  ]
}
```

- Limits: 1–500 pairs, at most 5 MiB. Counts in the result refer to pairs.
- `id`: stable non-empty UUID, unique within the file. Keep it on subsequent imports.
- `ru` and `en`: required, nonblank, up to 512 characters after trimming. Repeated text within one language in the same file is rejected.
- `weight`: optional positive 32-bit integer, default 1. Higher weights increase selection probability.
- `tag`: optional, up to 64 characters after trimming; blank tags become null.
- An identical existing pair is skipped. A changed existing pair returns a conflict; use the editor to change it.
- An exact single legacy match per language (text, weight and tag) is linked to the explicit group ID, preserving row IDs. Missing translations are inserted. Ambiguous matches, differing metadata, or translations already belonging to another pair reject the entire file without partial writes.
- Import does not delete catalog entries absent from the file. Imported advice becomes available for dashboard selection after commit.

Admin-only endpoints:

- `GET /api/v1/admin/daily-advices/groups`: grouped records, nullable `ru`/`en` for legacy singletons.
- `POST /api/v1/admin/daily-advices/groups/import`: version 2 envelope above; requires `Idempotency-Key`; returns `{ "importedCount": 1, "skippedCount": 0 }`.
- `PUT /api/v1/admin/daily-advices/groups/{id}`: `{ "ru": "...", "en": "...", "weight": 1, "tag": null }`; updates both translations or fills a missing one.
- `DELETE /api/v1/admin/daily-advices/groups/{id}`: deletes the group, returns 204; absent group returns 404.

## Existing catalogs and rollout

Apply migration `20260918123537_GroupDailyAdviceTranslations` before running the updated application. It adds `GroupId` and a unique `(GroupId, Locale)` index. Each existing row initially gets its own UUID; the migration never guesses translation relationships. Import a version 2 file to link known pairs. The database default also supports older writers inserting singleton rows.

Take the normal database backup before rollout. The migration downgrade removes group relationships but preserves language rows. Restoring relationships after a downgrade requires the paired import file or a backup.

The DailyAdvices owner stages writes; the Admin command commits them through the shared transaction. Dashboard selection stays deterministic per locale and keeps existing row IDs. Grouping does not synchronize which advice is selected across languages.

## Legacy import compatibility (version 1)

The old `GET /api/v1/admin/daily-advices` and `POST /api/v1/admin/daily-advices/import` remain unchanged, and the admin uploader still accepts version 1:

```json
{ "version": 1, "advices": [{ "value": "Drink water throughout the day.", "locale": "en", "weight": 1, "tag": "hydration" }] }
```

Version 1 accepts 1–1000 language rows, normalizes supported regional locales to `ru`/`en`, and skips duplicates by normalized text, locale, weight and tag. A changed tag/weight creates a distinct row. It cannot express translation relationships; new rows appear as singleton groups. Version 1 result counts refer to language rows. Simultaneous independent version 1 imports do not have a database content-uniqueness guarantee.
