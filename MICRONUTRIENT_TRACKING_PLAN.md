# Micronutrient Tracking — Implementation Plan

## Goal

Track vitamins and minerals with % Daily Reference Intake (DRI). Let users see micronutrient breakdown per product, per meal, and daily totals on the dashboard.

## Data Source

**USDA FoodData Central — SR Legacy** (free, CC0 public domain):
- 7,793 whole foods with up to 150 nutrient components each
- Download: https://fdc.nal.usda.gov/download-datasets/ (~6.7 MB zip, 54 MB CSV)
- API key (free): https://fdc.nal.usda.gov/api-key-signup/

### Key Nutrient IDs

**Vitamins:**
| ID | Name | Unit |
|----|------|------|
| 1106 | Vitamin A, RAE | mcg |
| 1165 | Thiamin (B1) | mg |
| 1166 | Riboflavin (B2) | mg |
| 1167 | Niacin (B3) | mg |
| 1170 | Pantothenic acid (B5) | mg |
| 1175 | Vitamin B-6 | mg |
| 1177 | Folate (B9) | mcg |
| 1178 | Vitamin B-12 | mcg |
| 1162 | Vitamin C | mg |
| 1110 | Vitamin D (D2+D3) | mcg |
| 1109 | Vitamin E | mg |
| 1185 | Vitamin K | mcg |

**Minerals:**
| ID | Name | Unit |
|----|------|------|
| 1087 | Calcium | mg |
| 1089 | Iron | mg |
| 1090 | Magnesium | mg |
| 1091 | Phosphorus | mg |
| 1092 | Potassium | mg |
| 1093 | Sodium | mg |
| 1095 | Zinc | mg |
| 1098 | Copper | mg |
| 1101 | Manganese | mg |
| 1103 | Selenium | mcg |

## Architecture

### Phase 1 — USDA Data Import (Infrastructure)

1. Download SR Legacy CSV
2. New entities: `UsdaFood`, `UsdaNutrient`, `UsdaFoodNutrient`, `UsdaFoodPortion`
3. EF Core migration for 4 new tables
4. One-time seed command (console app or Initializer job) that bulk-inserts CSV via `COPY`
5. Full-text search index on `UsdaFood.Description` for fast lookup
6. Estimated PostgreSQL size: ~100-200 MB with indexes

### Phase 2 — Link Products to USDA Foods (Domain + Application)

1. Add `UsdaFdcId` (int?) to Product entity — optional FK to USDA food
2. When creating/editing a product, user can search USDA foods and link one
3. Application service `UsdaNutrientResolver` — given a ProductId, resolves micronutrients:
   - If product has `UsdaFdcId` → fetch from `UsdaFoodNutrient` join
   - Scale by product's `BaseAmount` (USDA data is per 100g)
4. Migration to add `UsdaFdcId` column to Products table

### Phase 3 — DRI Reference Table (Domain)

1. `DailyReferenceValue` entity: NutrientId, Value, Unit, AgeGroup, Gender
2. Seed with FDA Daily Values (~30 nutrients × a few demographic groups)
3. `DriCalculator` service — computes `% DV = (amount / dailyValue) * 100`

### Phase 4 — Micronutrient Aggregation (Application)

1. `GetMicronutrientsForDate` query — aggregates all meal items for a date:
   - For each MealItem → resolve product → get UsdaFdcId → sum micronutrients
   - Scale by consumed amount vs base amount
2. `MicronutrientSummaryModel` — list of (nutrient name, amount, unit, % DRI)
3. Integrate into Dashboard snapshot or as separate endpoint

### Phase 5 — Frontend

1. **Product detail**: micronutrient panel showing vitamins/minerals when USDA-linked
2. **USDA food search dialog**: search and link USDA food to user's product
3. **Dashboard micronutrient card**: daily vitamin/mineral bars with % DRI
4. **Daily detail page**: full breakdown per meal

### Phase 6 (Optional) — API for Branded Foods

1. `UsdaFoodDataCentralClient` HttpClient service
2. Search branded foods (400K+) via API when local DB has no match
3. Rate limit: 1,000 req/hour — cache results in local DB
4. Complement with Open Food Facts API (2.8M+ products, international, barcodes)

## CSV Structure (SR Legacy)

| File | Contents |
|------|----------|
| `food.csv` | FDC ID, description, data type |
| `nutrient.csv` | Nutrient ID, name, unit |
| `food_nutrient.csv` | FDC ID + Nutrient ID + amount (main join) |
| `food_portion.csv` | Serving sizes, gram weights |
| `food_category.csv` | Food group classifications |

## Estimated Scope

- Backend: ~10 new entities/tables, ~15 new files, 2 migrations, seed command
- Frontend: ~10 new components (search dialog, nutrient panels, dashboard card)
- Data: ~7,800 foods × ~50 key nutrients = ~390K rows in food_nutrient table
- No breaking changes — UsdaFdcId is nullable, existing products unaffected

## Setup / Deployment

After applying the migration, you need to seed the USDA reference data:

```bash
# 1. Download USDA SR Legacy CSV from https://fdc.nal.usda.gov/download-datasets/
#    Direct link: https://fdc.nal.usda.gov/fdc-datasets/FoodData_Central_sr_legacy_food_csv_2018-04.zip
#    Unzip into a local folder (e.g., ./usda-data)

# 2. Apply the migration
dotnet run --project FoodDiary.Initializer -- update

# 3. Seed USDA data (~7,800 foods, ~390K nutrient rows)
dotnet run --project FoodDiary.Initializer -- seed-usda ./usda-data

# To re-seed (truncates and reimports):
dotnet run --project FoodDiary.Initializer -- seed-usda ./usda-data --force
```

The seed command uses Npgsql binary COPY for fast bulk insert. It only imports SR Legacy foods from the CSV and is idempotent (skips if data already exists).

## Dependencies

- Requires downloading USDA SR Legacy dataset (one-time, 6.7 MB)
- Enables Feature #14 (Nutrition scores by health area)
