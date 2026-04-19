# CSS Variable Audit

Аудит по переопределениям `--fd-*` в `FoodDiary.Web.Client/src`.

Цель:
- отделить нормальный public CSS API компонентов от legacy/page-level overrides;
- найти переопределения, которые повторяют текущий `fd-ui-kit` или отличаются незначительно;
- дать удобный чеклист для ручной визуальной проверки через DevTools.

## Как использовать

1. Открой нужную страницу.
2. В DevTools найди блок, указанный в таблице.
3. Временно отключи перечисленные `--fd-*` переопределения.
4. Если визуально всё ок, поставь `OK remove`.
5. Если ломается только стиль, но паттерн повторяется в нескольких местах, ставь `extract appearance`.
6. Если без переменных поведение действительно ухудшается, ставь `keep`.

Статусы:
- `todo`
- `ok remove`
- `extract appearance`
- `keep`

## Быстрый вывод

По быстрому срезу основная зона шума сейчас не в theme tokens, а в page-level `--fd-button-*` overrides.

Топ переопределяемых переменных в app-слое:

| Variable | Count |
| --- | ---: |
| `--fd-button-padding-x` | 24 |
| `--fd-button-background-default` | 23 |
| `--fd-button-background-hover` | 22 |
| `--fd-button-background-active` | 20 |
| `--fd-button-text-default` | 15 |
| `--fd-button-shadow-default` | 14 |
| `--fd-button-shadow-hover` | 14 |
| `--fd-button-border-default` | 14 |
| `--fd-button-border-radius` | 13 |
| `--fd-button-padding-y` | 12 |

Практический вывод:
- сначала стоит чистить кнопки;
- потом `fd-ui-card`/layout overrides;
- компонентные API вроде `image-upload`, `media-card`, `notice-banner` пока не трогать.

## Кандидаты На Упрощение

| Priority | Area | File | Variables | Hypothesis | Manual check | Status |
| --- | --- | --- | --- | --- | --- | --- |
| High | Public hero CTA | `src/app/features/public/components/hero/hero.component.scss` | `--fd-button-background-*`, `--fd-button-text-*`, `--fd-button-shadow-*`, `--fd-button-transform-*`, `--fd-button-transition` | Похоже на отдельный визуальный стиль CTA. Не candidate на удаление вслепую, но хороший candidate на новый shared `appearance`, чтобы не держать полную ручную раскладку прямо в странице. | Отключить сначала у secondary CTA, потом у primary CTA. Если останется приемлемо, можно упростить до `variant`/`appearance`; если нет, вынести в UI kit. | `todo` |
| High | Auth submit button | `src/app/features/auth/components/auth/auth.component.scss` | `--fd-button-height`, `--fd-button-border-radius`, `--fd-button-text-*`, `--fd-button-background-*`, `--fd-button-shadow-*`, `--fd-button-transform-*`, `--fd-button-disabled-opacity` | Очень похоже на старый локальный brand button. Большая часть может уйти в shared appearance для auth/hero CTA. | Отключить сначала `shadow/transform`, потом `background/text`, потом `radius`. Проверить login/register/disabled. | `todo` |
| High | Unsaved changes dialog actions | `src/app/components/shared/unsaved-changes-dialog/unsaved-changes-dialog.component.scss` | `--fd-button-height`, `--fd-button-padding-x`, `--fd-button-border-radius`, `--fd-button-background-*`, `--fd-button-border-*`, `--fd-button-text-*`, `--fd-button-shadow-*`, `--fd-button-transform-*` | Похоже на candidate для `fd-ui-button` dialog appearance. Сейчас слишком много низкоуровневых ручек для двух типовых кнопок. | Отключить у secondary action, затем у primary. Если остаётся читабельно, можно сильно сократить. | `todo` |
| High | Dashboard actions | `src/app/features/dashboard/pages/dashboard.component.scss` | `--fd-button-padding-*`, `--fd-button-border-radius`, `--fd-button-label-display`, `--fd-button-background-*`, `--fd-button-border-*`, `--fd-button-text-*`, `--fd-button-shadow-*`, `--fd-button-position`, `--fd-button-z-index`, `--fd-button-pointer-events` | Здесь смесь нормальных responsive tweaks и явного визуального ручного оркестра. Удалять всё сразу нельзя. Нужно отделить layout-only overrides от visual overrides. | Не трогать сразу `label-display`, `pointer-events`, `position`, `z-index`. Сначала проверить визуальные `background/border/text/shadow`. | `todo` |
| High | Manage screens primary actions | `src/app/features/products/components/manage/base-product-manage.component.scss` | `--fd-button-border-radius`, `--fd-button-background-*`, `--fd-button-shadow-*` | Похоже на повторяемый паттерн primary action в manage-экранах. Вероятно просится в shared `appearance`. | Отключить визуальные overrides на product manage и сравнить с recipe/meal manage. | `todo` |
| High | Manage screens primary actions | `src/app/features/recipes/components/manage/recipe-manage.component.scss` | `--fd-button-border-radius`, `--fd-button-background-*`, `--fd-button-shadow-*` | То же, что выше. Если совпадает визуально с product/meal manage, это почти точно shared preset. | Проверить вместе с product manage. | `todo` |
| High | Manage screens primary actions | `src/app/features/meals/components/manage/base-meal-manage.component.scss` | `--fd-button-border-radius`, `--fd-button-background-*`, `--fd-button-shadow-*` | То же семейство ручных brand-action кнопок. | Проверить вместе с recipe/product manage. | `todo` |
| Medium | Shopping list icon buttons | `src/app/features/shopping-lists/pages/shopping-list-page.component.scss` | `--fd-button-background-*`, `--fd-button-shadow-*`, `--fd-button-padding-*` | Похоже на локальный дубль toolbar/primary-toolbar поведения. Есть шанс, что после небольшой доработки `appearance="toolbar"` почти всё уйдёт. | Отключить сначала `background/shadow` у `icon-button--primary`, потом `padding` у new/delete/clear. | `todo` |
| Medium | Hydration card add button | `src/app/features/dashboard/components/hydration-card/hydration-card.component.scss` | `--fd-button-border-radius`, `--fd-button-padding-x`, `--fd-button-background-*`, `--fd-button-text-*`, `--fd-button-height`, `--fd-button-shadow-*` | Это уже не просто “обычная кнопка”, а полупрозрачная pill-кнопка внутри карточки. Скорее `keep` или отдельный appearance, чем удаление. | Проверить, не достаточно ли только `padding`/`height`, а остальное можно вынести в appearance. | `todo` |
| Medium | Recipe/Product list active filter state | `src/app/features/recipes/pages/list/recipe-list.component.scss` and `src/app/features/products/components/list/product-list-base.component.scss` | `--fd-button-border-*`, `--fd-button-background-*`, `--fd-button-text-*`, `--fd-button-padding-y`, `--fd-button-label-display`, `--fd-button-width` | Тут есть и нормальный active state, и responsive tweaks. Active state можно оставить, но responsive width/label rules возможно стоит оформить через shared toolbar behavior. | Не трогать active colors сразу. Проверить только `width/padding/label-display` на mobile/desktop-sm. | `todo` |
| Low | Manage header buttons | `src/app/components/shared/manage-header/manage-header.component.scss` | `--fd-button-text-*` | Небольшой, осмысленный override. Вряд ли главный источник сложности. | Проверить вместе с общим visual pass, но низкий приоритет. | `todo` |

