import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent, FdUiTextareaComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

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
    protected readonly text = signal('');
    protected readonly foodName = signal('');
    protected readonly amount = signal(DEFAULT_SAMPLE_AMOUNT);
    protected readonly unit = signal('g');
    protected readonly imageAssetId = signal<string | undefined>(undefined);
    protected readonly imageName = signal('');
    protected readonly uploading = signal(false);
    protected readonly busy = signal(false);
    protected readonly errorKey = signal('');
    protected readonly preview = signal('');
    protected readonly result = signal('');
    private readonly revision = computed(() => JSON.stringify(this.draft()));
    private readonly verifiedRevision = signal('');
    protected readonly requiresImage = computed(() => this.scenarioKey() === 'vision' || this.scenarioKey() === 'product-label');
    protected readonly canTest = computed(
        () => this.verifiedRevision() === this.revision() && (!this.requiresImage() || Boolean(this.imageAssetId())),
    );
    protected readonly draft = computed<AdminAiPromptDraft>(() => ({
        key: this.scenarioKey(),
        locale: this.locale(),
        promptText: this.promptText(),
        text: this.text(),
        imageAssetId: this.imageAssetId(),
        foodName: this.foodName(),
        amount: this.amount(),
        unit: this.unit(),
    }));

    public constructor() {
        effect(() => {
            this.scenarioKey();
            this.locale();
            untracked(() => {
                this.text.set('');
                this.foodName.set('');
                this.imageAssetId.set(undefined);
                this.imageName.set('');
            });
        });
        effect(() => {
            this.revision();
            this.preview.set('');
            this.result.set('');
            this.verifiedRevision.set('');
            this.errorKey.set('');
        });
    }

    protected async inspectAsync(run: boolean): Promise<void> {
        if (this.busy() || (run && !this.canTest())) {
            return;
        }
        const revision = this.revision();
        const draft = this.draft();
        this.busy.set(true);
        this.errorKey.set('');
        try {
            const response = await firstValueFrom(
                (run ? this.api.test(draft) : this.api.preview(draft)).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            if (revision !== this.revision()) {
                return;
            }
            if (run) {
                this.result.set(response.text);
            } else {
                this.preview.set(response.text);
                this.verifiedRevision.set(revision);
                this.verified.emit(draft.promptText);
            }
        } catch (error: unknown) {
            if (revision === this.revision()) {
                this.handleFailure(error, run);
            }
        } finally {
            this.busy.set(false);
        }
    }

    private handleFailure(error: unknown, run: boolean): void {
        if (error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.TooManyRequests)) {
            this.errorKey.set('ADMIN_PROMPTS.TEST_LIMIT');
            return;
        }
        this.errorKey.set(run ? 'ADMIN_PROMPTS.TEST_ERROR' : 'ADMIN_PROMPTS.PREVIEW_ERROR');
    }

    protected async uploadAsync(file: File | null | undefined): Promise<void> {
        if (file === null || file === undefined || this.uploading()) {
            return;
        }
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > MAX_IMAGE_BYTES) {
            this.errorKey.set('ADMIN_PROMPTS.IMAGE_ERROR');
            return;
        }
        const identity = `${this.scenarioKey()}:${this.locale()}`;
        this.uploading.set(true);
        this.imageAssetId.set(undefined);
        this.imageName.set('');
        try {
            const response = await firstValueFrom(this.api.uploadImage(file).pipe(takeUntilDestroyed(this.destroyRef)));
            if (identity !== `${this.scenarioKey()}:${this.locale()}`) {
                return;
            }
            this.imageAssetId.set(response.assetId);
            this.imageName.set(file.name);
        } catch {
            if (identity === `${this.scenarioKey()}:${this.locale()}`) {
                this.errorKey.set('ADMIN_PROMPTS.IMAGE_ERROR');
            }
        } finally {
            this.uploading.set(false);
        }
    }
}
