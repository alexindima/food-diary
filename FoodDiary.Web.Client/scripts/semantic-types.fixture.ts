import { of } from 'rxjs';

import {
    type CatalogRecipeServings,
    decodeCatalogIngredientSource,
} from '../projects/fooddiary-admin/src/app/features/admin-catalog/models/catalog-ingredient-source';
import {
    adminCalendarDate,
    type AdminId,
    adminId,
    adminUtcInstant,
} from '../projects/fooddiary-admin/src/app/shared/models/semantics/admin-meaning';
import type { CommentService } from '../src/app/features/explore/api/comment.service';
import type { ReportService } from '../src/app/features/explore/api/report.service';
import type { ExploreInteractionsFacade } from '../src/app/features/explore/lib/explore-interactions.facade';
import type { RecipeComment } from '../src/app/features/explore/models/comment.data';
import type { ObservedReportTarget, ReportTarget } from '../src/app/features/explore/models/report.data';
import type { LessonService } from '../src/app/features/lessons/api/lesson.service';
import type { MealPlanService } from '../src/app/features/meal-plans/api/meal-plan.service';
import type { MealPlanMeal } from '../src/app/features/meal-plans/models/meal-plan.data';
import {
    planDayNumber,
    planDurationDays,
    plannedMealTypeFromStored,
    plannedServings,
} from '../src/app/features/meal-plans/models/meal-plan-values';
import type { MealActions } from '../src/app/features/meals/contracts/meal-actions';
import type { MealFavoritesPickerFacade } from '../src/app/features/meals/lib/favorites/meal-favorites-picker.facade';
import type { PublicProductService } from '../src/app/features/products/api/public-product.service';
import type { ProductFavoritesPickerFacade } from '../src/app/features/products/lib/favorites/product-favorites-picker.facade';
import type { ActiveSessionsService } from '../src/app/features/profile/api/active-sessions.service';
import type { ActiveSessionsFacade } from '../src/app/features/profile/lib/active-sessions.facade';
import type { SessionRevocation } from '../src/app/features/profile/models/active-session.model';
import type { PublicRecipeService } from '../src/app/features/recipes/api/public-recipe.service';
import type { RecipeFavoritesPickerFacade } from '../src/app/features/recipes/lib/favorites/recipe-favorites-picker.facade';
import type { ShoppingListFacade } from '../src/app/features/shopping-lists/lib/shopping-list.facade';
import type { ShoppingListItemsPanelComponent } from '../src/app/features/shopping-lists/pages/shopping-list-items-panel/shopping-list-items-panel';
import type { WeeklyCheckInService } from '../src/app/features/weekly-check-in/api/weekly-check-in.service';
import type { WeeklyGoalService } from '../src/app/features/weekly-check-in/api/weekly-goal.service';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../src/app/features/weekly-check-in/models/weekly-goal.data';
import type { WeightEntriesService } from '../src/app/features/weight-history/api/weight-entries.service';
import type { FoodRecognitionService } from '../src/app/shared/api/food-recognition.service';
import type { RecipeLookupService } from '../src/app/shared/api/recipe-lookup.service';
import { anonymousVisitorId, type MarketingAttributionIdentity, marketingSessionId } from '../src/app/shared/marketing/marketing-identity';
import { MeasurementHistoryPager } from '../src/app/shared/measurements/measurement-history-pager';
import type { FoodVisionRequest } from '../src/app/shared/models/ai.data';
import type { UpdateMenstrualEpisodePayload, UpsertCycleDayPayload, UpsertCycleFactorPayload } from '../src/app/shared/models/cycle.data';
import type { FoodRecognitionJob } from '../src/app/shared/models/food-recognition.data';
import type { ImageSelection } from '../src/app/shared/models/image-upload.data';
import { MealSourceType, type ProductMealItem, type RecipeMealItem } from '../src/app/shared/models/meal.data';
import type { NotificationItem } from '../src/app/shared/models/notification.data';
import {
    type CalendarDate,
    calendarDate,
    optionalCalendarDate,
    type UtcInstant,
    utcInstant,
} from '../src/app/shared/models/semantics/date-value';
import { entityId, type MealId, optionalEntityId, type ProductId } from '../src/app/shared/models/semantics/entity-id';
import { publicImageUrl, signedImageUploadUrl } from '../src/app/shared/models/semantics/image-location';
import {
    type ProductQuantity,
    productQuantity,
    type RecipeServings,
    recipeServings,
} from '../src/app/shared/models/semantics/meal-quantity';
import type { User, WeightGoalHistoryItem } from '../src/app/shared/models/user.data';
import type { NotificationService } from '../src/app/shared/notifications/notification.service';

