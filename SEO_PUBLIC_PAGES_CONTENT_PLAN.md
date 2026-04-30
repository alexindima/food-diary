# SEO plan for public Food Diary pages

## Goal

Improve organic discovery for Food Diary by making the public pages match how people search for nutrition tools:

- "food diary app", "online food diary", "meal tracker"
- "calorie counter", "macro tracker", "calorie and macro tracker"
- "meal planner", "weekly meal planner", "meal plan with shopping list"
- "weight loss app", "weight tracker", "body progress tracker"
- "intermittent fasting app", "fasting tracker"
- "dietologist app", "share food diary with dietologist"

The product should be positioned as one connected nutrition workspace, not as a single-purpose calorie counter.

## Current Public Page Map

Source routes: `FoodDiary.Web.Client/src/app/app.routes.ts`

| Path | Current role | SEO action |
| --- | --- | --- |
| `/` | Main landing | Rewrite around broad product intent and link to all detailed scenario pages. |
| `/food-diary` | Main SEO hub | Make this the strongest "online food diary" page and use it as the central related page. |
| `/calorie-counter` | Scenario page | Target calorie counting with macro and goals context. |
| `/meal-planner` | Scenario page | Target weekly meal planning and recipes. |
| `/macro-tracker` | Scenario page | Target protein, fats, carbs, macro goals. |
| `/intermittent-fasting` | Scenario page | Target fasting tracker plus meal context. |
| `/meal-tracker` | Scenario page | Target simple meal logging and daily history. |
| `/weight-loss-app` | Scenario page | Target weight loss routines without medical claims. |
| `/dietologist-collaboration` | Scenario page | Target sharing diary data with a dietologist. |
| `/nutrition-planner` | Scenario page | Target structured nutrition planning. |
| `/weight-tracker` | Scenario page | Target weight history with food context. |
| `/body-progress-tracker` | Scenario page | Target body measurements and trend review. |
| `/shopping-list-for-meal-planning` | Scenario page | Target meal plans with grocery lists. |
| `/privacy-policy` | Legal page | Keep mostly informational; do not optimize as a marketing landing page. |

## Implementation Targets

Primary content files:

- `FoodDiary.Web.Client/assets/i18n/ru/landing.json`
- `FoodDiary.Web.Client/assets/i18n/en/landing.json`
- `FoodDiary.Web.Client/assets/i18n/ru/seo.json`
- `FoodDiary.Web.Client/assets/i18n/en/seo.json`
- `FoodDiary.Web.Client/assets/i18n/ru/core.json`
- `FoodDiary.Web.Client/assets/i18n/en/core.json`

Likely structural files:

- `FoodDiary.Web.Client/src/app/features/public/pages/landing/main.component.html`
- `FoodDiary.Web.Client/src/app/features/public/pages/seo-landing/seo-landing-page.component.html`
- `FoodDiary.Web.Client/src/app/features/public/pages/seo-landing/seo-landing-page.component.ts`
- `FoodDiary.Web.Client/src/app/services/seo.service.ts`

Sitemap/prerender files already include all public SEO paths:

- `FoodDiary.Web.Client/assets/sitemap.xml`
- `FoodDiary.Web.Client/assets/sitemap-ru.xml`
- `FoodDiary.Web.Client/src/prerender-routes.txt`

## Keyword Strategy

### Russian Clusters

| Cluster | Primary phrases | Supporting phrases |
| --- | --- | --- |
| Food diary | дневник питания, пищевой дневник, дневник питания онлайн | дневник еды, записывать питание, дневник рациона |
| Calories | счетчик калорий, подсчет калорий, калькулятор калорий еды | калории и БЖУ, учет калорий, норма калорий |
| Macros | трекер БЖУ, счетчик БЖУ, белки жиры углеводы | макронутриенты, баланс БЖУ, белок в рационе |
| Meal planning | планировщик питания, план питания на неделю, меню на неделю | рацион на неделю, планирование рациона, meal prep |
| Meal tracking | трекер приемов пищи, учет приемов пищи | завтрак обед ужин перекусы, история питания |
| Weight loss | приложение для похудения, дневник питания для похудения | контроль веса, похудение с подсчетом калорий |
| Progress | трекер веса, история веса, трекер прогресса тела | замеры тела, талия, график веса |
| Fasting | интервальное голодание, трекер голодания | fasting tracker, окно питания, таймер голодания |
| Dietologist | дневник питания для диетолога, работа с диетологом онлайн | доступ диетолога, поделиться дневником питания |
| Shopping | список покупок для меню, список продуктов на неделю | покупки для план питания, продукты для рецептов |

Use Russian spelling with `счетчик` in page copy only where consistency already uses it, but prefer `счётчик` in polished visible text unless the existing locale style says otherwise. Avoid medical promises like "guaranteed weight loss" or disease claims.

### English Clusters

| Cluster | Primary phrases | Supporting phrases |
| --- | --- | --- |
| Food diary | food diary app, online food diary, nutrition diary | meal diary, food log app, daily food log |
| Calories | calorie counter app, calorie tracker, calorie diary | daily calories, calorie goals, calorie and macro tracker |
| Macros | macro tracker, macro tracking app | protein fat carbs, macro goals, macro balance |
| Meal planning | meal planner app, weekly meal planner | nutrition planner, meal prep planner, meal plans |
| Meal tracking | meal tracker app, meal log | breakfast lunch dinner snacks, meal history |
| Weight loss | weight loss app, weight loss tracker | calorie deficit routine, weight loss meal tracker |
| Progress | weight tracker, body progress tracker | waist tracker, body measurement tracker |
| Fasting | intermittent fasting app, fasting tracker | fasting timer, eating window, fasting schedule |
| Dietologist | dietologist app, food diary for dietologist | share food diary, nutrition specialist collaboration |
| Shopping | meal plan shopping list, grocery list meal planner | weekly groceries, recipe shopping list |

