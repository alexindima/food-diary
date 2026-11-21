import { computed, DestroyRef, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { DASHBOARD_LAYOUT_CONFIG } from '../../../config/runtime-ui.tokens';
import { UserService } from '../../../shared/api/user.service';
import type { DashboardLayoutSettings } from '../../../shared/models/user.data';

type NormalizedDashboardLayout = DashboardLayoutSettings & { web: string[]; mobile: string[] };

const DEFAULT_LAYOUT: NormalizedDashboardLayout = {
    web: ['summary', 'meals', 'fasting', 'hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'],
    mobile: ['summary', 'meals', 'fasting', 'hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'],
};

// Dynamics predates layout customization and was always visible, including in saved layouts.
// Persist only its opt-out so existing users retain the widget without an API migration.
const HIDDEN_NUTRITION_BLOCK = 'nutrition-hidden';

@Service()
export class DashboardLayoutService {
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly config = inject(DASHBOARD_LAYOUT_CONFIG);

    private readonly layoutInitialized = signal<boolean>(false);
    private readonly layoutSnapshot = signal<NormalizedDashboardLayout | null>(null);
    private readonly viewportWidth = signal<number>(this.config.defaultViewportWidth);
    private saveRequest: Promise<boolean> | null = null;

    private readonly normalizedLayout = signal<NormalizedDashboardLayout>({
        web: [...DEFAULT_LAYOUT.web],
        mobile: [...DEFAULT_LAYOUT.mobile],
    });
    public readonly layoutSettings = this.normalizedLayout.asReadonly();

    public readonly isEditingLayout = computed(() => this.layoutSnapshot() !== null);
    public readonly isSaving = signal(false);
    public readonly saveFailed = signal(false);

    public readonly layoutKey = computed<'web' | 'mobile'>(() =>
        this.viewportWidth() < this.config.mobileBreakpointPx ? 'mobile' : 'web',
    );

    public readonly visibleBlocks = computed(() =>
        this.getLayoutForKey(this.layoutKey()).filter(block => block !== HIDDEN_NUTRITION_BLOCK),
    );

    public readonly hasAsideBlocks = computed(() => {
        const blocks = this.visibleBlocks();
        return blocks.some(block => ['hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'].includes(block));
    });

    public readonly hasLayoutChanges = computed(() => {
        const previous = this.layoutSnapshot();
        if (previous === null) {
            return false;
        }
        const current = this.normalizeLayout(this.layoutSettings());
        return !this.areLayoutsEqual(previous, current);
    });

    public updateViewportWidth(width: number): void {
        this.viewportWidth.set(width);
    }

    public initializeLayout(layout: DashboardLayoutSettings | null): void {
        if (this.layoutInitialized()) {
            return;
        }

        const normalized = this.normalizeLayout(layout);
        this.normalizedLayout.set(normalized);
        if (this.isEditingLayout()) {
            this.layoutSnapshot.set(normalized);
        }
        this.layoutInitialized.set(true);
    }

    public openSettings(): void {
        if (this.isSaving()) {
            return;
        }
        if (this.isEditingLayout()) {
            void this.saveAsync();
            return;
        }
        this.saveFailed.set(false);
        this.layoutSnapshot.set(this.normalizeLayout(this.layoutSettings()));
    }

    public async saveAsync(): Promise<boolean> {
        if (this.saveRequest !== null) {
            return this.saveRequest;
        }
        if (!this.isEditingLayout()) {
            return true;
        }
        if (!this.hasLayoutChanges()) {
            this.finishEditing();
            return true;
        }
        this.isSaving.set(true);
        this.saveFailed.set(false);
        this.saveRequest = this.persistLayoutAsync(this.normalizeLayout(this.layoutSettings())).finally(() => {
            this.saveRequest = null;
        });
        return this.saveRequest;
    }

    public discard(): void {
        const snapshot = this.layoutSnapshot();
        if (snapshot === null || this.isSaving()) {
            return;
        }

        this.normalizedLayout.set(this.normalizeLayout(snapshot));
        this.finishEditing();
    }

    public shouldRenderBlock(blockId: string): boolean {
        return this.isEditingLayout() || this.isBlockVisible(blockId);
    }

    public isBlockVisible(blockId: string): boolean {
        if (blockId === 'nutrition') {
            return !this.getLayoutForKey(this.layoutKey()).includes(HIDDEN_NUTRITION_BLOCK);
        }
        return this.visibleBlocks().includes(blockId);
    }

    public canToggleBlock(blockId: string): boolean {
        return blockId !== 'summary' && !this.isSaving();
    }

    public toggleBlock(blockId: string): void {
        if (!this.isEditingLayout() || !this.canToggleBlock(blockId)) {
            return;
        }

        const key = this.layoutKey();
        const baseOrder = [...DEFAULT_LAYOUT[key], HIDDEN_NUTRITION_BLOCK];
        const storedBlockId = blockId === 'nutrition' ? HIDDEN_NUTRITION_BLOCK : blockId;
        const current = this.getLayoutForKey(key);
        const isVisible = current.includes(storedBlockId);

        const next = isVisible
            ? current.filter(item => item !== storedBlockId)
            : baseOrder.filter(item => item === storedBlockId || current.includes(item));

        this.normalizedLayout.update(layout => ({
            ...layout,
            [key]: this.ensureSummary(next, baseOrder),
        }));
    }

    private getLayoutForKey(key: 'web' | 'mobile'): string[] {
        return this.layoutSettings()[key];
    }

    private normalizeLayout(layout: DashboardLayoutSettings | null): NormalizedDashboardLayout {
        return {
            web: this.normalizeLayoutList(layout?.web, DEFAULT_LAYOUT.web),
            mobile: this.normalizeLayoutList(layout?.mobile, DEFAULT_LAYOUT.mobile),
            ...(layout?.setupHintDismissed === undefined ? {} : { setupHintDismissed: layout.setupHintDismissed }),
        };
    }

    private normalizeLayoutList(values: string[] | null | undefined, fallback: string[]): string[] {
        const allowed = new Set([...fallback, HIDDEN_NUTRITION_BLOCK]);
        const source = values !== null && values !== undefined && values.length > 0 ? values : fallback;
        const filtered: string[] = [];
        for (const item of source) {
            if (allowed.has(item) && !filtered.includes(item)) {
                filtered.push(item);
            }
        }
        return this.ensureSummary(filtered, fallback);
    }

    private ensureSummary(values: string[], fallback: string[]): string[] {
        if (values.includes('summary')) {
            return values;
        }
        return ['summary', ...values.filter(item => item !== 'summary' && (fallback.includes(item) || item === HIDDEN_NUTRITION_BLOCK))];
    }

    private async persistLayoutAsync(layout: NormalizedDashboardLayout): Promise<boolean> {
        try {
            const user = await firstValueFrom(
                this.userService
                    .updateDashboardLayout({ web: layout.web, mobile: layout.mobile })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
                { defaultValue: null },
            );
            if (user === null) {
                this.saveFailed.set(true);
                return false;
            }
            this.normalizedLayout.set(this.normalizeLayout(user.dashboardLayout ?? layout));
            this.finishEditing();
            return true;
        } catch {
            this.saveFailed.set(true);
            return false;
        } finally {
            this.isSaving.set(false);
        }
    }

    private finishEditing(): void {
        this.layoutSnapshot.set(null);
        this.saveFailed.set(false);
    }

    private areLayoutsEqual(a: NormalizedDashboardLayout, b: NormalizedDashboardLayout): boolean {
        return this.layoutToKey(a) === this.layoutToKey(b);
    }

    private layoutToKey(layout: NormalizedDashboardLayout): string {
        const web = layout.web.join('|');
        const mobile = layout.mobile.join('|');
        return `${web}::${mobile}`;
    }
}