const PRODUCT_UNITS = 100;
const COMMUNITY_PAGE_SIZE = 10;

export function verifyMeaning(actions: MealActions): void {
    const meal: MealId = entityId<'meal'>('placeholder');
    const product: ProductId = entityId<'product'>('placeholder');
    const day: CalendarDate = calendarDate('2026-10-08');
    const instant: UtcInstant = utcInstant('2026-10-08T12:00:00.1234567Z');
    const grams: ProductQuantity = productQuantity(PRODUCT_UNITS);
    const servings: RecipeServings = recipeServings(2);
    const absentId: MealId | undefined = optionalEntityId<'meal'>(undefined);
    const absentDay: null = optionalCalendarDate(null);
    void absentId;
    void absentDay;
    actions.repeat(meal, instant, 'Lunch');
    // @ts-expect-error A product identity must not reach a meal mutation.
    actions.deleteById(product);
    // @ts-expect-error Calendar days must not be passed as meal timestamps.
    actions.repeat(meal, day, 'Lunch');
    // @ts-expect-error Rebranding an already-typed foreign ID is forbidden.
    entityId<'meal'>(product);
    // @ts-expect-error Rebranding an instant as a day is forbidden.
    calendarDate(instant);
    // @ts-expect-error Entity identities cannot be passed to a timestamp factory.
    utcInstant(product);
    // @ts-expect-error Calendar days cannot be passed to an identity factory.
    entityId<'meal'>(day);
    // @ts-expect-error Recipe servings are not product units.
    productQuantity(servings);
    // @ts-expect-error Product units are not recipe servings.
    recipeServings(grams);
    const identity = { id: entityId<'meal-item'>('item'), mealId: meal };
    const wrongProduct: ProductMealItem = {
        ...identity,
        sourceType: MealSourceType.Product,
        // @ts-expect-error The product branch requires ProductQuantity.
        amount: servings,
        product: null,
        recipe: null,
    };
    // @ts-expect-error The recipe branch requires RecipeServings.
    const wrongRecipe: RecipeMealItem = { ...identity, sourceType: MealSourceType.Recipe, amount: grams, product: null, recipe: null };
    void wrongProduct;
    void wrongRecipe;
    const profile: Pick<User, 'id' | 'birthDate'> = { id: entityId<'user'>('user'), birthDate: day };
    // @ts-expect-error Birth dates use a calendar meaning.
    profile.birthDate = instant;
    // @ts-expect-error A meal identity cannot address a user profile.
    profile.id = meal;
    const goal: Pick<WeightGoalHistoryItem, 'id' | 'startedAtUtc'> = { id: entityId<'weight-goal'>('goal'), startedAtUtc: instant };
    // @ts-expect-error Weight goal history is independent from a weight measurement.
    goal.id = entityId<'weight-entry'>('entry');
    // @ts-expect-error Goal history timestamps are instants.
    goal.startedAtUtc = day;
    void profile;
    void goal;
    const location = publicImageUrl('relative/photo');
    const upload = signedImageUploadUrl('https://storage.example/photo?sig=encoded');
    // @ts-expect-error A signed upload target cannot become a public image location.
    publicImageUrl(upload);
    // @ts-expect-error Public image locations cannot address an upload request.
    signedImageUploadUrl(location);
    // @ts-expect-error Uploaded selections require an image asset identity.
    const wrongSelection: ImageSelection = { kind: 'uploaded', url: location, assetId: null };
    // @ts-expect-error An image asset cannot use a product identity.
    const wrongAsset: ImageSelection = { kind: 'uploaded', url: location, assetId: product };
    const adminUser: AdminId<'user'> = adminId<'user'>('placeholder');
    // @ts-expect-error Admin user identities cannot address lessons.
    const adminLesson: AdminId<'lesson'> = adminUser;
    // @ts-expect-error The admin factory rejects rebranding a different owner.
    adminId<'lesson'>(adminUser);
    // @ts-expect-error Admin dates cannot be rebranded as instants.
    adminUtcInstant(adminCalendarDate('2026-10-08'));
    const catalogSource = decodeCatalogIngredientSource({ productId: 'product', nestedRecipeId: null, amount: 100 });
    if (catalogSource.kind === 'product') {
        // @ts-expect-error Catalog product amounts cannot stand in for recipe servings.
        const catalogServings: CatalogRecipeServings = catalogSource.amount;
        void catalogServings;
    }
    void wrongSelection;
    void wrongAsset;
    void adminLesson;
}