## Site-Wide Content Rules

1. Each SEO page gets one clear search intent and one primary keyword family.
2. H1 should be specific, not generic product branding.
3. First paragraph should repeat the intent naturally and explain why Food Diary is different.
4. Use feature sections for related long-tail keywords.
5. FAQ should answer real pre-signup questions and feed `FAQPage` structured data.
6. Related pages should form clusters:
   - `/food-diary` links to all core pages.
   - `/calorie-counter`, `/macro-tracker`, `/weight-loss-app` link heavily between each other.
   - `/meal-planner`, `/nutrition-planner`, `/shopping-list-for-meal-planning` link heavily between each other.
   - `/weight-tracker`, `/body-progress-tracker`, `/intermittent-fasting` link to progress-oriented pages.
7. Keep claims grounded: Food Diary helps organize, track, review, and collaborate. It does not diagnose, treat, or guarantee results.

## Recommended Plan Improvements

The existing page set is a good start, but it mostly targets broad head terms. To improve SEO depth, add three layers:

1. Better intent separation for existing pages.
2. More content depth inside the SEO page template.
3. A backlog of new long-tail pages for later implementation.

Do not create all new pages at once. Start by rewriting the existing pages, then add new pages only where the product can support unique, useful content.

## Search Intent Map

| Intent type | User mindset | Best page | Content angle |
| --- | --- | --- | --- |
| Find a general tool | "I need somewhere to write what I eat" | `/food-diary` | Online food diary with calories, meals, recipes, and progress. |
| Count intake | "I need to count calories or macros" | `/calorie-counter`, `/macro-tracker` | Daily totals, goals, reusable foods, trend review. |
| Prepare ahead | "I want a weekly menu or routine" | `/meal-planner`, `/nutrition-planner` | Weekly plans, recipes, shopping lists, realistic routines. |
| Reduce weight | "I want an app for losing weight" | `/weight-loss-app` | Food logging, goals, trend review, no guaranteed outcomes. |
| Understand progress | "I want to track weight/body changes" | `/weight-tracker`, `/body-progress-tracker` | Weight, waist, trends, weekly review, food context. |
| Use a method | "I do intermittent fasting" | `/intermittent-fasting` | Fasting windows connected with meals and progress. |
| Work with a specialist | "I need to show my diary to a dietologist" | `/dietologist-collaboration` | Shared access, permissions, structured data. |
| Execute a plan | "I need groceries from my meal plan" | `/shopping-list-for-meal-planning` | Meals, recipes, grocery preparation, weekly flow. |

This map should be used to keep pages from competing with each other. For example, `/calorie-counter` should not become another generic food diary page, and `/weight-loss-app` should not become a broad calorie counter page.

## Template Expansion Ideas

The current SEO page template already has hero, panel, features, steps, FAQ, related links, and CTA. That is enough for a first rewrite, but the pages can become stronger if the template later supports 2-3 more optional sections.

### 1. "Who this is for" section

Purpose:
Target practical user segments and long-tail phrases without stuffing the hero.

Example for `/food-diary`:

- People who want a simple food log.
- People who count calories but also want meal history.
- People who plan weekly meals.
- People who share progress with a dietologist.

Example RU heading:
`Кому подойдет Food Diary`

Example EN heading:
`Who Food Diary is for`

### 2. "What you can track" section

Purpose:
Make feature coverage explicit for search engines and users.

Example items:

- Meals and snacks.
- Calories and macros.
- Products and recipes.
- Water, goals, weight, waist.
- Fasting and weekly check-ins.

This section can be reused across several pages, but the order and emphasis should change by page intent.

### 3. "Common workflows" section

Purpose:
Move beyond generic feature cards and describe real user journeys.

Example for `/meal-planner`:

1. Build a weekly menu from recipes.
2. Check whether the plan fits calorie and macro goals.
3. Turn planned meals into a shopping list.
4. Adjust meals during the week.

Example for `/weight-loss-app`:

1. Set a nutrition direction.
2. Log meals consistently.
3. Compare intake with weight and waist trends.
4. Review weekly and adjust.

### 4. "What Food Diary is not" trust block

Purpose:
Improve credibility and reduce risky health claims.

Example RU:
`Food Diary не обещает быстрый результат и не заменяет медицинскую консультацию. Приложение помогает вести записи, видеть закономерности и готовить данные для самостоятельного анализа или работы со специалистом.`

Example EN:
`Food Diary does not promise quick results or replace medical advice. It helps you log, organize, and review nutrition data for your own routine or specialist collaboration.`

This is especially useful on `/weight-loss-app`, `/intermittent-fasting`, and `/dietologist-collaboration`.

## New Page Backlog

These pages should be considered after the current 12 SEO pages are rewritten and validated. Priority is based on likely search intent fit and whether Food Diary can offer a distinct answer.

### Priority 1

| Proposed path | Primary intent | Why add it |
| --- | --- | --- |
| `/nutrition-tracker` | nutrition tracker app | Broad English query that differs from "food diary" and can cover calories, macros, goals, and progress. |
| `/food-log` | food log app / daily food log | Simpler intent than `/food-diary`; useful for users who only want to record meals. |
| `/protein-tracker` | protein tracker / track protein intake | Strong long-tail macro intent, especially for users focused on satiety, fitness, or body composition. |
| `/meal-prep-planner` | meal prep planner | Similar to meal planning, but focused on preparation, repeat meals, and shopping. |
| `/food-diary-for-weight-loss` | food diary for weight loss | Bridges `/food-diary` and `/weight-loss-app`; should be created only if copy is distinct enough. |

