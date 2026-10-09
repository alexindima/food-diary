/* eslint-disable no-redeclare -- optional factory overloads preserve the caller's null versus undefined contract */
/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- these constructors attach a phantom meaning without changing scalar wire values */
import type { SemanticString, UnbrandedString } from './string-meaning';

export type EntityId<Kind extends string> = SemanticString<`entity-id:${Kind}`>;
export type MealId = EntityId<'meal'>;
export type MealItemId = EntityId<'meal-item'>;
export type ProductId = EntityId<'product'>;
export type RecipeId = EntityId<'recipe'>;
export type UserId = EntityId<'user'>;
export type NotificationId = EntityId<'notification'>;
export type MealPlanId = EntityId<'meal-plan'>;
export type MealPlanDayId = EntityId<'meal-plan-day'>;
export type MealPlanMealId = EntityId<'meal-plan-meal'>;
export type LessonId = EntityId<'lesson'>;
export type FoodRecognitionId = EntityId<'food-recognition'>;
export type FavoriteMealId = EntityId<'favorite-meal'>;
export type FavoriteProductId = EntityId<'favorite-product'>;
export type FavoriteRecipeId = EntityId<'favorite-recipe'>;
export type ImageAssetId = EntityId<'image-asset'>;
export type WeightEntryId = EntityId<'weight-entry'>;
export type WaistEntryId = EntityId<'waist-entry'>;
export type HydrationEntryId = EntityId<'hydration-entry'>;
export type ShoppingListId = EntityId<'shopping-list'>;
export type ShoppingListItemId = EntityId<'shopping-list-item'>;
export type FastingSessionId = EntityId<'fasting-session'>;

/** Transport and local placeholders retain their existing strings; this marks ownership. */
type UnbrandedId = UnbrandedString;

export function entityId<Kind extends string>(value: UnbrandedId | EntityId<Kind>): EntityId<Kind> {
    return value as EntityId<Kind>;
}

export function optionalEntityId<Kind extends string>(value: UnbrandedId | EntityId<Kind> | null): EntityId<Kind> | null;
export function optionalEntityId<Kind extends string>(value: UnbrandedId | EntityId<Kind> | undefined): EntityId<Kind> | undefined;
export function optionalEntityId<Kind extends string>(
    value: UnbrandedId | EntityId<Kind> | null | undefined,
): EntityId<Kind> | null | undefined;
export function optionalEntityId<Kind extends string>(
    value: UnbrandedId | EntityId<Kind> | null | undefined,
): EntityId<Kind> | null | undefined {
    return value === null || value === undefined ? value : entityId<Kind>(value);
}

export type WeightGoalId = EntityId<'weight-goal'>;
export type WaistGoalId = EntityId<'waist-goal'>;
export type RecommendationId = EntityId<'recommendation'>;
export type RecommendationCommentId = EntityId<'recommendation-comment'>;
export type ClientTaskId = EntityId<'client-task'>;
export type RecommendationTemplateId = EntityId<'recommendation-template'>;
export type AttentionSignalId = EntityId<'attention-signal'>;
export type DietologistInvitationId = EntityId<'dietologist-invitation'>;
export type CycleProfileId = EntityId<'cycle-profile'>;
export type BleedingEntryId = EntityId<'bleeding-entry'>;
export type CycleSymptomEntryId = EntityId<'cycle-symptom-entry'>;
export type CycleFactorId = EntityId<'cycle-factor'>;
export type FertilitySignalId = EntityId<'fertility-signal'>;
export type MenstrualEpisodeId = EntityId<'menstrual-episode'>;
export type CycleConsentId = EntityId<'cycle-consent'>;
export type CyclePredictionRevisionId = EntityId<'cycle-prediction-revision'>;
