import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength, pattern, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiConfirmDialogComponent, FdUiDialogService, FdUiInputComponent } from 'fd-ui-kit';
import { firstValueFrom, map, type Observable, of } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminTemplateHistoryComponent } from '../../admin-template-history/components/admin-template-history';
import type { AdminTemplateRevision } from '../../admin-template-history/models/admin-template-revision';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';

const PROMPT_KEY_MAX_LENGTH = 64;
const PROMPT_TEXT_MAX_LENGTH = 4096;

@Component({
    selector: 'fd-admin-ai-prompts',
    imports: [
        AdminTemplateHistoryComponent,
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        AdminLoadErrorComponent,
    ],
    templateUrl: './admin-ai-prompts.html',
    styleUrl: './admin-ai-prompts.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:beforeunload)': 'onBeforeUnload($event)' },
})
export class AdminAiPromptsPageComponent {
    private readonly api = inject(AdminAiPromptsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    protected readonly items = signal<AdminAiPrompt[]>([]);
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);
    protected readonly saving = signal(false);
    protected readonly saveFailed = signal(false);
    protected readonly saved = signal(false);
    protected readonly search = signal('');
    protected readonly selected = signal<AdminAiPrompt | null>(null);
    protected readonly filtered = computed(() =>
        this.items().filter(item => `${item.key} ${item.locale}`.toLowerCase().includes(this.search().toLowerCase())),
    );
    protected readonly formModel = signal({ key: '', locale: 'en', promptText: '', isActive: true });
    private readonly baseline = signal(JSON.stringify(this.formModel()));
    private readonly dirty = computed(() => JSON.stringify(this.formModel()) !== this.baseline());
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.saveAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.key);
            maxLength(path.key, PROMPT_KEY_MAX_LENGTH);
            required(path.locale);
            pattern(path.locale, /^(en|ru)$/);
            required(path.promptText);
            maxLength(path.promptText, PROMPT_TEXT_MAX_LENGTH);
        },
        { submission: { action: this.submitFormAsync } },
    );

    public constructor() {
        this.load();
    }
    protected load(): void {
        this.loading.set(true);
        this.failed.set(false);
        this.api
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: items => {
                    this.items.set(items);
                    this.loading.set(false);
                },
                error: () => {
                    this.failed.set(true);
                    this.loading.set(false);
                },
            });
    }
    protected edit(item: AdminAiPrompt | null): void {
        this.canLeave()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(allowed => {
                if (!allowed) {
                    return;
                }
                this.selected.set(item);
                this.saved.set(false);
                this.saveFailed.set(false);
                this.setEditor(item);
            });
    }
    private setEditor(item: AdminAiPrompt | null): void {
        this.formModel.set({
            key: item?.key ?? '',
            locale: item?.locale ?? 'en',
            promptText: item?.promptText ?? '',
            isActive: item?.isActive ?? true,
        });
        this.baseline.set(JSON.stringify(this.formModel()));
    }
    // Router canDeactivate calls this contract outside the component template.
    // eslint-disable-next-line local/prefer-protected-template-members -- Router canDeactivate invokes this public contract.
    public canLeave(): Observable<boolean> {
        if (this.saving()) {
            return of(false);
        }
        if (!this.dirty()) {
            return of(true);
        }
        return this.dialogs
            .open(FdUiConfirmDialogComponent, {
                size: 'sm',
                data: {
                    title: String(this.translate.instant('ADMIN_PROMPTS.UNSAVED_TITLE')),
                    message: String(this.translate.instant('ADMIN_PROMPTS.UNSAVED_MESSAGE')),
                    confirmLabel: String(this.translate.instant('ADMIN_PROMPTS.DISCARD')),
                },
            })
            .afterClosed()
            .pipe(map(result => result === true));
    }
    protected onBeforeUnload(event: BeforeUnloadEvent): void {
        if (this.dirty()) {
            event.preventDefault();
        }
    }
    protected restoreRevision(revision: AdminTemplateRevision): void {
        this.formModel.update(value => ({ ...value, promptText: revision.textBody, isActive: revision.isActive }));
        this.saved.set(false);
    }
    protected async saveAsync(): Promise<void> {
        if (this.form().invalid() || this.saving()) {
            return;
        }
        this.saving.set(true);
        this.saveFailed.set(false);
        this.saved.set(false);
        const value = this.formModel();
        try {
            const item = await firstValueFrom(
                this.api.save(value.key.trim(), value.locale, value.promptText, value.isActive).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.selected.set(item);
            this.setEditor(item);
            this.saved.set(true);
            this.load();
        } catch {
            this.saveFailed.set(true);
        } finally {
            this.saving.set(false);
        }
    }
}