### Priority 2

| Proposed path | Primary intent | Why add it |
| --- | --- | --- |
| `/calorie-and-macro-tracker` | calorie and macro tracker | Captures combined intent more directly than either page alone. |
| `/weekly-meal-planner` | weekly meal planner | More specific version of `/meal-planner`; useful if weekly planning content becomes deep enough. |
| `/waist-tracker` | waist tracker | Supports body progress and existing waist-history product functionality. |
| `/fasting-tracker` | fasting tracker | Simpler and broader than "intermittent fasting app"; can focus on timers and history. |
| `/recipe-nutrition-calculator` | recipe nutrition calculator | Only add if the product experience clearly supports recipe nutrition totals well enough. |

### Priority 3

| Proposed path | Primary intent | Notes |
| --- | --- | --- |
| `/diet-planner` | diet planner app | Risky term because "diet" can imply prescriptive plans; keep copy grounded. |
| `/healthy-meal-planner` | healthy meal planner | Broad and competitive; useful only with strong examples. |
| `/nutrition-journal` | nutrition journal | Lower-volume synonym for food diary; could be merged into `/food-diary`. |
| `/meal-planning-app-for-beginners` | beginner meal planning app | Good long-tail page if the product wants beginner onboarding content. |

## Pages Not Recommended Yet

Avoid creating these until the product has very specific functionality or strong content:

- `/bmi-calculator`: only if there is an actual calculator and clear disclaimer.
- `/calorie-calculator`: only if there is an actual calorie target calculator.
- `/keto-meal-planner`, `/vegan-meal-planner`, `/diabetes-meal-planner`: only if the product supports these use cases specifically and safely.
- Competitor comparison pages: only after there is a clear positioning strategy and legally safe copy.
- Medical condition pages: avoid unless reviewed carefully.

## Content Depth Targets

For the existing 12 SEO pages, use these approximate visible-copy targets after implementation:

| Page type | Suggested visible copy | Reason |
| --- | --- | --- |
| Main landing `/` | 900-1,300 words across sections | Broad product discovery and internal linking hub. |
| Main SEO hub `/food-diary` | 1,000-1,400 words | Should become the strongest non-branded page. |
| Core scenario pages | 800-1,100 words each | Enough depth for intent coverage without becoming repetitive. |
| Narrow pages like shopping list | 650-900 words | Keep practical and specific. |
| Privacy policy | No marketing target | Keep legal clarity. |

Word count is a guideline, not a target to force. Unique usefulness matters more than length.

## FAQ Expansion Plan

Current template supports 4 FAQ items per SEO page. That is fine for the first pass, but stronger pages could use 6-8 FAQ items if layout and structured data support it.

Add FAQ questions that match "People also ask" style:

- `Is a food diary the same as a calorie counter?`
- `What should I write in a food diary?`
- `Can I track macros without counting every calorie?`
- `How do I plan meals for a week?`
- `Can a food diary help with weight loss?`
- `Can I share my food diary with a dietologist?`

RU equivalents:

- `Чем дневник питания отличается от счетчика калорий?`
- `Что записывать в пищевой дневник?`
- `Можно ли следить за БЖУ без отдельного приложения?`
- `Как составить план питания на неделю?`
- `Помогает ли дневник питания при снижении веса?`
- `Можно ли показать пищевой дневник диетологу?`

## SERP Snippet Plan

Each page should have:

- Clear title tag.
- One description with primary keyword and differentiator.
- H1 aligned with title but not identical if possible.
- FAQ answers written as standalone 1-2 sentence answers.

Example RU `/calorie-counter`:

Title:
`Счетчик калорий для еды, БЖУ и целей`

Description:
`Считайте калории и БЖУ в Food Diary, добавляйте приемы пищи и рецепты, сравнивайте день с целями и смотрите недельные тренды.`

H1:
`Счетчик калорий для приемов пищи, БЖУ и дневных целей`

Example EN `/calorie-counter`:

Title:
`Calorie Counter for Meals, Macros, and Goals`

Description:
`Track calories and macros in Food Diary, log meals and recipes, compare daily intake with goals, and review weekly trends.`

H1:
`Calorie Counter App for Meals, Macros, and Daily Goals`

## Measurement Plan

After implementation, track performance by page cluster rather than only total traffic:

- Homepage branded/non-branded impressions.
- Food diary cluster: `/`, `/food-diary`, future `/food-log`, future `/nutrition-tracker`.
- Counting cluster: `/calorie-counter`, `/macro-tracker`, future `/protein-tracker`.
- Planning cluster: `/meal-planner`, `/nutrition-planner`, `/shopping-list-for-meal-planning`.
- Progress cluster: `/weight-loss-app`, `/weight-tracker`, `/body-progress-tracker`.
- Specialist cluster: `/dietologist-collaboration`.

Suggested metrics:

- Google Search Console impressions by page.
- Queries per page to detect cannibalization.
- CTR by title/description.
- Index coverage for prerendered SEO paths.
- Sign-up clicks from SEO pages.
- Scroll depth or section visibility if analytics are available.

If two pages rank for the same query, decide whether to:

- sharpen the page intent,
- change internal links,
- merge content,
- or create a canonical relationship only if one page truly duplicates another.

