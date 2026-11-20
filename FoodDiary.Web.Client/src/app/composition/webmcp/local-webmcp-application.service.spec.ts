import { ChangeDetectionStrategy, Component, DestroyRef, inject, type Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, NavigationEnd, provideRouter, Router, type Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeDevelopmentDraft } from '../../features/recipes/contracts/recipe-development-draft';
import { DevelopmentToolError } from '../../shared/development/development-tools';
import { LocalWebmcpApplicationService } from './local-webmcp-application.service';
import { LocalWebmcpCatalogService } from './local-webmcp-catalog.service';
import { LocalWebmcpPushService } from './local-webmcp-push.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class LocalRouteFixture {}

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class LocalDraftRouteFixture {
    public constructor() {
        const kind: unknown = inject(ActivatedRoute).snapshot.data['kind'];
        if (typeof kind !== 'string') {
            throw new Error('Expected a route with a published draft kind.');
        }
        let opened = kind === 'product-editor' || kind === 'recipe-editor';
        const detach = inject(LocalWebmcpScreensService).attach({
            kind,
            read: () => ({ opened }),
            openDraft: () => {
                opened = true;
            },
        });
        inject(DestroyRef).onDestroy(detach);
    }
}

function setup(
    routes: Routes = [],
    providers: Provider[] = [],
): {
    callAsync: (name: string, input: unknown) => Promise<unknown>;
    requireSession: ReturnType<typeof vi.fn<() => void>>;
    router: Router;
    screens: LocalWebmcpScreensService;
} {
    TestBed.configureTestingModule({ providers: [provideRouter(routes), ...providers] });
    const app = TestBed.inject(LocalWebmcpApplicationService);
    const screens = TestBed.inject(LocalWebmcpScreensService);
    const router = TestBed.inject(Router);
    const requireSession = vi.fn<() => void>();
    const operations = app.operations(
        () => ({ route: router.url, screen: screens.read() }),
        () => {
            if (screens.isDirty()) {
                throw new DevelopmentToolError('Unsaved draft exists.');
            }
        },
        requireSession,
    );
    return {
        callAsync: async (name, input): Promise<unknown> => {
            const operation = operations.find(item => item.name === `fooddiary_${name}`);
            if (operation === undefined) {
                throw new Error(`Unknown application operation "${name}"`);
            }
            return await operation.run(input);
        },
        requireSession,
        router,
        screens,
    };
}

describe('local development navigation with Angular route guards', () => {
    it('reports a canceled navigation and does not report a redirect as the requested screen', async () => {
        let mayLeave = false;
        const { callAsync, router } = setup([
            { path: 'dashboard', component: LocalRouteFixture, canDeactivate: [(): boolean => mayLeave] },
            { path: 'recipes', component: LocalRouteFixture },
            { path: 'products', redirectTo: 'dashboard', pathMatch: 'full' },
        ]);
        await RouterTestingHarness.create('/dashboard');

        await expect(callAsync('open_section', { section: 'recipes' })).rejects.toThrow('Navigation was cancelled.');
        expect(router.url).toBe('/dashboard');
        mayLeave = true;
        await expect(callAsync('open_section', { section: 'recipes' })).resolves.toMatchObject({ route: '/recipes' });
        await expect(callAsync('open_section', { section: 'products' })).rejects.toThrow('unavailable for this session');
        expect(router.url).toBe('/dashboard');
    });

    it('leaves an unpublished dialog open and does not navigate behind it', async () => {
        const { callAsync, router } = setup([
            { path: 'dashboard', component: LocalRouteFixture },
            { path: 'recipes', component: LocalRouteFixture },
        ]);
        await RouterTestingHarness.create('/dashboard');
        const dialog = document.createElement('div');
        dialog.setAttribute('role', 'dialog');
        document.body.append(dialog);
        try {
            await expect(callAsync('open_section', { section: 'recipes' })).rejects.toThrow('Close the current dialog');
            expect(router.url).toBe('/dashboard');
            expect(dialog.isConnected).toBe(true);
        } finally {
            dialog.remove();
        }
    });

    it('keeps a selected client and resets the view through the client list before selecting a different client', async () => {
        const { callAsync, router, requireSession } = setup([
            { path: 'dietologist', component: LocalRouteFixture },
            { path: 'dietologist/clients/:clientId', component: LocalRouteFixture },
        ]);
        const currentRoute = '/dietologist/clients/client-a?tab=meals';
        await RouterTestingHarness.create(currentRoute);
        const seen: string[] = [];
        const events = router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
            seen.push(event.urlAfterRedirects);
        });
        try {
            await expect(callAsync('select_client', { clientId: 'not a client id' })).rejects.toThrow('Invalid entity ID.');
            await expect(callAsync('select_client', { clientId: 'client-a' })).resolves.toMatchObject({ route: currentRoute });
            expect(seen).toEqual([]);
            requireSession.mockClear();
            await expect(callAsync('select_client', { clientId: 'client-b' })).resolves.toMatchObject({
                route: '/dietologist/clients/client-b',
            });
            expect(seen).toEqual(['/dietologist', '/dietologist/clients/client-b']);
            expect(requireSession).toHaveBeenCalledTimes(2);
        } finally {
            events.unsubscribe();
        }
    });
});