function cycleMutationMeanings(): void {
    const day = calendarDate('2026-04-12');
    const instant = utcInstant('2026-04-12T22:30:00Z');
    const factor = entityId<'cycle-factor'>('factor');
    const episode = entityId<'menstrual-episode'>('episode');
    const dayPayload: UpsertCycleDayPayload = { date: day, symptoms: [] };
    const factorPayload: UpsertCycleFactorPayload = { factorId: factor, type: 0, startDate: day, endDate: null, clearNotes: false };
    const episodePayload: UpdateMenstrualEpisodePayload = { startDate: day };
    // @ts-expect-error A cycle day is a selected calendar date, not an instant.
    dayPayload.date = instant;
    // @ts-expect-error A factor cannot be identified by a menstrual episode.
    factorPayload.factorId = episode;
    // @ts-expect-error A factor start is not a UTC instant.
    factorPayload.startDate = instant;
    // @ts-expect-error An optional factor end still needs calendar meaning.
    factorPayload.endDate = instant;
    // @ts-expect-error An episode start cannot be an instant.
    episodePayload.startDate = instant;
    // @ts-expect-error An episode end cannot be an instant.
    episodePayload.endDate = instant;
    void dayPayload;
    void factorPayload;
    void episodePayload;
}
void cycleMutationMeanings;

function notificationMeanings(service: NotificationService, notification: NotificationItem): void {
    service.markAsRead(notification.id);
    // @ts-expect-error A user cannot address notification mutation.
    service.markAsRead(entityId<'user'>('user'));
    // @ts-expect-error Notification creation is an instant, not a calendar date.
    notification.createdAtUtc = calendarDate('2026-04-12');
    // @ts-expect-error Raw strings must be decoded before notification classification.
    notification.type = 'NewRecommendation';
}
void notificationMeanings;

const FRACTIONAL_CONSUMED_SERVINGS = 0.5;
const SEMANTIC_PLAN_DURATION = 7;

function remainingFeatureMeanings(
    plans: MealPlanService,
    lessons: LessonService,
    recognition: FoodRecognitionService,
    data: { meal: MealPlanMeal; job: FoodRecognitionJob },
): void {
    const { meal, job } = data;
    const plan = entityId<'meal-plan'>('plan');
    const day = entityId<'meal-plan-day'>('day');
    const plannedMeal = entityId<'meal-plan-meal'>('planned-meal');
    const lesson = entityId<'lesson'>('lesson');
    const task = entityId<'food-recognition'>('task');
    const image = entityId<'image-asset'>('image');
    plans.getById(plan);
    plans.adopt(plan);
    plans.deletePlan(plan);
    plans.generateShoppingList(plan);
    lessons.getById(lesson);
    lessons.markRead(lesson);
    recognition.resume(task);
    recognition.deleteRecognition(task);
    const request: FoodVisionRequest = { imageAssetId: image, additionalImageAssetIds: [image] };
    // @ts-expect-error A lesson cannot address a plan.
    plans.getById(lesson);
    // @ts-expect-error A day cannot be adopted as a plan.
    plans.adopt(day);
    // @ts-expect-error A planned meal cannot address deletion of its plan.
    plans.deletePlan(plannedMeal);
    // @ts-expect-error A recipe cannot address generation for a plan.
    plans.generateShoppingList(entityId<'recipe'>('recipe'));
    // @ts-expect-error A plan cannot be marked as a read lesson.
    lessons.markRead(plan);
    // @ts-expect-error A lesson cannot address a recognition task.
    recognition.resume(lesson);
    // @ts-expect-error An image cannot address deletion of recognition work.
    recognition.deleteRecognition(image);
    // @ts-expect-error A task is not the input image asset.
    request.imageAssetId = task;
    // @ts-expect-error Every additional image needs the asset role.
    request.additionalImageAssetIds = [task];
    // @ts-expect-error Planned meals have their own identity, distinct from diary meals.
    meal.id = entityId<'meal'>('diary-meal');
    // @ts-expect-error Job creation is an instant, not a calendar date.
    job.createdOnUtc = calendarDate('2026-10-09');
    // @ts-expect-error A signed upload target is not a displayed recognition photo.
    job.imageUrl = signedImageUploadUrl('https://storage.example/upload');
    const servings = plannedServings(2);
    const consumed = recipeServings(FRACTIONAL_CONSUMED_SERVINGS);
    // @ts-expect-error Planned integer servings cannot become a plan duration.
    planDurationDays(servings);
    // @ts-expect-error Plan duration cannot become a day ordinal.
    planDayNumber(planDurationDays(SEMANTIC_PLAN_DURATION));
    // @ts-expect-error Consumed servings cannot be retagged as planned integral servings.
    plannedServings(consumed);
    // @ts-expect-error Planned servings need an explicit conversion into consumed servings.
    recipeServings(servings);
    // @ts-expect-error A plan ID is not a meal type code.
    plannedMealTypeFromStored(plan);
    void request;
}
void remainingFeatureMeanings;