## Pre-Implementation Research Checklist

Before rewriting production copy, do a short manual SERP pass for the most important queries. This should not turn into a large research project; the goal is to avoid writing pages in a vacuum.

For each priority query, record:

- Top 5 ranking page types: app landing page, article, calculator, marketplace listing, comparison page, template, or forum result.
- Common title patterns.
- Common FAQ questions.
- Search result language: Russian pages should use natural Russian phrasing, English pages should avoid literal translation.
- Whether Google shows calculators, app packs, recipes, videos, or People Also Ask style results.
- Gaps Food Diary can answer better with real product functionality.

Priority queries to check first:

- RU: `дневник питания онлайн`, `счетчик калорий`, `трекер БЖУ`, `планировщик питания`, `приложение для похудения`, `трекер веса`, `интервальное голодание приложение`.
- EN: `food diary app`, `calorie counter app`, `macro tracker`, `meal planner app`, `weight loss app`, `weight tracker`, `intermittent fasting app`.

Save notes in this document or in a separate `SEO_SERP_RESEARCH.md` before implementation if the findings materially change page priorities.

## Page Brief Template

Before editing `seo.json`, each page should have a compact brief. This prevents copy from drifting into the same generic message.

```md
### /example-page

Primary query:

Secondary queries:

User problem:

What Food Diary can credibly solve:

What this page should not claim:

H1:

Meta title:

Meta description:

Sections:

FAQ:

Related pages:

Conversion goal:
```

Use this especially for new pages. Existing pages can use the shorter plans already written below.

## Technical SEO QA Checklist

Use this checklist after content implementation and before merging:

- Each public page has exactly one visible H1.
- Title and meta description are unique per page.
- Canonical URL points to the current language/domain version.
- `hreflang` alternates are present for `en`, `ru`, and `x-default`.
- Sitemap includes every indexable public page in both domain variants.
- Prerender routes include every public SEO path.
- No private/auth pages are accidentally indexable.
- FAQ structured data matches visible FAQ content on the page.
- Structured data does not include content that users cannot see.
- Russian pages render Cyrillic without mojibake or replacement symbols.
- Internal related links are crawlable anchors, not click-only JavaScript controls.
- Register/login CTAs do not block crawlers from reading page content.
- Images used in public sections have useful alt text where they carry meaning.
- Page source after prerender contains the main SEO text, not only an empty app shell.

Google Search Central notes relevant to this checklist:

- Google uses page titles and headings as title-link signals, so titles should be descriptive and page-specific.
- Meta descriptions can influence snippets and should reflect page content.
- Canonical URLs help consolidate duplicate or similar URLs.
- Structured data should describe content that is visible to users.
- Search Console URL Inspection should be used after launch to confirm indexing, canonical choice, and structured data status.

References:

