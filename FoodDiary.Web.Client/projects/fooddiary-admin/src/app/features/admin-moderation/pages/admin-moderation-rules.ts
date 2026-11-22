import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import type { Observable } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminModerationNavigationComponent } from '../components/admin-moderation-navigation';
import { AdminRecipeModerationRuleCategoriesComponent } from '../components/admin-recipe-moderation-rule-categories';
import { AdminRecipeModerationRuleHistoryComponent } from '../components/admin-recipe-moderation-rule-history';
import { AdminRecipeModerationFacade } from '../lib/admin-recipe-moderation.facade';
import type { AdminRecipeModerationPolicy, RecipeModerationPolicyId } from '../models/admin-recipe-moderation.data';

const COMMON_CATEGORIES = [
    { value: 'self-harm', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.SELF_HARM' },
    { value: 'self-harm/intent', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.SELF_HARM_INTENT' },
    { value: 'self-harm/instructions', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.SELF_HARM_INSTRUCTIONS' },
    { value: 'sexual', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.SEXUAL' },
    { value: 'violence', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.VIOLENCE' },
    { value: 'violence/graphic', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.VIOLENCE_GRAPHIC' },
] as const;

const TEXT_CATEGORIES = [
    { value: 'harassment', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.HARASSMENT' },
    { value: 'harassment/threatening', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.HARASSMENT_THREATENING' },
    { value: 'hate', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.HATE' },
    { value: 'hate/threatening', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.HATE_THREATENING' },
    { value: 'illicit', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.ILLICIT' },
    { value: 'illicit/violent', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.ILLICIT_VIOLENT' },
    { value: 'sexual/minors', label: 'ADMIN_RECIPE_MODERATION.CATEGORIES.SEXUAL_MINORS' },
    ...COMMON_CATEGORIES,
] as const;

const MAX_SAMPLE_PERCENT = 100;

@Component({
    selector: 'fd-admin-moderation-rules',
    imports: [
        AdminLoadErrorComponent,
        AdminModerationNavigationComponent,
        AdminRecipeModerationRuleCategoriesComponent,
        AdminRecipeModerationRuleHistoryComponent,
        DatePipe,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        FdUiInputComponent,
        TranslatePipe,
    ],
    templateUrl: './admin-moderation-rules.html',
    styleUrl: './admin-moderation-rules.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationRulesComponent {
    private readonly moderation = inject(AdminRecipeModerationFacade);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly textCategoryOptions = TEXT_CATEGORIES;
    protected readonly imageCategoryOptions = COMMON_CATEGORIES;
    protected readonly categoryLabels: Record<string, string> = Object.fromEntries(
        TEXT_CATEGORIES.map(option => [option.value, option.label]),
    );
    protected readonly policy = signal<AdminRecipeModerationPolicy | null>(null);
    protected readonly textCategories = signal<string[]>([]);
    protected readonly imageCategories = signal<string[]>([]);
    protected readonly samplePercent = signal<number | null>(null);
    protected readonly loading = signal(false);
    protected readonly loadFailed = signal(false);
    protected readonly saving = signal(false);
    protected readonly updateFailed = signal(false);
    protected readonly updated = signal(false);
    protected readonly validSample = computed(() => {
        const amount = this.samplePercent();
        return amount !== null && Number.isInteger(amount) && amount >= 0 && amount <= MAX_SAMPLE_PERCENT;
    });

    protected readonly validRules = computed(
        () => this.validSample() && this.textCategories().length > 0 && this.imageCategories().length > 0,
    );

    public constructor() {
        this.load();
    }

    protected load(): void {
        if (this.loading()) {
            return;
        }
        this.loading.set(true);
        this.loadFailed.set(false);
        this.moderation
            .getPolicy()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: policy => {
                    this.applyPolicy(policy);
                    this.loading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.loading.set(false);
                },
            });
    }

    protected setCategory(kind: 'text' | 'image', category: string, selected: boolean): void {
        const values = kind === 'text' ? this.textCategories : this.imageCategories;
        values.update(items => (selected ? [...items, category] : items.filter(item => item !== category)));
        this.updated.set(false);
    }

    protected setSamplePercent(value: FdUiInputValue): void {
        this.samplePercent.set(value === null || value === '' ? null : Number(value));
        this.updated.set(false);
    }

    protected publish(): void {
        const samplePercent = this.samplePercent();
        if (samplePercent === null || this.saving() || !this.validRules()) {
            return;
        }
        this.save(
            this.moderation.publishPolicy({
                textReviewCategories: this.textCategories(),
                imageReviewCategories: this.imageCategories(),
                mistralSamplePercent: samplePercent,
            }),
        );
    }

    protected restore(id: RecipeModerationPolicyId): void {
        if (!this.saving()) {
            this.save(this.moderation.restorePolicy(id));
        }
    }

    private save(operation: Observable<AdminRecipeModerationPolicy>): void {
        this.saving.set(true);
        this.updated.set(false);
        this.updateFailed.set(false);
        operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: policy => {
                this.applyPolicy(policy);
                this.updated.set(true);
                this.saving.set(false);
            },
            error: () => {
                this.updateFailed.set(true);
                this.saving.set(false);
            },
        });
    }

    private applyPolicy(policy: AdminRecipeModerationPolicy): void {
        this.policy.set(policy);
        this.textCategories.set(policy.active.rules.textReviewCategories);
        this.imageCategories.set(policy.active.rules.imageReviewCategories);
        this.samplePercent.set(policy.active.rules.mistralSamplePercent);
    }
}