function attributionIdentityMeanings(): void {
    const visitor = anonymousVisitorId('opaque/visitor');
    const session = marketingSessionId('opaque:session');
    const identity: MarketingAttributionIdentity = { anonymousId: visitor, sessionId: session };
    // @ts-expect-error Session IDs cannot stand in for a persistent visitor.
    identity.anonymousId = session;
    // @ts-expect-error Visitor IDs cannot stand in for a session.
    identity.sessionId = visitor;
    // @ts-expect-error Factories do not silently retag an existing session ID.
    anonymousVisitorId(session);
    // @ts-expect-error Factories do not silently retag an existing visitor ID.
    marketingSessionId(visitor);
    // @ts-expect-error Account identity is distinct from an anonymous visitor.
    identity.anonymousId = entityId<'user'>('user');
    void identity;
}
void attributionIdentityMeanings;

function communityAndWeeklyMeanings(
    {
        comments,
        interactions,
        reports,
        goals,
        weeks,
    }: {
        comments: CommentService;
        interactions: ExploreInteractionsFacade;
        reports: ReportService;
        goals: WeeklyGoalService;
        weeks: WeeklyCheckInService;
    },
    { comment, goal, payload }: { comment: RecipeComment; goal: WeeklyGoal; payload: UpsertWeeklyGoalPayload },
): void {
    const recipe = entityId<'recipe'>('recipe');
    const commentId = entityId<'recipe-comment'>('comment');
    const day = calendarDate('2026-10-05');
    const instant = utcInstant('2026-10-05T00:00:00Z');
    comments.updateComment(recipe, commentId, { text: 'Updated' });
    interactions.deleteComment(recipe, commentId);
    reports.create({ target: { kind: 'recipe', recipeId: recipe }, reason: 'Spam' });
    reports.create({ target: { kind: 'comment', commentId }, reason: 'Spam' });
    goals.getGoal(day);
    weeks.getData(day);
    // @ts-expect-error A comment cannot address its parent recipe.
    comments.getComments(commentId, 1, COMMUNITY_PAGE_SIZE);
    // @ts-expect-error A recipe cannot address a comment mutation.
    comments.updateComment(recipe, recipe, { text: 'Updated' });
    // @ts-expect-error A facade cannot swap recipe and comment roles.
    interactions.deleteComment(commentId, recipe);
    // @ts-expect-error Raw IDs must be decoded before invoking a comment mutation.
    comments.deleteComment('recipe', 'comment');
    // @ts-expect-error Recipe targets cannot contain comment IDs.
    const wrongRecipe: ReportTarget = { kind: 'recipe', recipeId: commentId };
    // @ts-expect-error Comment targets cannot contain recipe IDs.
    const wrongComment: ReportTarget = { kind: 'comment', commentId: recipe };
    const observed: ObservedReportTarget = { kind: 'unknown', targetType: 'FutureTarget', targetId: 'opaque' };
    // @ts-expect-error Unknown response observations cannot become supported mutation targets.
    reports.create({ target: observed, reason: 'Spam' });
    // @ts-expect-error A comment author needs an account identity.
    comment.authorId = recipe;
    // @ts-expect-error Comment timestamps are instants, not calendar days.
    comment.createdAtUtc = day;
    // @ts-expect-error Weekly goal lookup requires a calendar date, not an instant.
    goals.getGoal(instant);
    // @ts-expect-error Weekly check-in shares the calendar date contract.
    weeks.getData(instant);
    // @ts-expect-error Raw strings must be decoded before a weekly goal lookup.
    goals.getGoal('2026-10-05');
    // @ts-expect-error Weekly goal read models retain a calendar date.
    goal.weekStart = instant;
    // @ts-expect-error Weekly goal mutation payloads retain a calendar date.
    payload.weekStart = instant;
    void wrongRecipe;
    void wrongComment;
}
void communityAndWeeklyMeanings;

