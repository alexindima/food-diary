# Daily advice JSON import

Open **Daily advices** in the admin menu, download the example JSON, edit it, then choose **Import JSON**. The page shows added and skipped counts and reloads the stored advice list.

```json
{
  "version": 1,
  "advices": [
    { "value": "Пейте воду в течение дня.", "locale": "ru", "weight": 1, "tag": "hydration" },
    { "value": "Drink water throughout the day.", "locale": "en" }
  ]
}
```

- Import limits: 1–1000 entries, at most 5 MiB.
- `value`: required, up to 512 characters after trimming.
- `locale`: required, `ru` or `en`; supported regional forms such as `ru-RU` normalize to the primary language.
- `weight`: optional positive 32-bit integer, default 1. Higher weights increase selection probability.
- `tag`: optional, up to 64 characters after trimming; blank tags become null.
- Duplicates match the normalized text, locale, weight and tag, both against stored entries and earlier entries in the same file. They are skipped. Existing entries are not updated or deleted. A changed tag or weight creates a distinct entry.
- All entries are validated before any are staged. Invalid files add nothing. The Admin command commits the owner context through the existing shared transaction.
- There is no draft/publication step. Imported advice is available for dashboard selection after the import commits; it is not guaranteed to be today's selected advice.

API (Admin role required): `GET /api/v1/admin/daily-advices` and `POST /api/v1/admin/daily-advices/import`. POST requires `Idempotency-Key` and returns `{ "importedCount": 1, "skippedCount": 0 }`. Sequential reimports with new keys are deduplicated by content, as with lesson imports. This is not a database uniqueness guarantee for simultaneous independent imports.

No schema migration is required. Admin consumes only DailyAdvices.Contracts; a separate owner write repository stages changes while the existing dashboard read repository stays read-only.