describe('local development operations on the published screen', () => {
    const dates = { dateFrom: '2026-05-17', dateTo: '2026-05-23' };

    it.each(['meal-diary', 'statistics', 'client-dashboard'])('passes calendar filters in the format accepted by %s', async kind => {
        const { callAsync, screens } = setup();
        let filters: unknown;
        const set = vi.fn((value: unknown): void => {
            filters = value;
        });
        screens.attach({ kind, read: () => ({ filters }), setFilters: set });
        const timeZone = new Intl.DateTimeFormat().resolvedOptions().timeZone;

        await expect(callAsync('set_calendar_period', { ...dates, timeZone })).resolves.toMatchObject({
            screen: { kind, data: { filters: dates } },
        });
        expect(set).toHaveBeenCalledExactlyOnceWith(kind === 'client-dashboard' ? { ...dates, timeZone } : dates);
    });

    it('rejects a reversed range and requires that the current screen publishes calendar filtering', async () => {
        const { callAsync, screens } = setup();
        const timeZone = new Intl.DateTimeFormat().resolvedOptions().timeZone;
        await expect(callAsync('set_calendar_period', { ...dates, timeZone })).rejects.toThrow('with published filters is required');
        const detach = screens.attach({ kind: 'statistics', read: () => ({}) });
        await expect(callAsync('set_calendar_period', { ...dates, timeZone })).rejects.toThrow('with published filters is required');
        detach();
        const filters = vi.fn();
        screens.attach({ kind: 'statistics', read: () => ({}), setFilters: filters });

        await expect(callAsync('set_calendar_period', { dateFrom: dates.dateTo, dateTo: dates.dateFrom, timeZone })).rejects.toThrow(
            'ordered range',
        );
        expect(filters).not.toHaveBeenCalled();
    });

    it('waits for async filters before reading the updated context and delegates entity opening to that same screen', async () => {
        const { callAsync, screens } = setup();
        let view: unknown = { filters: null };
        const pending = new Subject<void>();
        const setFilters = vi.fn();
        const setFiltersAsync = vi.fn(async (filters: unknown) => {
            await firstValueFrom(pending);
            view = { filters };
        });
        const openEntityAsync = vi.fn((id: string): void => {
            view = { opened: id };
        });
        const input = { filters: { category: 'desserts' } };
        await expect(callAsync('set_screen_filters', input)).rejects.toThrow('does not publish filters');
        await expect(callAsync('open_entity', { id: 'listed-recipe' })).rejects.toThrow('does not publish entity opening');
        screens.attach({ kind: 'recipes', read: () => view, setFilters, setFiltersAsync, openEntityAsync });

        const applying = callAsync('set_screen_filters', input);
        expect(setFiltersAsync).toHaveBeenCalledExactlyOnceWith(input.filters);
        expect(setFilters).not.toHaveBeenCalled();
        expect(screens.read()?.data).toEqual({ filters: null });
        pending.next();
        pending.complete();
        await expect(applying).resolves.toMatchObject({ screen: { data: input } });

        await expect(callAsync('open_entity', { id: 'listed-recipe' })).resolves.toMatchObject({
            screen: { data: { opened: 'listed-recipe' } },
        });
        expect(openEntityAsync).toHaveBeenCalledExactlyOnceWith('listed-recipe');
    });
});

