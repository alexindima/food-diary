import { DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    ApplicationRef,
    computed,
    DestroyRef,
    effect,
    inject,
    Injector,
    isDevMode,
    Service,
    untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { registerWebmcpTools, WebmcpError, WebmcpExecutor } from 'fd-webmcp';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import type {
    MealDevelopmentDraft,
    MealDevelopmentScreen,
    MealDevelopmentTools,
} from '../../features/meals/contracts/meal-development-tools';
import { AuthService } from '../../services/auth.service';
import { DevelopmentToolError } from '../../shared/development/development-tools';
import { entityId } from '../../shared/models/semantics/entity-id';
import { recipeServings } from '../../shared/models/semantics/meal-quantity';
import { localModelContext, type LocalTool } from './local-webmcp.types';
import { LocalWebmcpApplicationService } from './local-webmcp-application.service';
import { LocalWebmcpError } from './local-webmcp-error';
import { DAY_SCHEMA, DRAFT_SCHEMA, inputRecord, readDay, readDraft } from './local-webmcp-input';
import { LocalWebmcpRequestsService } from './local-webmcp-requests.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

const PRODUCT_SEARCH_LIMIT = 25;
type ToolOperation = Omit<LocalTool, 'execute'> & { run: (input: unknown) => unknown };

/** Composition adapter: existing navigation, catalog reads and the live editor. */
@Service()
export class LocalWebmcpPilotService implements MealDevelopmentTools {
    private readonly document = inject(DOCUMENT);
    private readonly injector = inject(Injector);
    private readonly router = inject(Router);
    private readonly auth = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly screens = inject(LocalWebmcpScreensService);
    private readonly application = inject(LocalWebmcpApplicationService);
    private readonly requests = inject(LocalWebmcpRequestsService);
    private screen: MealDevelopmentScreen | null = null;
    private readonly executor = new WebmcpExecutor();
    private started = false;
    private hasRegisteredSession = false;

    public initialize(): void {
        if (this.started) {
            return;
        }
        this.started = true;
        afterNextRender(() => {
            const context = localModelContext(this.document, isDevMode());
            if (context === undefined) {
                return;
            }
            const sessionUser = computed(() => (this.auth.isAuthenticated() ? this.auth.getUserId() : null));
            effect(
                onCleanup => {
                    const userId = sessionUser();
                    if (userId === null) {
                        return;
                    }
                    if (this.hasRegisteredSession) {
                        this.requests.clear();
                    }
                    this.hasRegisteredSession = true;
                    const tools = untracked(() => this.tools(userId));
                    onCleanup(registerWebmcpTools(context, tools));
                },
                { injector: this.injector },
            );
        });
        this.destroyRef.onDestroy(() => {
            this.screen = null;
        });
    }

    public attach(screen: MealDevelopmentScreen): () => void {
        this.screen = screen;
        const detach =
            screen.clearDraft === undefined
                ? undefined
                : this.screens.attach({ kind: 'meal-editor', read: screen.read, isDirty: screen.isDirty, clearDraft: screen.clearDraft });
        return () => {
            detach?.();
            if (this.screen === screen) {
                this.screen = null;
            }
        };
    }

    private tools(userId: string): LocalTool[] {
        const openDiaryAsync = async (input: unknown): Promise<unknown> => this.openDiaryAsync(input, userId);
        const fillDraftAsync = async (input: unknown): Promise<unknown> => this.fillDraftAsync(input, userId);
        return [
            this.tool(userId, {
                name: 'fooddiary_read_screen_context',
                description:
                    'Read live screen context, role/verification status, locale/timezone, configured API routes, list filters or unsaved editor values and validation. No credentials or arbitrary URL queries are returned.',
                inputSchema: { type: 'object', properties: {}, additionalProperties: false },
                annotations: { readOnlyHint: true },
                run: input => {
                    inputRecord(input, []);
                    return this.context();
                },
            }),
            this.tool(userId, {
                name: 'fooddiary_open_diary_date',
                description:
                    'Open the meal diary for one local calendar date. Changes navigation and filters only. Refuses to leave a dirty editor.',
                inputSchema: { type: 'object', properties: { date: DAY_SCHEMA }, required: ['date'], additionalProperties: false },
                annotations: { readOnlyHint: false },
                run: openDiaryAsync,
            }),
            this.tool(userId, {
                name: 'fooddiary_fill_meal_draft',
                description:
                    'Open and fill a NEW unsaved meal editor using exact existing product names/base units (G/ML/PCS), or exact accessible recipeId and servings. Owner converts recipe servings into editor display quantities. Never saves or calls AI. Refuses dirty editors.',
                inputSchema: DRAFT_SCHEMA,
                annotations: { readOnlyHint: false },
                run: fillDraftAsync,
            }),
            ...this.application
                .operations(
                    () => this.context(),
                    () => {
                        this.requireCleanScreen();
                    },
                    () => {
                        this.requireSession(userId);
                    },
                )
                .map(operation => this.tool(userId, operation)),
        ];
    }

    private tool(userId: string, operation: ToolOperation): LocalTool {
        return this.executor.tool(operation, {
            requireSession: () => {
                this.requireSession(userId);
            },
            errorMessage: error =>
                error instanceof LocalWebmcpError || error instanceof DevelopmentToolError || error instanceof WebmcpError
                    ? error.message
                    : 'The tool could not complete. Inspect the current screen and retry.',
            failureDescription: 'Failures return {ok:false,error}; inspect the result before continuing.',
        });
    }

    private async openDiaryAsync(input: unknown, userId: string): Promise<unknown> {
        const date = readDay(inputRecord(input, ['date'])['date']);
        this.requireCleanScreen();
        const targetUrl = `/meals?dateFrom=${date}&dateTo=${date}`;
        this.screens.prepareNavigation(this.document.querySelector('[role="dialog"]') !== null);
        if (this.router.url !== targetUrl && !(await this.router.navigate(['/meals'], { queryParams: { dateFrom: date, dateTo: date } }))) {
            throw new LocalWebmcpError('Diary navigation was cancelled.');
        }
        await this.injector.get(ApplicationRef).whenStable();
        this.requireSession(userId);
        return this.context();
    }

    private async fillDraftAsync(input: unknown, userId: string): Promise<unknown> {
        const draft = readDraft(input);
        this.requireCleanScreen();
        const initialUrl = this.router.url;
        const items = await this.resolveProductsAsync(draft);
        this.requireSession(userId);
        this.requireCleanScreen();
        if (initialUrl !== this.router.url) {
            throw new LocalWebmcpError('Screen changed while loading products. Retry from the current screen.');
        }
        await this.openDraftEditorAsync(draft.mealType);
        await this.injector.get(ApplicationRef).whenStable();
        this.requireSession(userId);
        const editor = this.screen;
        if (editor?.fillDraft === undefined || editor.isDirty?.() === true) {
            throw new LocalWebmcpError('A clean new meal editor is required.');
        }
        await editor.fillDraft({ ...draft, items }, () => {
            this.requireSession(userId);
        });
        this.requireSession(userId);
        return { saved: false, ...this.context() };
    }

    private async resolveProductsAsync(draft: ReturnType<typeof readDraft>): Promise<MealDevelopmentDraft['items']> {
        const { ProductService } = await import('../../features/products/api/product.service');
        const products = this.injector.get(ProductService);
        const items: MealDevelopmentDraft['items'] = [];
        for (const item of draft.items) {
            if ('recipeId' in item) {
                const { RecipeService } = await import('../../features/recipes/api/recipe.service');
                const recipe = await firstValueFrom(this.injector.get(RecipeService).getById(entityId<'recipe'>(item.recipeId)));
                if (recipe === null || !Number.isFinite(recipe.servings) || recipe.servings <= 0) {
                    throw new LocalWebmcpError('Recipe missing or unavailable.');
                }
                items.push({ recipe, servings: recipeServings(item.servings) });
                continue;
            }
            const page = await firstValueFrom(products.query(1, PRODUCT_SEARCH_LIMIT, { search: item.productName }));
            const matches = page.data.filter(product => product.name === item.productName);
            if (matches.length !== 1 || page.totalPages > 1) {
                throw new LocalWebmcpError('Product name is missing or ambiguous. Use an exact unique existing product name.');
            }
            const product = matches[0];
            if (String(product.baseUnit) !== item.unit) {
                throw new LocalWebmcpError('Product unit does not match. No unit conversion is performed.');
            }
            items.push({ product, amount: item.amount });
        }
        return items;
    }

    private async openDraftEditorAsync(mealType: string): Promise<void> {
        if (this.screen?.fillDraft !== undefined) {
            return;
        }
        this.screens.prepareNavigation(this.document.querySelector('[role="dialog"]') !== null);
        if (!(await this.router.navigate(['/meals/add'], { queryParams: { mealType } }))) {
            throw new LocalWebmcpError('Editor navigation was cancelled.');
        }
    }

    private context(): { path: string; screen: unknown; application: unknown } {
        // Arbitrary query parameters can contain authentication return values.
        return {
            path: this.router.url.split('?')[0].split('#')[0],
            screen: this.screens.read() ?? this.screen?.read() ?? null,
            application: {
                authenticated: this.auth.isAuthenticated(),
                roles: { admin: this.auth.isAdmin(), dietologist: this.auth.isDietologist() },
                emailConfirmed: this.auth.isEmailConfirmed(),
                locale: this.document.documentElement.lang,
                timeZone: new Intl.DateTimeFormat().resolvedOptions().timeZone,
                origin: this.document.location.origin,
                configuredApiUrls: Object.fromEntries(
                    Object.entries(environment.apiUrls).map(([key, value]) => {
                        const url = new URL(value, this.document.location.origin);
                        return [key, `${url.origin}${url.pathname}`];
                    }),
                ),
                buildRevision: environment.developmentBuild?.revision ?? null,
                frontendBuild: {
                    version: environment.buildVersion ?? null,
                    availability: environment.developmentBuild === undefined ? 'not-emitted' : 'emitted',
                    sourceCapture: environment.developmentBuild ?? null,
                },
                apiRuntimeObservations: this.requests
                    .read()
                    .filter(request => request.apiRuntime !== null)
                    .map(request => ({
                        requestId: request.id,
                        observedOrigin: request.origin,
                        ...request.apiRuntime,
                    })),
                requests: this.requests.read(),
            },
        };
    }

    private requireSession(userId: string): void {
        if (this.auth.getUserId() !== userId || !this.auth.isAuthenticated()) {
            throw new LocalWebmcpError('The authenticated session changed.');
        }
    }

    private requireCleanScreen(): void {
        if (
            this.screen === null &&
            this.screens.current() === undefined &&
            !['/', '/dashboard', '/meals', '/products', '/recipes'].includes(this.router.url.split('?')[0])
        ) {
            throw new LocalWebmcpError('This screen does not publish unsaved-change state. Navigate through the interface first.');
        }
        if (this.screen?.isDirty?.() === true || this.screens.isDirty()) {
            throw new LocalWebmcpError('Unsaved draft exists. Review, save or discard it in the interface first.');
        }
    }
}
