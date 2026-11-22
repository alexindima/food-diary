import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { Subscription } from 'rxjs';

import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminRecipeModerationFacade } from '../lib/admin-recipe-moderation.facade';
import type { AdminRecipeModerationItem } from '../models/admin-recipe-moderation.data';

function formatResults(label: string, raw: string | null): Array<{ label: string; result: string }> {
    if (raw === null || raw.length === 0) {
        return [];
    }

    try {
        const json: unknown = JSON.parse(raw);
        return [{ label, result: JSON.stringify(json, null, 2) }];
    } catch {
        return [{ label, result: raw }];
    }
}

@Component({
    selector: 'fd-admin-recipe-moderation-evidence',
    imports: [FdUiButtonComponent, FdUiFormErrorComponent, TranslatePipe],
    templateUrl: './admin-recipe-moderation-evidence.html',
    styleUrl: './admin-recipe-moderation-evidence.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRecipeModerationEvidenceComponent {
    private readonly moderation = inject(AdminRecipeModerationFacade);
    private readonly destroyRef = inject(DestroyRef);
    private imageRequest?: Subscription;

    public readonly item = input.required<AdminRecipeModerationItem>();
    protected readonly imageLoading = signal(false);
    protected readonly imageFailed = signal(false);
    protected readonly imageUrl = signal<string | null>(null);
    protected readonly results = computed(() => [
        ...formatResults('ADMIN_RECIPE_MODERATION.OPENAI_RESULT', this.item().primaryResult),
        ...formatResults('ADMIN_RECIPE_MODERATION.MISTRAL_RESULT', this.item().shadowResult),
    ]);

    protected showImage(assetId: AdminId<'image-asset'>): void {
        this.closeImage();
        this.imageLoading.set(true);
        this.imageRequest = this.moderation
            .getImage(this.item().revisionId, assetId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: imageUrl => {
                    this.imageUrl.set(imageUrl);
                    this.imageLoading.set(false);
                },
                error: () => {
                    this.imageFailed.set(true);
                    this.imageLoading.set(false);
                },
            });
    }

    protected closeImage(): void {
        this.imageRequest?.unsubscribe();
        this.imageUrl.set(null);
        this.imageLoading.set(false);
        this.imageFailed.set(false);
    }
}