- [Google SEO Starter Guide](https://developers.google.com/search/docs/fundamentals/seo-starter-guide)
- [Google structured data guidelines](https://developers.google.com/search/docs/appearance/structured-data/sd-policies)
- [Google canonical URL guidance](https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls)
- [Search Console URL Inspection](https://support.google.com/webmasters/answer/9012289)

## Localization and Terminology Rules

The project has RU and EN locales, so SEO copy should be localized by intent, not translated word-for-word.

RU style:

- Prefer `приемы пищи`, `пищевой дневник`, `план питания`, `рацион`, `БЖУ`, `вес`, `талия`, `диетолог`.
- Use `счётчик` in polished visible copy if the file supports `ё`; use `счетчик` only if consistency or search spelling is more important in a specific phrase.
- Avoid awkward Anglicisms unless they are already product terms, such as `fasting` in an existing UI context.
- Keep health claims cautious: `помогает`, `поддерживает`, `позволяет сравнивать`, not `лечит`, `гарантирует`, `избавляет`.

EN style:

- Use `food diary`, `meal tracker`, `calorie counter`, `macro tracker`, `meal planner`, `weight tracker`.
- Use `dietologist` because the product already uses that term, but consider adding supporting phrases like `nutrition specialist` in body copy if the product positioning allows it.
- Avoid "best", "guaranteed", "medical", or disease-oriented language.
- Prefer practical verbs: `log`, `track`, `plan`, `review`, `compare`, `share`.

Glossary:

| Concept | RU preferred | EN preferred |
| --- | --- | --- |
| Food diary | дневник питания / пищевой дневник | food diary / nutrition diary |
| Meal logging | учет приемов пищи | meal logging |
| Calories | калории | calories |
| Macros | БЖУ / белки, жиры и углеводы | macros / protein, fat, carbs |
| Meal plan | план питания / меню на неделю | meal plan / weekly menu |
| Body progress | прогресс тела / метрики тела | body progress / body metrics |
| Specialist | диетолог | dietologist / nutrition specialist |

## Conversion and UX Plan

SEO pages should convert without sounding like aggressive sales pages.

Recommended CTA hierarchy:

- Primary CTA: account creation, specific to page intent.
- Secondary CTA: return to homepage or related page, not a competing action.
- Mid-page contextual links: only where they help the reader move to a more specific scenario.

Examples:

- `/calorie-counter`: primary CTA `Start calorie tracking`; related links to `/macro-tracker` and `/weight-loss-app`.
- `/meal-planner`: primary CTA `Start meal planning`; related links to `/shopping-list-for-meal-planning` and `/nutrition-planner`.
- `/dietologist-collaboration`: primary CTA `Create account`; secondary text should emphasize controlled access rather than generic registration.

Track these events if analytics are available:

- SEO page primary CTA click.
- SEO page secondary CTA click.
- Related page link click.
- FAQ open.
- Language switch from public pages.

## Rollout Plan

Phase 1: Rewrite existing content only.

- Update `landing.json`, `seo.json`, and `core.json`.
- Keep routes unchanged.
- Build and visually check RU/EN pages.

Phase 2: Improve template depth.

- Add optional sections if needed: "Who this is for", "What you can track", "Common workflows", trust block.
- Keep sections driven by i18n keys so both locales remain complete.

Phase 3: Add high-priority new pages.

- Start with `/nutrition-tracker`, `/food-log`, `/protein-tracker`, and `/meal-prep-planner`.
- Add routes, sitemap, prerender routes, metadata, structured data, RU/EN copy, and internal links.

Phase 4: Measure and prune.

- Use Search Console query/page data.
- Strengthen pages that get impressions but low CTR.
- Merge or redirect pages that cannibalize each other without adding unique value.

## Landing Page Rewrite

### Search intent

Broad branded and non-branded entry: people looking for a food diary, calorie counter, meal planner, macro tracker, and weight/progress tracker.

### Proposed page structure

1. Hero: broad value proposition with four search-intent chips.
2. Product preview: daily diary, calories/macros, body progress.
3. Use-case grid: track, plan, review, collaborate.
4. Dietologist section: shared access and permissions.
5. Steps: create account, add meals, review routine.
6. FAQ: broad product objections.
7. SEO guide links: all scenario pages grouped by task.

### RU draft

Hero overline:
`Дневник питания, счетчик калорий и планировщик рациона`

H1:
`Food Diary помогает записывать питание, считать калории и видеть прогресс в одном месте`

Subtitle:
`Ведите пищевой дневник онлайн, добавляйте приемы пищи и рецепты, следите за калориями и БЖУ, планируйте меню на неделю и сравнивайте питание с весом, талией и недельными трендами.`

Search panel title:
`Для тех, кто ищет не просто счетчик калорий, а систему для питания`

Search panel text:
`Food Diary соединяет дневник еды, планировщик питания, трекер веса, интервальное голодание и работу с диетологом. Так ежедневные записи превращаются в понятную картину, а не остаются разрозненными цифрами.`

Feature section title:
`Один продукт для учета питания, планирования и анализа`

Feature subtitle:
`Начните с простого дневника приемов пищи, а затем добавляйте цели, рецепты, планы питания, списки покупок, графики прогресса и совместную работу со специалистом.`

FAQ questions:

- `Food Diary подходит как дневник питания онлайн?`
- `Можно ли считать калории и БЖУ в том же приложении?`
- `Можно ли планировать питание на неделю?`
- `Можно ли показать дневник диетологу?`

CTA title:
`Начните с дневника питания и соберите вокруг него всю рутину`

CTA subtitle:
`Создайте аккаунт, чтобы записывать еду, видеть калории и макросы, планировать рацион и отслеживать прогресс без отдельных таблиц и заметок.`

### EN draft

Hero overline:
`Food diary, calorie counter, and nutrition planner`

H1:
`Food Diary helps you log meals, track calories, and understand progress in one place`

Subtitle:
`Keep an online food diary, add meals and recipes, track calories and macros, plan weekly menus, and compare nutrition with weight, waist, and weekly trends.`

Search panel title:
`For people who need more than a basic calorie counter`

Search panel text:
`Food Diary connects meal logging, nutrition planning, weight tracking, intermittent fasting, and dietologist collaboration so daily entries become a clearer routine instead of scattered numbers.`

CTA title:
`Start with a food diary and build your full nutrition routine around it`

CTA subtitle:
`Create an account to log food, see calories and macros, plan nutrition, and track progress without separate spreadsheets or notes.`

## SEO Page Text Plan

Each SEO page should keep the existing component layout:

- Hero: eyebrow, H1, subtitle, CTAs, chips.
- Context panel: why this page matters.
- Features: 6 cards.
- Steps: 3 cards.
- FAQ: 4 questions.
- Related pages.
- CTA.

### `/food-diary`

Primary keyword:
`online food diary` / `дневник питания онлайн`

RU H1:
`Дневник питания онлайн для еды, калорий, БЖУ и прогресса`

RU subtitle:
`Food Diary помогает вести пищевой дневник без разрыва между записями, целями и результатом: добавляйте приемы пищи, считайте калории и БЖУ, планируйте рацион и смотрите, как меняются вес, талия и привычки.`

RU panel title:
`Пищевой дневник полезнее, когда он связан с планированием и аналитикой`

RU features:

- `Быстро записывайте завтраки, обеды, ужины и перекусы`
- `Смотрите калории и БЖУ по мере заполнения дня`
- `Используйте продукты и рецепты повторно`
- `Планируйте рацион заранее`
- `Сравнивайте питание с весом и талией`
- `Открывайте выбранные данные диетологу`

RU FAQ:

- `Чем Food Diary отличается от обычного дневника еды?`
- `Можно ли использовать его бесплатно на старте?`
- `Подойдет ли дневник для похудения или набора массы?`
- `Можно ли вести дневник вместе с диетологом?`

EN H1:
`Online Food Diary for Meals, Calories, Macros, and Progress`

EN subtitle:
`Food Diary helps you keep a food diary without separating logs, goals, and results: add meals, track calories and macros, plan nutrition, and review weight, waist, and habit trends.`

### `/calorie-counter`

Primary keyword:
`calorie counter app` / `счетчик калорий`

RU H1:
`Счетчик калорий для приемов пищи, БЖУ и дневных целей`

RU subtitle:
`Считайте калории в контексте реального питания: добавляйте блюда и продукты, отслеживайте белки, жиры и углеводы, сравнивайте день с целями и смотрите недельные тренды.`

RU panel title:
`Калории работают лучше, когда рядом видны блюда, макросы и цель`

RU features:

- `Итоги дня обновляются во время записи`
- `БЖУ отображается рядом с калориями`
- `Цели помогают понимать дневной контекст`
- `Рецепты уменьшают ручной ввод`
- `Недельные тренды показывают стабильность`
- `Данные можно обсудить с диетологом`

RU FAQ:

- `Food Diary заменяет отдельный счетчик калорий?`
- `Можно ли считать калории и БЖУ вместе?`
- `Подходит ли счетчик калорий для снижения веса?`
- `Можно ли сохранить продукты и рецепты для повторного учета?`

EN H1:
`Calorie Counter App for Meals, Macros, and Daily Goals`

EN subtitle:
`Track calories in the context of real meals: add foods and recipes, monitor protein, fat, and carbs, compare the day with goals, and review weekly trends.`

### `/meal-planner`

Primary keyword:
`meal planner app` / `планировщик питания`

RU H1:
`Планировщик питания на неделю с рецептами и списками покупок`

RU subtitle:
`Собирайте меню на несколько дней или неделю, используйте сохраненные рецепты, связывайте план с покупками и держите питание рядом с целями по калориям и БЖУ.`

RU panel title:
`План питания должен быть исполнимым, а не просто красивым списком`

RU features:

- `Планируйте завтраки, обеды, ужины и перекусы`
- `Добавляйте рецепты как готовые блоки`
- `Связывайте план с продуктами и покупками`
- `Смотрите план рядом с целями питания`
- `Меняйте неделю без потери структуры`
- `Обсуждайте план со специалистом при необходимости`

RU FAQ:

- `Можно ли составить план питания на неделю?`
- `Можно ли использовать свои рецепты?`
- `Есть ли связь со списками покупок?`
- `Можно ли менять план по ходу недели?`

EN H1:
`Weekly Meal Planner App with Recipes and Shopping Lists`

EN subtitle:
`Build menus for several days or a full week, reuse saved recipes, connect plans with shopping lists, and keep nutrition targets close to the plan.`

### `/macro-tracker`

Primary keyword:
`macro tracker` / `трекер БЖУ`

RU H1:
`Трекер БЖУ для белков, жиров, углеводов и ежедневного питания`

RU subtitle:
`Следите за белками, жирами и углеводами вместе с калориями, приемами пищи, рецептами и целями, чтобы понимать не только сколько вы едите, но и из чего состоит рацион.`

RU panel title:
`БЖУ легче контролировать, когда макросы связаны с реальными блюдами`

RU features:

- `Видите белки, жиры и углеводы в дневном итоге`
- `Сравнивайте макросы с целями`
- `Проверяйте баланс по каждому приему пищи`
- `Используйте рецепты с уже известной нутрициологией`
- `Находите повторяющиеся паттерны в рационе`
- `Связывайте макросы с весом и прогрессом`

RU FAQ:

- `Можно ли считать БЖУ без отдельного приложения?`
- `Можно ли отслеживать белок по дням?`
- `Чем трекер БЖУ отличается от счетчика калорий?`
- `Подходит ли это для изменения состава тела?`

EN H1:
`Macro Tracker for Protein, Fat, Carbs, and Daily Meals`

EN subtitle:
`Track protein, fat, and carbs alongside calories, meals, recipes, and goals so you can understand not just how much you eat, but what your nutrition is made of.`

### `/intermittent-fasting`

Primary keyword:
`intermittent fasting app` / `интервальное голодание`

RU H1:
`Приложение для интервального голодания с дневником питания и прогрессом`

RU subtitle:
`Ведите интервальное голодание рядом с приемами пищи, калориями, водой и метриками тела, чтобы видеть не только окно питания, но и то, как режим связан с рационом.`

RU panel title:
`Трекер голодания полезнее, когда он не оторван от питания`

RU features:

- `Отслеживайте окна голодания и питания`
- `Связывайте fasting с приемами пищи`
- `Смотрите калории и БЖУ внутри выбранного режима`
- `Отмечайте регулярность и самочувствие`
- `Сравнивайте режим с весом и талией`
- `При необходимости делитесь данными со специалистом`

RU FAQ:

- `Можно ли использовать Food Diary как трекер голодания?`
- `Можно ли считать калории во время интервального голодания?`
- `Поддерживает ли приложение разные графики?`
- `Можно ли смотреть прогресс вместе с fasting?`

EN H1:
`Intermittent Fasting App with Meal Tracking and Progress Review`

EN subtitle:
`Track intermittent fasting alongside meals, calories, hydration, and body metrics so your eating window stays connected to the rest of your nutrition routine.`

### `/meal-tracker`

Primary keyword:
`meal tracker app` / `трекер приемов пищи`

RU H1:
`Трекер приемов пищи для завтраков, обедов, ужинов и перекусов`

RU subtitle:
`Записывайте ежедневные приемы пищи, переиспользуйте продукты и рецепты, смотрите историю еды и держите калории, БЖУ и комментарии рядом с каждой записью.`

RU panel title:
`Учет приемов пищи должен быть быстрым для обычного дня`

RU features:

- `Добавляйте завтрак, обед, ужин и перекусы`
- `Сохраняйте историю приемов пищи`
- `Используйте продукты и рецепты повторно`
- `Видите итоги питания по дню`
- `Добавляйте контекст и комментарии`
- `Переходите от записей к планированию`

RU FAQ:

- `Можно ли просто записывать, что я ел?`
- `Можно ли хранить историю приемов пищи?`
- `Можно ли повторять частые блюда?`
- `Можно ли потом перейти к подсчету калорий?`

EN H1:
`Meal Tracker App for Breakfast, Lunch, Dinner, and Snacks`

EN subtitle:
`Log daily meals, reuse foods and recipes, review meal history, and keep calories, macros, and notes close to each entry.`

### `/weight-loss-app`

Primary keyword:
`weight loss app` / `приложение для похудения`

RU H1:
`Приложение для похудения с дневником питания, целями и прогрессом`

RU subtitle:
`Food Diary помогает организовать снижение веса без обещаний быстрых результатов: ведите питание, сравнивайте калории с целями, отслеживайте вес и талию и корректируйте рутину по трендам.`

RU panel title:
`Для похудения важны не только калории за один день, а устойчивая система`

RU features:

- `Задавайте цели по питанию`
- `Смотрите фактические калории и БЖУ`
- `Отслеживайте вес и талию`
- `Используйте недельные обзоры`
- `Планируйте питание заранее`
- `Подключайте диетолога при необходимости`

RU FAQ:

- `Можно ли использовать Food Diary для похудения?`
- `Гарантирует ли приложение снижение веса?`
- `Можно ли отслеживать вес и питание вместе?`
- `Можно ли работать с диетологом?`

EN H1:
`Weight Loss App for Food Tracking, Goals, and Progress Review`

EN subtitle:
`Food Diary helps organize weight loss routines without promising quick results: log meals, compare calories with goals, track weight and waist, and adjust based on trends.`

### `/dietologist-collaboration`

Primary keyword:
`share food diary with dietologist` / `дневник питания для диетолога`

RU H1:
`Дневник питания для работы с диетологом и управляемым доступом`

RU subtitle:
`Пригласите диетолога в Food Diary, откройте только выбранные категории данных и обсуждайте приемы пищи, цели, статистику, вес, талию и fasting без ручных отчетов.`

RU panel title:
`Совместная работа лучше, когда данные уже структурированы`

RU features:

- `Приглашение диетолога по email`
- `Выбор категорий данных для доступа`
- `Общий контекст по питанию и целям`
- `История веса, талии и недельных обзоров`
- `Планы питания и рекомендации в одном процессе`
- `Возможность изменить доступ позже`

RU FAQ:

- `Что видит диетолог после приглашения?`
- `Можно ли выбрать, какими данными делиться?`
- `Можно ли закрыть доступ позже?`
- `Заменяет ли это консультацию специалиста?`

EN H1:
`Food Diary Sharing for Dietologist Collaboration`

EN subtitle:
`Invite a dietologist to Food Diary, share only selected data categories, and review meals, goals, statistics, weight, waist, and fasting without manual reports.`

### `/nutrition-planner`

Primary keyword:
`nutrition planner` / `планировщик рациона`

RU H1:
`Планировщик рациона для целей, приемов пищи и недельного обзора`

RU subtitle:
`Планируйте рацион вокруг реальных блюд, целей по калориям и БЖУ, списков покупок и недельного анализа, чтобы питание было не только записано, но и заранее продумано.`

RU panel title:
`План рациона должен связывать цели с тем, что вы реально едите`

RU features:

- `Задавайте направление питания`
- `Стройте рацион из продуктов и рецептов`
- `Связывайте план с покупками`
- `Смотрите соответствие целям`
- `Корректируйте план по итогам недели`
- `Используйте данные для работы со специалистом`

RU FAQ:

- `Чем планировщик рациона отличается от дневника питания?`
- `Можно ли планировать по калориям и БЖУ?`
- `Можно ли связать рацион со списком покупок?`
- `Можно ли пересматривать план каждую неделю?`

EN H1:
`Nutrition Planner for Goals, Meals, and Weekly Review`

EN subtitle:
`Plan nutrition around real meals, calorie and macro goals, shopping lists, and weekly review so your routine is not only logged but planned ahead.`

### `/weight-tracker`

Primary keyword:
`weight tracker` / `трекер веса`

RU H1:
`Трекер веса с историей питания, целями и недельными трендами`

RU subtitle:
`Записывайте вес и смотрите динамику рядом с приемами пищи, калориями, целями и недельными обзорами, чтобы понимать прогресс без изолированных взвешиваний.`

RU panel title:
`Вес легче интерпретировать, когда рядом виден режим`

RU features:

- `Ведите историю веса`
- `Смотрите тренды, а не только отдельные значения`
- `Сравнивайте вес с питанием`
- `Держите цели рядом с прогрессом`
- `Используйте недельные check-in`
- `Делитесь контекстом с диетологом`

RU FAQ:

- `Можно ли использовать Food Diary как трекер веса?`
- `Можно ли смотреть график веса?`
- `Можно ли сравнивать вес с калориями?`
- `Можно ли отслеживать вес вместе с талией?`

EN H1:
`Weight Tracker with Meal History, Goals, and Weekly Trends`

EN subtitle:
`Record weight and review trends alongside meals, calories, goals, and weekly check-ins so progress is easier to understand than isolated weigh-ins.`

### `/body-progress-tracker`

Primary keyword:
`body progress tracker` / `трекер прогресса тела`

RU H1:
`Трекер прогресса тела для веса, талии и долгосрочных трендов`

RU subtitle:
`Отслеживайте изменения тела через несколько сигналов: вес, талию, питание, калории, недельные обзоры и привычки, которые помогают объяснить динамику.`

RU panel title:
`Прогресс тела не сводится к одной цифре на весах`

RU features:

- `Записывайте вес и талию`
- `Смотрите динамику на дистанции`
- `Сравнивайте метрики тела с питанием`
- `Используйте недельный обзор`
- `Снижайте шум от случайных колебаний`
- `Держите прогресс рядом с целями`

RU FAQ:

- `Какие метрики тела можно отслеживать?`
- `Зачем отслеживать талию вместе с весом?`
- `Можно ли смотреть тренды за период?`
- `Можно ли связать прогресс тела с питанием?`

EN H1:
`Body Progress Tracker for Weight, Waist, and Long-Term Trends`

EN subtitle:
`Track body changes through multiple signals: weight, waist, nutrition, calories, weekly reviews, and habits that help explain the trend.`

### `/shopping-list-for-meal-planning`

Primary keyword:
`meal plan shopping list` / `список покупок для меню`

RU H1:
`План питания со списком покупок для меню на неделю`

RU subtitle:
`Связывайте блюда, рецепты и продукты в одном плане, чтобы заранее подготовить список покупок и проще пройти неделю без спонтанных решений.`

RU panel title:
`План питания становится полезнее, когда сразу понятно, что купить`

RU features:

- `Собирайте меню из блюд и рецептов`
- `Переходите от плана к продуктам`
- `Готовьте покупки на неделю`
- `Переиспользуйте частые рецепты`
- `Сокращайте решения в последний момент`
- `Держите план, покупки и дневник вместе`

RU FAQ:

- `Можно ли сделать список покупок из плана питания?`
- `Можно ли использовать рецепты для списка продуктов?`
- `Подходит ли это для меню на неделю?`
- `Можно ли менять список после изменения плана?`

EN H1:
`Meal Plan with Shopping List for Weekly Menus`

EN subtitle:
`Connect meals, recipes, and foods in one plan so you can prepare a shopping list in advance and get through the week with fewer last-minute decisions.`

## Metadata Plan

Update `core.json` SEO titles and descriptions to match the new H1/search intent.

Recommended title pattern:

- Keep under roughly 55-65 characters where possible.
- Use `| Food Diary` from `SeoService`, so translation values should not repeat the brand unless needed.

Examples:

- RU `/food-diary`: `Дневник питания онлайн для еды, калорий и БЖУ`
- EN `/food-diary`: `Online Food Diary for Meals, Calories, and Macros`
- RU `/calorie-counter`: `Счетчик калорий для еды, БЖУ и дневных целей`
- EN `/calorie-counter`: `Calorie Counter for Meals, Macros, and Goals`

Recommended descriptions:

- One sentence, 130-160 characters when possible.
- Include primary keyword and one differentiator.
- Avoid vague claims like "best app".

## Structured Data Plan

Current `SeoService` already emits:

- `WebSite`
- `SoftwareApplication`
- `WebPage`
- `FAQPage`

Keep this structure. During implementation, make sure all FAQ answers in `seo.json` and `landing.json` are complete enough to stand alone in search results.

Potential later improvement:

- Add `sameAs` only after public social/profile URLs exist.
- Add `inLanguage` already exists and should remain.
- Keep `offers.price = 0` only if the free start remains true.

## Internal Linking Plan

Landing FAQ guide links should be grouped by user task instead of one flat list if the template is changed:

- Track daily nutrition: `/food-diary`, `/meal-tracker`, `/calorie-counter`, `/macro-tracker`
- Plan ahead: `/meal-planner`, `/nutrition-planner`, `/shopping-list-for-meal-planning`
- Review progress: `/weight-loss-app`, `/weight-tracker`, `/body-progress-tracker`
- Specialist and routines: `/dietologist-collaboration`, `/intermittent-fasting`

SEO related pages should prioritize cluster relevance:

- `/food-diary`: all major pages.
- `/calorie-counter`: `/macro-tracker`, `/weight-loss-app`, `/meal-tracker`, `/food-diary`.
- `/meal-planner`: `/nutrition-planner`, `/shopping-list-for-meal-planning`, `/food-diary`, `/calorie-counter`.
- `/macro-tracker`: `/calorie-counter`, `/weight-loss-app`, `/meal-tracker`, `/food-diary`.
- `/intermittent-fasting`: `/weight-tracker`, `/body-progress-tracker`, `/calorie-counter`, `/food-diary`.
- `/dietologist-collaboration`: `/food-diary`, `/nutrition-planner`, `/weight-loss-app`, `/meal-planner`.

## Implementation Checklist

1. Rewrite RU/EN landing copy in `landing.json`.
2. Rewrite RU/EN SEO page copy in `seo.json`.
3. Update RU/EN meta titles and descriptions in `core.json`.
4. Check if `seo-landing-page.component.html` needs more semantic text sections or if the existing structure is enough.
5. Check if landing guide links should be visually grouped by search intent.
6. Run locale validation:
   - `cd FoodDiary.Web.Client && npm run build`
   - optionally `npm run lint`
7. Inspect prerendered pages or local SSR output for:
   - correct Cyrillic rendering
   - one H1 per page
   - no missing translation keys
   - FAQ content present in page source/structured data

## Risks and Notes

- The existing content is already SEO-oriented, so the rewrite should focus on clearer intent separation and less repetitive generic phrasing.
- Do not over-optimize with keyword stuffing. Each page should read naturally and answer a specific user need.
- Russian and English pages should be equivalent in intent, but not literal word-for-word translations.
- Avoid claims that imply medical treatment, guaranteed weight loss, or professional diagnosis.
