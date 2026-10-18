import { DOCUMENT } from '@angular/common';
import { afterNextRender, ApplicationRef, effect, inject, Injector, isDevMode, Service, untracked } from '@angular/core';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../../features/admin-auth/lib/admin-auth.service';
import type { AdminDevelopmentScreen } from './admin-development-screen';

type AdminTool = {
    name: string;
    description: string;
    inputSchema: Record<string, unknown>;
    annotations: { readOnlyHint: boolean };
    execute: (input: unknown) => Promise<unknown>;
};
type AdminModelContext = {
    registerTool: (tool: AdminTool, options: { signal: AbortSignal }) => void | Promise<void>;
    unregisterTool?: (name: string) => void;
};
const MAX_FILTER_LENGTH = 200;
const ADMIN_SECTIONS = [
    '',
    'users',
    'catalog',
    'meal-plans',
    'analytics/retention',
    'audit',
    'bugs',
    'ai-usage',
    'acquisition',
    'ai-prompts',
    'billing',
    'email-templates',
    'outgoing-emails',
    'mail-inbox',
    'daily-advices',
    'lessons',
    'achievements',
    'moderation',
];

@Service()
export class AdminWebmcpService {
    private readonly document = inject(DOCUMENT);
    private readonly auth = inject(AdminAuthService);
    private readonly router = inject(Router);
    private readonly injector = inject(Injector);
    private screens: AdminDevelopmentScreen[] = [];
    private busy = false;

    public attach(screen: AdminDevelopmentScreen): () => void {
        this.screens.push(screen);
        return () => {
            this.screens = this.screens.filter(candidate => candidate !== screen);
        };
    }

    public initialize(): void {
        afterNextRender(() => {
            if (!isDevMode() || !['localhost', '127.0.0.1', '[::1]'].includes(this.document.location.hostname)) {
                return;
            }
            const context = (this.document as Document & { modelContext?: AdminModelContext }).modelContext;
            if (typeof context?.registerTool !== 'function') {
                return;
            }
            effect(
                onCleanup => {
                    if (!this.auth.isAuthenticated() || !this.auth.isAdmin()) {
                        return;
                    }
                    const session = this.auth.getToken();
                    const controller = new AbortController();
                    const tools = untracked(() => this.tools(session));
                    void this.registerAsync(context, tools, controller);
                    onCleanup(() => {
                        controller.abort();
                        for (const tool of tools) {
                            context.unregisterTool?.(tool.name);
                        }
                    });
                },
                { injector: this.injector },
            );
        });
    }

    private async registerAsync(context: AdminModelContext, tools: AdminTool[], controller: AbortController): Promise<void> {
        try {
            for (const tool of tools) {
                if (controller.signal.aborted) {
                    return;
                }
                await context.registerTool(tool, { signal: controller.signal });
            }
        } catch {
            controller.abort();
            for (const tool of tools) {
                context.unregisterTool?.(tool.name);
            }
        }
    }

