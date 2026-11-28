import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent, FdUiTextareaComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

import { adminAiPromptFailureKey } from '../lib/admin-ai-prompt-failure';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPromptDraft, AdminAiPromptKey } from '../models/admin-ai-prompt-scenario';

const DEFAULT_SAMPLE_AMOUNT = 100;
const MAX_IMAGE_BYTES = 20_971_520;

@Component({
    selector: 'fd-admin-ai-prompt-workbench',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent, FdUiTextareaComponent],
    templateUrl: './admin-ai-prompt-workbench.html',
    styleUrl: './admin-ai-prompt-workbench.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiPromptWorkbenchComponent {
    public readonly scenarioKey = input.required<AdminAiPromptKey>();
    public readonly locale = input.required<string>();
    public readonly promptText = input.required<string>();
    public readonly verified = output<string>();
    private readonly api = inject(AdminAiPromptsFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly context = computed(() => ({ key: this.scenarioKey(), locale: this.locale() }));
    protected readonly text = linkedSignal({ source: this.context, computation: () => '' });
    protected readonly foodName = linkedSignal({ source: this.context, computation: () => '' });
    protected readonly amount = signal(DEFAULT_SAMPLE_AMOUNT);
    protected readonly unit = signal('g');
    protected readonly imageAssetId = linkedSignal({ source: this.context, computation: (): string | undefined => undefined });
    protected readonly imageName = linkedSignal({ source: this.context, computation: () => '' });
    protected readonly uploading = signal(false);
    protected readonly busy = signal(false);
    private readonly draft = computed<AdminAiPromptDraft>(() => ({
        ...this.context(),
        promptText: this.promptText(),
        text: this.text(),
        imageAssetId: this.imageAssetId(),
        foodName: this.foodName(),
        amount: this.amount(),
        unit: this.unit(),
    }));
    private readonly checkedPreview = linkedSignal({ source: this.draft, computation: (): string | null => null });
    protected readonly preview = computed(() => this.checkedPreview() ?? '');
    protected readonly result = linkedSignal({ source: this.draft, computation: () => '' });
    protected readonly errorKey = linkedSignal({ source: this.draft, computation: () => '' });
    protected readonly requiresImage = computed(() => this.scenarioKey() === 'vision' || this.scenarioKey() === 'product-label');
    protected readonly canTest = computed(() => this.checkedPreview() !== null && (!this.requiresImage() || Boolean(this.imageAssetId())));

    protected async inspectAsync(run: boolean): Promise<void> {
        if (this.busy() || (run && !this.canTest())) {
            return;
        }
        const draft = this.draft();
        this.busy.set(true);
        this.errorKey.set('');
        try {
            const response = await firstValueFrom(
                (run ? this.api.test(draft) : this.api.preview(draft)).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            if (draft !== this.draft()) {
                return;
            }
            if (run) {
                this.result.set(response.text);
            } else {
                this.checkedPreview.set(response.text);
                this.verified.emit(draft.promptText);
            }
        } catch (error: unknown) {
            if (draft === this.draft()) {
                this.errorKey.set(adminAiPromptFailureKey(error, run));
            }
        } finally {
            this.busy.set(false);
        }
    }

    protected async uploadAsync(file: File | null | undefined): Promise<void> {
        if (file === null || file === undefined || this.uploading()) {
            return;
        }
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > MAX_IMAGE_BYTES) {
            this.errorKey.set('ADMIN_PROMPTS.IMAGE_ERROR');
            return;
        }
        const context = this.context();
        this.uploading.set(true);
        this.imageAssetId.set(undefined);
        this.imageName.set('');
        try {
            const response = await firstValueFrom(this.api.uploadImage(file).pipe(takeUntilDestroyed(this.destroyRef)));
            if (context !== this.context()) {
                return;
            }
            this.imageAssetId.set(response.assetId);
            this.imageName.set(file.name);
        } catch {
            if (context === this.context()) {
                this.errorKey.set('ADMIN_PROMPTS.IMAGE_ERROR');
            }
        } finally {
            this.uploading.set(false);
        }
    }
}
