# FoodDiary Technology Radar

Public, repository-backed engineering portfolio for `techradar.fooddiary.club`.

The curated assessment lives in `catalog/radar.json`. `npm run discover` reads authoritative repository files and writes detected versions to `generated/inventory.json`. It never changes a ring automatically: moving a technology between Adopt, Trial, Assess, and Hold is an engineering decision.

## Commands

```powershell
npm install
npm run dev
npm run build
npm run validate
```

Run these commands from `FoodDiary.TechRadar`. The production image uses the repository root as its Docker build context:

```powershell
docker build -f FoodDiary.TechRadar/Dockerfile -t fooddiary-tech-radar .
```

## Updating the radar

1. Edit the curated entry in `catalog/radar.json` and attach repository evidence.
2. Run `npm run discover` to refresh detected versions.
3. Run `npm run build`; validation fails when an evidence path is missing.
4. Review ring changes like an ADR-level technical decision.

The visual approach is inspired by Zalando's open-source Tech Radar and Thoughtworks Technology Radar. This implementation uses its own React/D3 renderer rather than copying upstream source code.
