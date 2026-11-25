import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';

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
    private readonly imageRequest = signal<
        { revisionId: AdminRecipeModerationItem['revisionId']; assetId: AdminId<'image-asset'> } | undefined
    >(undefined);
    private readonly imageResource = rxResource({
        params: this.imageRequest,
        stream: ({ params }) => this.moderation.getImage(params.revisionId, params.assetId),
    });

    public readonly item = input.required<AdminRecipeModerationItem>();
    protected readonly imageLoading = this.imageResource.isLoading;
    protected readonly imageFailed = computed(() => this.imageResource.error() !== undefined);
    protected readonly imageUrl = computed(() => (this.imageResource.hasValue() ? this.imageResource.value() : null));
    protected readonly results = computed(() => [
        ...formatResults('ADMIN_RECIPE_MODERATION.OPENAI_RESULT', this.item().primaryResult),
        ...formatResults('ADMIN_RECIPE_MODERATION.MISTRAL_RESULT', this.item().shadowResult),
    ]);

    protected showImage(assetId: AdminId<'image-asset'>): void {
        this.imageRequest.set({ revisionId: this.item().revisionId, assetId });
    }

    protected closeImage(): void {
        this.imageRequest.set(undefined);
    }
}
