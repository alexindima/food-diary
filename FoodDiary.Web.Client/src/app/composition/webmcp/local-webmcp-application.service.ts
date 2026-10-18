import { DOCUMENT } from '@angular/common';
import { ApplicationRef, inject, Injector, Service } from '@angular/core';
import { Router } from '@angular/router';

import {
    DEV_EMPTY_SCHEMA,
    DEV_OBJECT_SCHEMA,
    devDay,
    devEnum,
    devNumber,
    devRecord,
    devText,
} from '../../shared/development/development-input';
import { type DevelopmentScreen, DevelopmentToolError } from '../../shared/development/development-tools';
import type { LocalTool } from './local-webmcp.types';
import { LocalWebmcpCatalogService } from './local-webmcp-catalog.service';
import { LocalWebmcpPushService } from './local-webmcp-push.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

type OperationContext = {
    read: () => unknown;
    requireClean: () => void;
    requireSession: () => void;
    completeAsync: () => Promise<unknown>;
};
export type LocalToolOperation = Omit<LocalTool, 'execute'> & { run: (input: unknown) => unknown };
const MAX_KIND_LENGTH = 80;
const MAX_SEARCH_LENGTH = 200;
const MAX_ID_LENGTH = 100;
const SECTIONS = [
    'dashboard',
    'meals',
    'products',
    'recipes',
    'weight-history',
    'waist-history',
    'statistics',
    'goals',
    'meal-plans',
    'shopping-lists',
    'dietologist',
    'recommendations',
    'lessons',
    'cycle-tracking',
    'fasting',
] as const;

@Service()
export class LocalWebmcpApplicationService {
    private readonly document = inject(DOCUMENT);
    private readonly router = inject(Router);
    private readonly injector = inject(Injector);
    private readonly screens = inject(LocalWebmcpScreensService);
    private readonly catalog = inject(LocalWebmcpCatalogService);