describe('local development screen filters with a synchronous owner', () => {
    it('applies the published statistics filters before returning the new view', async () => {
        const { callAsync, screens } = setup();
        let filters: unknown = null;
        const setFilters = vi.fn((next: unknown): void => {
            filters = next;
        });
        screens.attach({ kind: 'statistics', read: () => ({ filters }), setFilters });
        const input = { filters: { dateFrom: '2026-05-17', dateTo: '2026-05-23' } };

        await expect(callAsync('set_screen_filters', input)).resolves.toMatchObject({ screen: { kind: 'statistics', data: input } });
        expect(setFilters).toHaveBeenCalledExactlyOnceWith(input.filters);
    });
});

describe('local development draft navigation and discard', () => {
    it.each([
        ['product', '/products/add', 'product-editor'],
        ['recipe', '/recipes/add', 'recipe-editor'],
        ['weight', '/weight-history', 'weight-history'],
        ['waist', '/waist-history', 'waist-history'],
        ['cycle-settings', '/cycle-tracking', 'cycle-tracking'],
        ['fasting', '/fasting', 'fasting'],
    ])('opens the %s draft through its owning route', async (kind, path, owner) => {
        const { callAsync, requireSession } = setup([
            { path: 'dashboard', component: LocalRouteFixture },
            { path: path.slice(1), component: LocalDraftRouteFixture, data: { kind: owner } },
        ]);
        await RouterTestingHarness.create('/dashboard');

        await expect(callAsync('open_draft', { kind })).resolves.toMatchObject({
            route: path,
            screen: { kind: owner, data: { opened: true } },
        });
        expect(requireSession).toHaveBeenCalledTimes(kind === 'product' || kind === 'recipe' ? 1 : 2);
    });

    it('reports that a measurement editor is unavailable when its route has not published an opener', async () => {
        const { callAsync } = setup([
            { path: 'dashboard', component: LocalRouteFixture },
            { path: 'weight-history', component: LocalRouteFixture },
        ]);
        await RouterTestingHarness.create('/dashboard');
        await expect(callAsync('open_draft', { kind: 'weight' })).rejects.toThrow('The requested editor is not ready.');
    });

    it('reads draft capabilities and discards only the explicitly confirmed integer revision', async () => {
        const { callAsync, screens } = setup();
        let draft = 'Unsaved recipe name';
        const draftSchema = { type: 'object', properties: { name: { type: 'string' } } };
        screens.attach({
            kind: 'recipe-editor',
            read: () => ({ draft }),
            isDirty: () => draft !== '',
            draftSchema,
            clearDraft: () => {
                draft = '';
            },
        });
        const revision = screens.read()?.revision;
        const confirmed = { revision, confirmDiscard: true };

        await expect(callAsync('read_capabilities', {})).resolves.toMatchObject({
            screen: { kind: 'recipe-editor', revision, draftSchema, dirty: true, actions: { clearDraft: true } },
        });
        await expect(callAsync('clear_current_draft', { ...confirmed, confirmDiscard: false })).rejects.toThrow('Explicit confirmation');
        const fractionalRevision = 1.5;
        await expect(callAsync('clear_current_draft', { ...confirmed, revision: fractionalRevision })).rejects.toThrow('current revision');
        expect(draft).toBe('Unsaved recipe name');

        draft = 'Changed while reviewing the draft';
        await expect(callAsync('clear_current_draft', confirmed)).rejects.toThrow('Draft changed');
        const currentRevision = screens.read()?.revision;
        await expect(callAsync('clear_current_draft', { ...confirmed, revision: currentRevision })).resolves.toMatchObject({
            screen: { dirty: false, data: { draft: '' } },
        });
    });
});

