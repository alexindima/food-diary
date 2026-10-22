import { ChangeDetectionStrategy, Component, computed, effect, inject, input, linkedSignal, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCheckboxComponent, FdUiInlineAlertComponent } from 'fd-ui-kit';

import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { RecipeClipboardSuggestion } from '../../../lib/recipe-clipboard-suggestion';
import { RecipeImportFacade } from '../../../lib/recipe-import.facade';
import { RecipeVideoInputComponent } from '../recipe-video-input/recipe-video-input';

@Component({
    selector: 'fd-recipe-recognition',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCheckboxComponent, FdUiInlineAlertComponent, RecipeVideoInputComponent],
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
    protected readonly videoFile = signal<File | null>(null);
    private readonly videoModeOverride = linkedSignal<string, boolean | null>({
        source: () => this.sourceUrl().trim(),
        computation: (_sourceUrl, previous) => (previous?.value === true ? true : null),
    });
    protected readonly videoMode = computed(
        () =>
            this.videoModeOverride() ??
            (this.videoFile() !== null || (isYouTubeVideoUrl(this.sourceUrl()) && this.text().trim().length === 0)),
    );

    protected selectVideo(file: File | null): void {
        this.videoFile.set(file);
        if (file !== null) {
            this.videoModeOverride.set(true);
        }
    }

    protected setVideoMode(enabled: boolean): void {
        this.videoModeOverride.set(enabled);
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

function isYouTubeVideoUrl(source: string): boolean {
    try {
        const url = new URL(source.trim());
        if (!hasYouTubeVideoAuthority(url)) {
            return false;
        }
        if (url.hostname === 'youtu.be') {
            return /^\/[\w-]{11}\/?$/.test(url.pathname);
        }
        if (/^\/shorts\/[\w-]{11}\/?$/.test(url.pathname)) {
            return true;
        }
        const ids = url.searchParams.getAll('v');
        return url.pathname === '/watch' && ids.length === 1 && /^[\w-]{11}$/.test(ids[0] ?? '');
    } catch {
        return false;
    }
}

function hasYouTubeVideoAuthority(url: URL): boolean {
    return (
        url.protocol === 'https:' &&
        url.port === '' &&
        url.username === '' &&
        url.password === '' &&
        ['youtube.com', 'www.youtube.com', 'm.youtube.com', 'youtu.be'].includes(url.hostname)
    );
}
