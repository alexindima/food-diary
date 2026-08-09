import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, FormField, FormRoot, min, pattern, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { AdminAchievementsFacade } from '../lib/admin-achievements.facade';
import type {
    AchievementMetric,
    AdminAchievementDefinition,
    CreateAdminAchievementDefinitionRequest,
} from '../models/admin-achievement.data';

const EMPTY_MODEL: CreateAdminAchievementDefinitionRequest = {
    key: '',
    category: 'habits',
    metric: 'LongestStreak',
    threshold: 1,
    titleRu: '',
    titleEn: '',
    descriptionRu: '',
    descriptionEn: '',
    icon: 'trophy',
    sortOrder: 0,
    isActive: true,
};

@Component({
    selector: 'fd-admin-achievements',
    imports: [FormField, FormRoot, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent, FdUiTextareaComponent, TranslatePipe],
    templateUrl: './admin-achievements.html',
    styleUrl: './admin-achievements.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAchievementsComponent {
    private readonly facade = inject(AdminAchievementsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    protected readonly definitions = signal<AdminAchievementDefinition[]>([]);
    protected readonly editingId = signal<string | null>(null);
    protected readonly editingVersion = signal<number | null>(null);
    protected readonly isLoading = signal(false);
    protected readonly isSaving = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly metricOptions: Array<FdUiSelectOption<AchievementMetric>> = [
        { value: 'LongestStreak', label: this.translateMetric('ADMIN_ACHIEVEMENTS.METRICS.LONGEST_STREAK') },
        { value: 'TotalMeals', label: this.translateMetric('ADMIN_ACHIEVEMENTS.METRICS.TOTAL_MEALS') },
    ];
    protected readonly formModel = signal<CreateAdminAchievementDefinitionRequest>({ ...EMPTY_MODEL });
    protected readonly form = form(this.formModel, path => {
        required(path.key);
        pattern(path.key, /^[a-z0-9_-]+$/);
        disabled(path.key, { when: () => this.editingId() !== null });
        required(path.category);
        required(path.metric);
        min(path.threshold, 1);
        required(path.titleRu);
        required(path.titleEn);
        required(path.descriptionRu);
        required(path.descriptionEn);
        required(path.icon);
        min(path.sortOrder, 0);
    });

    public constructor() {
        this.load();
    }

    protected load(): void {
        this.isLoading.set(true);
        this.facade
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: definitions => {
                    this.definitions.set(definitions);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.error.set('ADMIN_ACHIEVEMENTS.ERRORS.LOAD');
                    this.isLoading.set(false);
                },
            });
    }

    protected edit(definition: AdminAchievementDefinition): void {
        this.editingId.set(definition.id);
        this.editingVersion.set(definition.version);
        this.formModel.set({
            key: definition.key,
            category: definition.category,
            metric: definition.metric,
            threshold: definition.threshold,
            titleRu: definition.titleRu,
            titleEn: definition.titleEn,
            descriptionRu: definition.descriptionRu,
            descriptionEn: definition.descriptionEn,
            icon: definition.icon,
            sortOrder: definition.sortOrder,
            isActive: definition.isActive,
        });
    }

    protected createNew(): void {
        this.editingId.set(null);
        this.editingVersion.set(null);
        this.formModel.set({ ...EMPTY_MODEL });
    }

    protected save(): void {
        if (this.form().invalid() || this.isSaving()) {
            return;
        }
        this.isSaving.set(true);
        this.error.set(null);
        const request = this.formModel();
        const editingId = this.editingId();
        const editingVersion = this.editingVersion();
        const operation =
            editingId === null || editingVersion === null
                ? this.facade.create(request)
                : this.facade.update(editingId, {
                      category: request.category,
                      metric: request.metric,
                      threshold: request.threshold,
                      titleRu: request.titleRu,
                      titleEn: request.titleEn,
                      descriptionRu: request.descriptionRu,
                      descriptionEn: request.descriptionEn,
                      icon: request.icon,
                      sortOrder: request.sortOrder,
                      isActive: request.isActive,
                      version: editingVersion,
                  });
        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.isSaving.set(false);
                this.createNew();
                this.load();
            },
            error: () => {
                this.error.set('ADMIN_ACHIEVEMENTS.ERRORS.SAVE');
                this.isSaving.set(false);
            },
        });
    }

    private translateMetric(key: string): string {
        const translation: unknown = this.translate.instant(key);
        return typeof translation === 'string' ? translation : key;
    }
}