    public operations(read: () => unknown, requireClean: () => void, requireSession: () => void): LocalToolOperation[] {
        const context: OperationContext = {
            read,
            requireClean,
            requireSession,
            completeAsync: async () => {
                await this.injector.get(ApplicationRef).whenStable();
                requireSession();
                return read();
            },
        };
        const readPushAsync = async (input: unknown): Promise<unknown> => {
            devRecord(input, []);
            const diagnostics = await this.injector.get(LocalWebmcpPushService).readAsync();
            requireSession();
            return diagnostics;
        };
        return [
            this.openSectionOperation(context),
            this.selectClientOperation(context),
            this.openEntityOperation(context),
            this.setScreenFiltersOperation(context),
            this.openDraftOperation(context),
            this.setCalendarPeriodOperation(context),
            this.fillCurrentDraftOperation(context),
            this.clearCurrentDraftOperation(context),
            this.searchCatalogOperation(context),
            this.readCapabilitiesOperation(),
            {
                name: 'fooddiary_' + 'read_push_diagnostics',
                description: 'Read browser permission, service worker, subscription presence, provider availability and redacted server device metadata. Never enables push or proves delivery.',
                inputSchema: DEV_EMPTY_SCHEMA,
                annotations: { readOnlyHint: true },
                run: readPushAsync,
            },
        ];
    }
    private openSectionOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const section = devEnum(devRecord(input, ['section'])['section'], SECTIONS);
            requireClean();
            await this.navigateAsync(`/${section}`);
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'open_section',
            description: 'Open an allowlisted user section. Existing route guards apply. Refuses unsaved drafts.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['section'],
                properties: { section: { type: 'string', enum: SECTIONS } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private selectClientOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const id = this.readId(devRecord(input, ['clientId'])['clientId']);
            requireClean();
            if (
                this.router.url.startsWith('/dietologist/clients/') &&
                this.router.url.split('?')[0] !== `/dietologist/clients/${encodeURIComponent(id)}`
            ) {
                await this.navigateAsync('/dietologist');
                await completeAsync();
            }
            await this.navigateAsync(`/dietologist/clients/${encodeURIComponent(id)}`);
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'select_client',
            description: 'Open the dietologist dashboard for an existing client ID. Server permissions apply; no role switching.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['clientId'],
                properties: { clientId: { type: 'string' } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private openEntityOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const id = this.readId(devRecord(input, ['id'])['id']);
            requireClean();
            const open = this.screens.current()?.openEntityAsync;
            if (open === undefined) {
                throw new DevelopmentToolError('This screen does not publish entity opening.');
            }
            await open(id);
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'open_entity',
            description: 'Open an entity from the CURRENT published list by ID. Does not guess URLs or mutate records.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['id'],
                properties: { id: { type: 'string' } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private setScreenFiltersOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const value = devRecord(input, ['filters']);
            requireClean();
            const screen = this.screens.current();
            const set = screen?.setFiltersAsync ?? screen?.setFilters;
            if (set === undefined) {
                throw new DevelopmentToolError('This screen does not publish filters.');
            }
            await set(value['filters']);
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'set_screen_filters',
            description:
                'Set filters on the CURRENT supported list, statistics or dashboard. Read screen context for supported filter fields. Uses owning URL/state logic.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['filters'],
                properties: { filters: DEV_OBJECT_SCHEMA },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private openDraftOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const kind = devEnum(devRecord(input, ['kind'])['kind'], ['product', 'recipe', 'weight', 'waist', 'cycle-settings', 'fasting']);
            requireClean();
            if (kind === 'product' || kind === 'recipe') {
                await this.navigateAsync(kind === 'product' ? '/products/add' : '/recipes/add');
            } else {
                await this.navigateAsync(
                    kind === 'fasting' ? '/fasting' : kind === 'cycle-settings' ? '/cycle-tracking' : kind === 'weight' ? '/weight-history' : '/waist-history',
                );
                await completeAsync();
                const open = this.screens.current()?.openDraft;
                if (open === undefined) {
                    throw new DevelopmentToolError('The requested editor is not ready.');
                }
                open();
            }
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'open_draft',
            description: 'Open an unsaved product, recipe, weight, waist, existing cycle-settings or fasting setup form. Never creates, starts or saves a record.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['kind'],
                properties: { kind: { type: 'string', enum: ['product', 'recipe', 'weight', 'waist', 'cycle-settings', 'fasting'] } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private setCalendarPeriodOperation(context: OperationContext): LocalToolOperation {
        const { requireClean, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const value = devRecord(input, ['dateFrom', 'dateTo', 'timeZone']);
            const dateFrom = devDay(value['dateFrom']);
            const dateTo = devDay(value['dateTo']);
            requireClean();
            if (value['timeZone'] !== new Intl.DateTimeFormat().resolvedOptions().timeZone || dateFrom > dateTo) {
                throw new DevelopmentToolError('Use the current timezone and an ordered range on a supported diary, client or statistics screen.');
            }
            const screen = this.requirePeriodScreen();
            const setFilters = screen.setFiltersAsync ?? screen.setFilters;
            await setFilters?.(
                screen.kind === 'client-dashboard' ? { dateFrom, dateTo, timeZone: value['timeZone'] } : { dateFrom, dateTo },
            );
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'set_calendar_period',
            description:
                'Set an inclusive calendar period on the diary, client dashboard or statistics. timeZone must exactly match current context; no implicit timezone conversion.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['dateFrom', 'dateTo', 'timeZone'],
                properties: { dateFrom: { type: 'string' }, dateTo: { type: 'string' }, timeZone: { type: 'string' } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private fillCurrentDraftOperation(context: OperationContext): LocalToolOperation {
        const { read, requireClean, requireSession, completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const value = devRecord(input, ['kind', 'draft', 'expectedRevision']);
            const kind = devText(value['kind'], MAX_KIND_LENGTH);
            requireClean();
            const screen = this.screens.current();
            if (screen?.kind !== kind || screen.fillDraft === undefined) {
                throw new DevelopmentToolError('Read the current editor kind and schema first.');
            }
            if (kind === 'cycle-settings' || kind === 'fasting-setup' || value['expectedRevision'] !== undefined) {
                this.screens.requireRevision(devNumber(value['expectedRevision'], 1, Number.MAX_SAFE_INTEGER));
            }
            const draft = kind === 'recipe-editor' ? await this.catalog.recipeDraftAsync(value['draft']) : value['draft'];
            requireSession();
            requireClean();
            if (screen !== this.screens.current()) {
                throw new DevelopmentToolError('Screen changed while loading.');
            }
            screen.fillDraft(draft);
            await completeAsync();
            return { saved: false, context: read() };
        };
        return {
            name: 'fooddiary_' + 'fill_current_draft',
            description:
                'Fill the CURRENT clean form using its kind and draftSchema. Cycle settings and fasting setup require expectedRevision. Cycle settings accept only duration fields; consent is never changed. Never submits or starts.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['kind', 'draft'],
                properties: { kind: { type: 'string' }, draft: DEV_OBJECT_SCHEMA, expectedRevision: { type: 'integer', minimum: 1 } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private clearCurrentDraftOperation(context: OperationContext): LocalToolOperation {
        const { completeAsync } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const value = devRecord(input, ['revision', 'confirmDiscard']);
            const revision = devNumber(value['revision'], 1, Number.MAX_SAFE_INTEGER);
            if (value['confirmDiscard'] !== true || !Number.isInteger(revision)) {
                throw new DevelopmentToolError('Explicit confirmation and current revision are required.');
            }
            this.screens.clear(revision);
            return completeAsync();
        };
        return {
            name: 'fooddiary_' + 'clear_current_draft',
            description:
                'Explicitly DISCARD the current unsaved draft, including duration-only cycle settings edits. Requires confirmDiscard=true and the unchanged revision returned by read_screen_context. Does not delete saved records.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['revision', 'confirmDiscard'],
                properties: { revision: { type: 'integer', minimum: 1 }, confirmDiscard: { const: true } },
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        };
    }

    private searchCatalogOperation(context: OperationContext): LocalToolOperation {
        const { requireSession } = context;
        const runAsync = async (input: unknown): Promise<unknown> => {
            const value = devRecord(input, ['kind', 'search', 'page']);
            const result = await this.catalog.searchAsync(
                devEnum(value['kind'], ['product', 'recipe']),
                devText(value['search'], MAX_SEARCH_LENGTH, true),
                value['page'] ?? 1,
            );
            requireSession();
            return result;
        };
        return {
            name: 'fooddiary_' + 'search_catalog',
            description:
                'Search existing products or recipes using the normal authenticated SDK. Returns IDs, units/basis/portions and nutrition. No external provider or mutation.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                required: ['kind', 'search'],
                properties: {
                    kind: { enum: ['product', 'recipe'] },
                    search: { type: 'string', maxLength: 200 },
                    page: { type: 'integer', minimum: 1 },
                },
            },
            annotations: { readOnlyHint: true },
            run: runAsync,
        };
    }

    private readCapabilitiesOperation(): LocalToolOperation {
        return {
            name: 'fooddiary_' + 'read_capabilities',
            description:
                'Read the current published screen capabilities, schemas and limitations. No operation implies tested UI coverage.',
            inputSchema: DEV_EMPTY_SCHEMA,
            annotations: { readOnlyHint: true },
            run: (input: unknown): unknown => {
                devRecord(input, []);
                return {
                    screen: this.screens.read(),
                    limitations: [
                        'No save/delete/provider calls',
                        'Frontend source captures and API response-observed binary/process identities are separate; absent metadata stays unavailable. Server outbox/SMTP state requires separate runtime diagnostics.',
                    ],
                };
            },
        };
    }

    private readId(value: unknown): string {
        const id = devText(value, MAX_ID_LENGTH);
        if (!/^[a-zA-Z0-9_-]+$/.test(id)) {
            throw new DevelopmentToolError('Invalid entity ID.');
        }
        return id;
    }

    private async navigateAsync(path: string): Promise<void> {
        this.screens.prepareNavigation(this.document.querySelector('[role="dialog"]') !== null);
        await this.injector.get(ApplicationRef).whenStable();
        if (this.router.url.split('?')[0] !== path && !(await this.router.navigateByUrl(path))) {
            throw new DevelopmentToolError('Navigation was cancelled.');
        }
        if (this.router.url.split('?')[0] !== path) {
            throw new DevelopmentToolError(
                'The requested screen is unavailable for this session. Inspect the current route and permissions.',
            );
        }
    }

    private requirePeriodScreen(): DevelopmentScreen {
        const screen = this.screens.current();
        if (
            screen === undefined ||
            !['meal-diary', 'client-dashboard', 'statistics'].includes(screen.kind) ||
            (screen.setFiltersAsync === undefined && screen.setFilters === undefined)
        ) {
            throw new DevelopmentToolError('A diary, client dashboard or statistics screen with published filters is required.');
        }
        return screen;
    }
}
