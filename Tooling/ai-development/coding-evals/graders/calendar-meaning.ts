import type { MealActions } from "@candidate/app/features/meals/contracts/meal-actions";
import {
    calendarDate,
    utcInstant,
} from "@candidate/app/shared/models/semantics/date-value";
import { entityId } from "@candidate/app/shared/models/semantics/entity-id";

declare const meals: MealActions;
const meal = entityId<"meal">("owned");
const day = calendarDate("2026-10-08");
const instant = utcInstant("2026-10-08T23:59:59.1234567Z");
meals.repeat(meal, instant, "Lunch");
// @ts-expect-error A calendar date is not an instant for repeating a meal.
meals.repeat(meal, day, "Lunch");
// @ts-expect-error Constructors must not erase an existing time meaning.
calendarDate(instant);
// @ts-expect-error Identity cannot become a timestamp by branding.
utcInstant(meal);
