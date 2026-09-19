import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, max, maxLength, min, pattern, readonly, required, validate } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, FdUiTextareaComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

import { AdminDailyAdvicesFacade } from '../lib/admin-daily-advices.facade';
import type { AdminDailyAdvice } from '../models/admin-daily-advice.models';

const TEXT_MAX_LENGTH = 512;
const TAG_MAX_LENGTH = 64;
const WEIGHT_MAX = 2147483647;

@Component({
    selector: 'fd-admin-daily-advice-group-card',
    imports: [TranslatePipe, FormField, FormRoot, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, FdUiTextareaComponent],
    templateUrl: './daily-advice-group-card.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyAdviceGroupCardComponent {
    private readonly api = inject(AdminDailyAdvicesFacade);
    private readonly destroyRef = inject(DestroyRef);
    public readonly advice = input.required<AdminDailyAdvice>();
    public readonly changed = output();
    protected readonly editing = signal(false);
    protected readonly confirmingDelete = signal(false);
    protected readonly busy = signal(false);
    protected readonly error = signal(false);
    protected readonly formModel = signal({ ru: '', en: '', tag: '', weight: 1 });
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.saveAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.ru);
            required(path.en);
            pattern(path.ru, /\S/);
            pattern(path.en, /\S/);
            maxLength(path.ru, TEXT_MAX_LENGTH);
            maxLength(path.en, TEXT_MAX_LENGTH);
            maxLength(path.tag, TAG_MAX_LENGTH);
            min(path.weight, 1);
            max(path.weight, WEIGHT_MAX);
            validate(path.weight, context => (Number.isInteger(context.value()) ? null : { kind: 'integer' }));
            readonly(path, { when: () => this.busy() });
        },
        { submission: { action: this.submitFormAsync } },
    );

    protected edit(): void {
        const advice = this.advice();
        this.formModel.set({ ru: advice.ru ?? '', en: advice.en ?? '', tag: advice.tag ?? '', weight: advice.weight });
        this.error.set(false);
        this.confirmingDelete.set(false);
        this.editing.set(true);
    }

    protected async saveAsync(): Promise<void> {
        if (this.form().invalid() || this.busy()) {
            return;
        }
        const value = this.formModel();
        this.busy.set(true);
        this.error.set(false);
        try {
            await firstValueFrom(
                this.api
                    .update(this.advice().id, {
                        ...value,
                        ru: value.ru.trim(),
                        en: value.en.trim(),
                        tag: value.tag.trim().length > 0 ? value.tag.trim() : null,
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.editing.set(false);
            this.changed.emit();
        } catch {
            this.error.set(true);
        } finally {
            this.busy.set(false);
        }
    }

    protected delete(): void {
        if (this.busy() || !this.confirmingDelete()) {
            return;
        }
        this.busy.set(true);
        this.error.set(false);
        this.api
            .delete(this.advice().id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.busy.set(false);
                    this.changed.emit();
                },
                error: () => {
                    this.busy.set(false);
                    this.error.set(true);
                },
            });
    }
}