describe('local development operations waiting on application services', () => {
    it('does not hand resolved recipe data to an editor that lost ownership, and fills only the current editor on retry', async () => {
        const pending = new Subject<RecipeDevelopmentDraft>();
        const recipeDraftAsync = vi.fn().mockReturnValueOnce(firstValueFrom(pending));
        const { callAsync, screens } = setup([], [{ provide: LocalWebmcpCatalogService, useValue: { recipeDraftAsync } }]);
        const fillDraft = vi.fn();
        const detach = screens.attach({ kind: 'recipe-editor', read: () => ({}), fillDraft });
        const input = { kind: 'recipe-editor', draft: { name: 'Oats', servings: 2, steps: [{ description: 'Mix', ingredients: [] }] } };
        const first = callAsync('fill_current_draft', input);
        expect(recipeDraftAsync).toHaveBeenCalledExactlyOnceWith(input.draft);
        detach();
        const fillNewDraft = vi.fn();
        screens.attach({ kind: 'recipe-editor', read: () => ({}), fillDraft: fillNewDraft });
        const resolved = new RecipeDevelopmentDraft(input.draft.name, input.draft.servings, input.draft.steps);
        pending.next(resolved);
        pending.complete();
        await expect(first).rejects.toThrow('Screen changed while loading.');
        expect(fillDraft).not.toHaveBeenCalled();
        expect(fillNewDraft).not.toHaveBeenCalled();

        recipeDraftAsync.mockResolvedValueOnce(resolved);
        await expect(callAsync('fill_current_draft', input)).resolves.toMatchObject({
            saved: false,
            context: { screen: { kind: 'recipe-editor' } },
        });
        expect(fillNewDraft).toHaveBeenCalledExactlyOnceWith(resolved);
    });

    it('reads catalog pages through the application service and drops a result after the session changes', async () => {
        const page = { data: [{ id: 'oats', name: 'Oats' }], page: 1, totalItems: 1 };
        const pending = new Subject<typeof page>();
        const searchAsync = vi.fn().mockResolvedValueOnce(page).mockReturnValueOnce(firstValueFrom(pending));
        const { callAsync, requireSession } = setup([], [{ provide: LocalWebmcpCatalogService, useValue: { searchAsync } }]);
        await expect(callAsync('search_catalog', { kind: 'product', search: ' Oats ' })).resolves.toEqual(page);
        expect(searchAsync).toHaveBeenCalledExactlyOnceWith('product', ' Oats ', 1);

        const second = callAsync('search_catalog', { kind: 'recipe', search: 'Porridge', page: 2 });
        expect(searchAsync).toHaveBeenLastCalledWith('recipe', 'Porridge', 2);
        requireSession.mockImplementationOnce(() => {
            throw new DevelopmentToolError('Session changed.');
        });
        pending.next(page);
        pending.complete();
        await expect(second).rejects.toThrow('Session changed.');
    });

    it('relays read-only push diagnostics after checking the session', async () => {
        const diagnostics = { permission: 'denied', hasSubscription: false };
        const readAsync = vi.fn().mockResolvedValue(diagnostics);
        const { callAsync, requireSession } = setup([], [{ provide: LocalWebmcpPushService, useValue: { readAsync } }]);

        await expect(callAsync('read_push_diagnostics', { enable: true })).rejects.toThrow(DevelopmentToolError);
        expect(readAsync).not.toHaveBeenCalled();
        await expect(callAsync('read_push_diagnostics', {})).resolves.toEqual(diagnostics);
        expect(readAsync).toHaveBeenCalledOnce();
        expect(requireSession).toHaveBeenCalledOnce();
    });
});
