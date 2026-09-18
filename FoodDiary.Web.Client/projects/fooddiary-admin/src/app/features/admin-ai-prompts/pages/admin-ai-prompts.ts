import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength, readonly, required } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiCheckboxComponent,
    FdUiConfirmDialogComponent,
    FdUiDialogService,
    FdUiSelectComponent,
    FdUiTextareaComponent,
} from 'fd-ui-kit';
import { firstValueFrom, map, type Observable, of } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminTemplateHistoryComponent } from '../../admin-template-history/components/admin-template-history';
import type { AdminTemplateRevision } from '../../admin-template-history/models/admin-template-revision';
import { AdminAiPromptContextComponent } from '../components/admin-ai-prompt-context';
import { AdminAiPromptVariablesComponent } from '../components/admin-ai-prompt-variables';
import { AdminAiPromptWorkbenchComponent } from '../components/admin-ai-prompt-workbench';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPromptKey, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';

const PROMPT_MAX_LENGTH = 4096;

@Component({
    selector: 'fd-admin-ai-prompts',
    imports: [
        FdUiSelectComponent,
        FdUiCheckboxComponent,
        FdUiTextareaComponent,
        AdminTemplateHistoryComponent,
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiButtonComponent,
        AdminLoadErrorComponent,
        AdminAiPromptWorkbenchComponent,
        AdminAiPromptVariablesComponent,
        AdminAiPromptContextComponent,
    ],
    templateUrl: './admin-ai-prompts.html',
    styleUrl: './admin-ai-prompts.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:beforeunload)': 'onBeforeUnload($event)' },
})
export class AdminAiPromptsPageComponent {
    private readonly api = inject(AdminAiPromptsFacade);
    private readonly languageSelect = viewChild(FdUiSelectComponent);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    protected readonly keys: AdminAiPromptKey[] = ['vision', 'text-parse', 'nutrition'];
    protected readonly items = signal<AdminAiPromptScenario[]>([]);
    protected readonly key = signal<AdminAiPromptKey>('vision');
    protected readonly locale = signal('en');
    protected readonly selected = computed(() => this.items().find(item => item.key === this.key() && item.locale === this.locale()));
    protected readonly loading = signal(false);
    protected readonly failed = signal(false);
    protected readonly saving = signal(false);
    protected readonly saveFailed = signal(false);
    protected readonly saved = signal(false);
    protected readonly verifiedText = signal<string | null>(null);
    protected readonly formModel = signal({ promptText: '', isActive: false });
    private readonly baseline = signal(JSON.stringify(this.formModel()));
    private readonly dirty = computed(() => JSON.stringify(this.formModel()) !== this.baseline());
    protected readonly draftText = computed(() =>
        this.formModel().isActive ? this.formModel().promptText : (this.selected()?.inheritedPromptText ?? ''),
    );
    protected readonly canApply = computed(() => this.dirty() && this.verifiedText() === this.draftText() && !this.form().invalid());
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.saveAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            required(path.promptText);
            readonly(path.promptText, { when: () => this.saving() });
            maxLength(path.promptText, PROMPT_MAX_LENGTH);
        },
        {
            submission: {
                action: this.submitFormAsync,
            },
        },
    );

    public constructor() {
        this.load();
    }
    protected load(): void {
        this.loading.set(true);
        this.failed.set(false);
        this.api
            .getScenarios()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: items => {
                    this.items.set(items);
                    this.resetEditor();
                    this.loading.set(false);
                },
                error: () => {
                    this.failed.set(true);
                    this.loading.set(false);
                },
            });
    }
    protected selectScenario(key: AdminAiPromptKey, locale: string): void {
        if (key === this.key() && locale === this.locale()) {
            return;
        }
        this.canLeave()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(allowed => {
                if (!allowed) {
                    this.languageSelect()?.value.set(this.locale());
                    return;
                }
                this.key.set(key);
                this.locale.set(locale);
                this.saved.set(false);
                this.saveFailed.set(false);
                this.resetEditor();
            });
    }
    protected setCustom(active: boolean): void {
        this.formModel.update(value => ({ ...value, isActive: active }));
        this.verifiedText.set(null);
        this.saved.set(false);
    }
    private resetEditor(): void {
        const item = this.selected();
        this.formModel.set({
            promptText: item?.template?.promptText ?? item?.promptText ?? '',
            isActive: item?.template?.isActive ?? false,
        });
        this.baseline.set(JSON.stringify(this.formModel()));
        this.verifiedText.set(null);
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
        this.formModel.set({ promptText: revision.textBody, isActive: revision.isActive });
        this.verifiedText.set(null);
        this.saved.set(false);
    }
    protected async saveAsync(): Promise<void> {
        if (!this.canApply() || this.saving()) {
            return;
        }
        this.saving.set(true);
        this.saveFailed.set(false);
        this.saved.set(false);
        try {
            await firstValueFrom(
                this.api
                    .save(this.key(), this.locale(), this.formModel().promptText, this.formModel().isActive)
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.baseline.set(JSON.stringify(this.formModel()));
            this.verifiedText.set(null);
            this.saved.set(true);
            this.load();
        } catch {
            this.saveFailed.set(true);
        } finally {
            this.saving.set(false);
        }
    }
}