## Скорее Оставить Как Component API

Эти переменные выглядят не как мусор, а как сознательный public API конкретного компонента.

| Area | File | Why keep for now |
| --- | --- | --- |
| Image upload | `src/app/components/shared/image-upload-field/image-upload-field.component.scss` | Компонент сильно параметризуется по размеру, preview behavior, hint visibility и mobile layout. Это похожо на осмысленный component contract. |
| Media card | `src/app/components/shared/media-card/media-card.component.scss` | Набор переменных описывает layout и spacing самой карточки, а не случайные page hacks. |
| Entity card | `src/app/components/shared/entity-card/entity-card.component.scss` | Переопределения связаны с media width, badges и action sizing. Это больше похоже на внутренний reusable surface area. |
| Notice banner | `src/app/components/shared/notice-banner/notice-banner.component.scss` | Цветовые переменные выглядят как намеренная семантическая тема banner-состояний. |
| Badge | `src/app/components/shared/badge/badge.component.scss` | Очень маленький и понятный color API. |

## Подозрительные Паттерны

Это места, где CSS variables уже выглядят как обход отсутствующего shared API:

| Pattern | Why suspicious |
| --- | --- |
| Полные наборы `background/text/border/shadow/transform` на одной кнопке | Обычно это значит, что страница рисует свой собственный appearance вместо использования design-system preset. |
| Повтор одинаковых gradient + shadow наборов в `auth`, `hero`, `manage`, `shopping-list` | Вероятный candidate на 1-2 новых `appearance` в `fd-ui-kit`. |
| Локальные `--fd-button-border-radius` | После недавней чистки часть радиусов уже должна жить через tokens. Остатки надо пересмотреть особенно внимательно. |
| Ручные `--fd-button-position`, `--fd-button-z-index`, `--fd-button-pointer-events` | Иногда это legit, но это уже очень низкий уровень API. Нужны только если реально нельзя решить контейнером или состоянием компонента. |

## Предлагаемый Порядок Чистки

1. Кнопки в `auth`, `hero`, `unsaved-changes-dialog`.
2. Повторяемые primary actions в `product/recipe/meal manage`.
3. Dashboard button overrides: сначала visual, потом layout-only.
4. Shopping list buttons.
5. Только после этого смотреть на `fd-ui-card-*` и layout variables.

## Рабочий Шаблон Для Ручной Проверки

Можно использовать такой комментарий прямо в задаче или PR:

```md
- Screen:
- File:
- Variables disabled:
- Visual result:
- Decision: keep / ok remove / extract appearance
- Notes:
```

## Итоговая Гипотеза

Наиболее вероятный выигрыш даст не удаление всех CSS variables вообще, а такой рефакторинг:

1. сократить page-level `--fd-button-*`;
2. вынести повторяющиеся button presets в `fd-ui-kit`;
3. оставить компонентные CSS API там, где они реально описывают reusable behavior.
