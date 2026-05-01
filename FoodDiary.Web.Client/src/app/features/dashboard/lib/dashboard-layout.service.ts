import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserService } from '../../../shared/api/user.service';
import { DashboardLayoutSettings } from '../../../shared/models/user.data';

const DEFAULT_LAYOUT: DashboardLayoutSettings = {
    web: ['summary', 'meals', 'fasting', 'hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'],
    mobile: ['summary', 'meals', 'fasting', 'hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'],
};

@Injectable()
export class DashboardLayoutService {
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly layoutInitialized = signal<boolean>(false);
    private readonly layoutSnapshot = signal<DashboardLayoutSettings | null>(null);
    private readonly viewportWidth = signal<number>(typeof window === 'undefined' ? 1024 : window.innerWidth);

    public readonly layoutSettings = signal<DashboardLayoutSettings>({
        web: [...DEFAULT_LAYOUT.web!],
        mobile: [...DEFAULT_LAYOUT.mobile!],
    });

    public readonly isEditingLayout = signal<boolean>(false);

    public readonly layoutKey = computed<'web' | 'mobile'>(() => (this.viewportWidth() < 768 ? 'mobile' : 'web'));

    public readonly visibleBlocks = computed(() => this.getLayoutForKey(this.layoutKey()));

    public readonly hasAsideBlocks = computed(() => {
        const blocks = this.visibleBlocks();
        return blocks.some(block => ['hydration', 'cycle', 'weight', 'waist', 'tdee', 'advice'].includes(block));
    });

    public readonly hasLayoutChanges = computed(() => {
        if (!this.isEditingLayout()) {
            return false;
        }
        const previous = this.layoutSnapshot();
        if (!previous) {
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
        this.layoutSettings.set(normalized);
        this.layoutInitialized.set(true);
    }

    public openSettings(): void {
        const next = !this.isEditingLayout();
        this.isEditingLayout.set(next);
        if (!next) {
            this.persistLayoutIfChanged();
            this.layoutSnapshot.set(null);
        } else {
            this.layoutSnapshot.set(this.normalizeLayout(this.layoutSettings()));
        }
    }

    public save(): void {
        if (!this.isEditingLayout()) {
            return;
        }

        this.isEditingLayout.set(false);
        this.persistLayoutIfChanged();
        this.layoutSnapshot.set(null);
    }

    public discard(): void {
        if (!this.isEditingLayout()) {
            return;
        }

        const snapshot = this.layoutSnapshot();
        if (snapshot) {
            this.layoutSettings.set(this.normalizeLayout(snapshot));
        }
        this.isEditingLayout.set(false);
        this.layoutSnapshot.set(null);
    }

    public shouldRenderBlock(blockId: string): boolean {
        return this.isEditingLayout() || this.isBlockVisible(blockId);
    }

    public isBlockVisible(blockId: string): boolean {
        return this.visibleBlocks().includes(blockId);
    }

    public canToggleBlock(blockId: string): boolean {
        return blockId !== 'summary';
    }

    public toggleBlock(blockId: string): void {
        if (!this.isEditingLayout() || !this.canToggleBlock(blockId)) {
            return;
        }

        const key = this.layoutKey();
        const baseOrder = DEFAULT_LAYOUT[key] ?? [];
        const current = this.getLayoutForKey(key);
        const isVisible = current.includes(blockId);

        const next = isVisible
            ? current.filter(item => item !== blockId)
            : baseOrder.filter(item => item === blockId || current.includes(item));

        this.layoutSettings.update(layout => ({
            ...layout,
            [key]: this.ensureSummary(next, baseOrder),
        }));
    }

    private getLayoutForKey(key: 'web' | 'mobile'): string[] {
        const layout = this.layoutSettings();
        const fallback = DEFAULT_LAYOUT[key] ?? [];
        const values = layout[key] && layout[key]?.length ? layout[key]! : fallback;
        return this.ensureSummary(values, fallback);
    }

    private normalizeLayout(layout: DashboardLayoutSettings | null): DashboardLayoutSettings {
        return {
            web: this.normalizeLayoutList(layout?.web, DEFAULT_LAYOUT.web ?? []),
            mobile: this.normalizeLayoutList(layout?.mobile, DEFAULT_LAYOUT.mobile ?? []),
        };
    }

    private normalizeLayoutList(values: string[] | null | undefined, fallback: string[]): string[] {
        const allowed = new Set(fallback);
        const source = values && values.length ? values : fallback;
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
        return ['summary', ...values.filter(item => item !== 'summary' && fallback.includes(item))];
    }

    private persistLayout(): void {
        const layout = this.normalizeLayout(this.layoutSettings());
        this.userService
            .updateDashboardLayout(layout)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(user => {
                if (user?.dashboardLayout) {
                    this.layoutSettings.set(this.normalizeLayout(user.dashboardLayout));
                } else {
                    this.layoutSettings.set(layout);
                }
            });
    }

    private persistLayoutIfChanged(): void {
        const current = this.normalizeLayout(this.layoutSettings());
        const previous = this.layoutSnapshot();
        if (previous && this.areLayoutsEqual(previous, current)) {
            return;
        }

        this.persistLayout();
    }

    private areLayoutsEqual(a: DashboardLayoutSettings, b: DashboardLayoutSettings): boolean {
        return this.layoutToKey(a) === this.layoutToKey(b);
    }

    private layoutToKey(layout: DashboardLayoutSettings): string {
        const web = (layout.web ?? []).join('|');
        const mobile = (layout.mobile ?? []).join('|');
        return `${web}::${mobile}`;
    }
}
