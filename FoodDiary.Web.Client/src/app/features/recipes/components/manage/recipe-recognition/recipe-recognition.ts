import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCheckboxComponent } from 'fd-ui-kit';

import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { RecipeClipboardSuggestion } from '../../../lib/recipe-clipboard-suggestion';
import { RecipeImportFacade } from '../../../lib/recipe-import.facade';
import { RecipeVideoInputComponent } from '../recipe-video-input/recipe-video-input';

@Component({
    selector: 'fd-recipe-recognition',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCheckboxComponent, RecipeVideoInputComponent],
    templateUrl: './recipe-recognition.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RecipeImportFacade, RecipeClipboardSuggestion],
})
export class RecipeRecognitionComponent {
    protected readonly facade = inject(RecipeImportFacade);
    protected readonly clipboard = inject(RecipeClipboardSuggestion);
    public readonly sourceUrl = input('');
    public readonly text = input('');
    public readonly draftRecognized = output<RecipeImportResult>();
    public readonly sourceUrlSuggested = output<string>();
    public readonly busyChanged = output<boolean>();
    protected readonly reviewKeys = signal<string[]>([]);
    protected readonly videoMode = signal(false);
    protected readonly videoFile = signal<File | null>(null);

    protected selectVideo(file: File | null): void {
        this.videoFile.set(file);
        if (file !== null) {
            this.videoMode.set(true);
        }
    }

    protected setVideoMode(enabled: boolean): void {
        this.videoMode.set(enabled);
        if (!enabled) {
            this.videoFile.set(null);
        }
    }

    public constructor() {
        effect(() => {
            this.busyChanged.emit(this.facade.busy());
        });
    }

    protected async recognizeAsync(): Promise<void> {
        this.reviewKeys.set([]);
        const draft = await this.facade.recognizeAsync(this.sourceUrl(), this.text(), this.videoMode(), this.videoFile());
        if (draft !== null) {
            const keys = ['RECIPE_MANAGE.IMPORT.REVIEW_HINT'];
            if (draft.servings === null) {
                keys.push('RECIPE_MANAGE.IMPORT.SERVINGS_HINT');
            }
            if (draft.steps.length === 0) {
                keys.push('RECIPE_MANAGE.IMPORT.STEPS_HINT');
            }
            this.reviewKeys.set(keys);
            this.draftRecognized.emit(draft);
        }
    }

    protected async pasteAsync(): Promise<void> {
        const text = await this.clipboard.pasteAsync();
        if (text !== null) {
            this.sourceUrlSuggested.emit(text);
        }
    }

    protected acceptSuggestion(): void {
        const url = this.clipboard.suggestion();
        if (url !== null) {
            this.sourceUrlSuggested.emit(url);
        }
        this.clipboard.dismiss();
    }

    protected setClipboardEnabled(enabled: boolean): void {
        void this.clipboard.setEnabledAsync(enabled);
    }
}