function actionAndLookupMeanings(ports: {
    shopping: ShoppingListFacade;
    panel: ShoppingListItemsPanelComponent;
    sessions: ActiveSessionsService;
    sessionState: ActiveSessionsFacade;
    recipeFavorites: RecipeFavoritesPickerFacade;
    productFavorites: ProductFavoritesPickerFacade;
    mealFavorites: MealFavoritesPickerFacade;
    recipes: RecipeLookupService;
    products: PublicProductService;
    publicRecipes: PublicRecipeService;
    weights: WeightEntriesService;
}): void {
    const list = entityId<'shopping-list'>('list');
    const item = entityId<'shopping-list-item'>('item');
    const recipe = entityId<'recipe'>('recipe');
    const product = entityId<'product'>('product');
    const session = entityId<'refresh-token-session'>('session');
    const day = calendarDate('2026-10-09');
    const instant = utcInstant('2026-10-09T12:00:00Z');
    ports.shopping.removePurchased(list, [item]);
    ports.panel.itemCheckedChange.emit({ itemId: item, checked: true });
    ports.sessions.revoke(session);
    ports.sessionState.revoke(session);
    ports.recipes.getById(recipe);
    ports.products.getById(product);
    ports.publicRecipes.getById(recipe);
    ports.weights.getHistoryPage(day);
    // @ts-expect-error A shopping item cannot address its list or use a list ID as an item.
    ports.shopping.removePurchased(item, [list]);
    // @ts-expect-error A recipe cannot address removal of a shopping item.
    ports.shopping.removeItem(recipe);
    // @ts-expect-error UI events preserve the shopping item role.
    ports.panel.itemCheckedChange.emit({ itemId: list, checked: true });
    // @ts-expect-error A product cannot address a refresh session.
    ports.sessions.revoke(product);
    // @ts-expect-error The session facade requires the same session owner meaning.
    ports.sessionState.revoke(recipe);
    // @ts-expect-error Revoke-others does not carry a single-session identity.
    const invalidOthers: SessionRevocation = { kind: 'others', sessionId: session };
    // @ts-expect-error A single-session state requires a refresh-session identity.
    const invalidSingle: SessionRevocation = { kind: 'single', sessionId: recipe };
    // @ts-expect-error A source recipe ID is distinct from the favorite row ID.
    ports.recipeFavorites.markRestored(recipe);
    // @ts-expect-error Product favorite IDs cannot address recipe favorite state.
    ports.recipeFavorites.markRemoved(entityId<'favorite-product'>('favorite'));
    // @ts-expect-error Recipe favorite IDs cannot address product favorite state.
    ports.productFavorites.markRemoved(entityId<'favorite-recipe'>('favorite'));
    // @ts-expect-error Diary meal IDs cannot address meal favorite state.
    ports.mealFavorites.markRemoved(entityId<'meal'>('meal'));
    // @ts-expect-error The recipe lookup does not accept product IDs.
    ports.recipes.getById(product);
    // @ts-expect-error Public product reads do not accept recipe IDs.
    ports.products.getById(recipe);
    // @ts-expect-error Public recipe reads do not accept product IDs.
    ports.publicRecipes.getById(product);
    // @ts-expect-error A history cursor is a calendar day, not an instant.
    ports.weights.getHistoryPage(instant);
    // @ts-expect-error Instant-based rows cannot become measurement calendar rows.
    const invalidRows = new MeasurementHistoryPager(() => of([{ id: 'meal', date: instant }]));
    void invalidOthers;
    void invalidSingle;
    void invalidRows;
}
void actionAndLookupMeanings;