    private tools(session: string | null): AdminTool[] {
        return [
            this.readScreenContextTool(session),
            this.openSectionTool(session),
            this.setScreenFiltersTool(session),
            this.openEntityTool(session),
        ];
    }
    private readScreenContextTool(session: string | null): AdminTool {
        return this.operation(session, {
            name: 'fooddiary_admin_' + 'read_screen_context',
            description:
                'Read safe live admin context and published list/filter state. No message bodies, tokens, passwords or confirmation links.',
            inputSchema: { type: 'object', additionalProperties: false, properties: {}, required: [] },
            annotations: { readOnlyHint: true },
            run: () => this.read(),
        });
    }
    private openSectionTool(session: string | null): AdminTool {
        const runAsync = async (value: Record<string, unknown>): Promise<unknown> => {
            this.requireClean();
            const section = value['section'];
            if (typeof section !== 'string' || !ADMIN_SECTIONS.includes(section)) {
                throw new Error('Unsupported section.');
            }
            const path = `/${section}`;
            if (this.router.url.split('?')[0] !== path && !(await this.router.navigateByUrl(path))) {
                throw new Error('Cancelled.');
            }
            return this.completeAsync();
        };
        return this.operation(session, {
            name: 'fooddiary_admin_' + 'open_section',
            description: 'Open an allowlisted admin section in this app using normal route guards. Empty section means dashboard.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                properties: { section: { type: 'string', enum: ADMIN_SECTIONS } },
                required: ['section'],
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        });
    }
    private setScreenFiltersTool(session: string | null): AdminTool {
        const runAsync = async (value: Record<string, unknown>): Promise<unknown> => {
            this.requireClean();
            const screen = this.screens.at(-1);
            if (screen?.setFiltersAsync === undefined) {
                throw new Error('Unsupported filters.');
            }
            const fields = screen.filterFields ?? [];
            const raw = this.record(value['filters'], fields);
            const params: Record<string, string | null> = Object.fromEntries(fields.map(field => [field, null]));
            for (const [key, item] of Object.entries(raw)) {
                params[key] = this.filterValue(item);
            }
            await screen.setFiltersAsync(params);
            return this.completeAsync();
        };
        return this.operation(session, {
            name: 'fooddiary_admin_' + 'set_screen_filters',
            description:
                'Replace allowlisted CURRENT list query filters. Read filterFields first. Calendar/UTC policies belong to the existing admin screen.',
            inputSchema: {
                type: 'object',
                additionalProperties: false,
                properties: { filters: { type: 'object' } },
                required: ['filters'],
            },
            annotations: { readOnlyHint: false },
            run: runAsync,
        });
    }
    private openEntityTool(session: string | null): AdminTool {
        const runAsync = async (value: Record<string, unknown>): Promise<unknown> => {
            this.requireClean();
            const id = value['id'];
            const open = this.screens.at(-1)?.openEntityAsync;
            if (typeof id !== 'string' || open === undefined) {
                throw new Error('Unsupported record.');
            }
            await open(id);
            return this.completeAsync();
        };
        return this.operation(session, {
            name: 'fooddiary_admin_' + 'open_entity',
            description: 'Open a record from the CURRENT published list by ID. Only screens with a read-only detail action support this.',
            inputSchema: { type: 'object', additionalProperties: false, properties: { id: { type: 'string' } }, required: ['id'] },
            annotations: { readOnlyHint: false },
            run: runAsync,
        });
    }
    private operation(
        session: string | null,
        operation: Omit<AdminTool, 'execute'> & { run: (input: Record<string, unknown>) => unknown },
    ): AdminTool {
        const executeAsync = async (input: unknown): Promise<unknown> => {
            const changesScreen = !operation.annotations.readOnlyHint;
            try {
                this.requireSession(session);
                if (changesScreen && this.busy) {
                    return { ok: false, error: 'Another development tool is running.' };
                }
                if (changesScreen) {
                    this.busy = true;
                }
                try {
                    const schema = operation.inputSchema['properties'];
                    const value = this.record(input, typeof schema === 'object' && schema !== null ? Object.keys(schema) : []);
                    const result = await operation.run(value);
                    this.requireSession(session);
                    return result;
                } finally {
                    if (changesScreen) {
                        this.busy = false;
                    }
                }
            } catch {
                return {
                    ok: false,
                    error: 'Operation unavailable. Read current context; check session, supported fields, IDs and unsaved changes.',
                };
            }
        };
        const { run: _run, ...definition } = operation;
        return { ...definition, description: `${definition.description} Errors return {ok:false,error}.`, execute: executeAsync };
    }

    private filterValue(item: unknown): string | null {
        if (item === null) {
            return null;
        }
        if (typeof item !== 'string' && typeof item !== 'boolean' && (typeof item !== 'number' || !Number.isFinite(item))) {
            throw new Error('Invalid filter.');
        }
        const value = String(item);
        if (value.length > MAX_FILTER_LENGTH) {
            throw new Error('Filter too long.');
        }
        return value;
    }

    private read(): unknown {
        const screen = this.screens.at(-1);
        return {
            path: this.router.url.split('?')[0].split('#')[0],
            role: 'Admin',
            locale: this.document.documentElement.lang,
            timeZone: new Intl.DateTimeFormat().resolvedOptions().timeZone,
            configuredApiUrls: environment.apiUrls,
            buildRevision: null,
            blockingDialog: this.document.querySelector('[role="dialog"]') !== null,
            screen:
                screen === undefined
                    ? null
                    : {
                          kind: screen.kind,
                          data: screen.read(),
                          filterFields: screen.filterFields ?? [],
                          actions: {
                              setFiltersAsync: screen.setFiltersAsync !== undefined,
                              openEntityAsync: screen.openEntityAsync !== undefined,
                          },
                      },
            mailInfrastructure: 'Server outbox, SMTP delivery and worker health are not observable from browser list state.',
        };
    }

    private async completeAsync(): Promise<unknown> {
        await this.injector.get(ApplicationRef).whenStable();
        return this.read();
    }

    private requireSession(session: string | null): void {
        if (session === null || !this.auth.isAuthenticated() || !this.auth.isAdmin() || this.auth.getToken() !== session) {
            throw new Error('Session changed.');
        }
    }

    private requireClean(): void {
        if (this.document.querySelector('[role="dialog"]') !== null) {
            throw new Error('Close the current dialog through the interface.');
        }
        if (this.screens.length === 0 && this.router.url.split('?')[0] !== '/') {
            throw new Error('This screen does not publish unsaved-change state. Navigate through the interface.');
        }
        if (this.screens.some(screen => screen.isDirty?.() === true)) {
            throw new Error('Unsaved changes.');
        }
    }

    private record(input: unknown, keys: readonly string[]): Record<string, unknown> {
        if (typeof input !== 'object' || input === null || Array.isArray(input) || Object.keys(input).some(key => !keys.includes(key))) {
            throw new Error('Invalid arguments.');
        }
        return Object.fromEntries(Object.entries(input));
    }
}
