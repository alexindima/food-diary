import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { AiConsentDialogComponent } from '../../../components/shared/ai-consent-dialog/ai-consent-dialog';
import { AuthService } from '../../../services/auth.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { UserFacade } from '../../../shared/lib/user.facade';
import type { RecipeImportResult } from '../../../shared/models/recipe-import.data';

const MAXIMUM_VIDEO_BYTES = 52_428_800;
const MAXIMUM_VIDEO_CAPTION_CHARACTERS = 8000;

@Injectable()
export class RecipeImportFacade {
    private readonly ai = inject(AiFoodService);
    private readonly auth = inject(AuthService);
    private readonly user = inject(UserFacade);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    public readonly busy = signal(false);
    public readonly errorKey = signal<string | null>(null);

    public async recognizeAsync(
        sourceUrl: string,
        text: string,
        videoMode = false,
        video: File | null = null,
    ): Promise<RecipeImportResult | null> {
        if (this.busy()) {
            return null;
        }
        this.errorKey.set(null);
        const validationError = recipeInputError(sourceUrl, text, videoMode, video);
        if (validationError !== null) {
            this.errorKey.set(validationError);
            return null;
        }
        this.busy.set(true);
        try {
            if (!(await this.ensureAccessAsync()) || this.destroyRef.destroyed) {
                return null;
            }
            const request = { sourceUrl: nullableText(sourceUrl), text: nullableText(text) };
            const recognition = videoMode ? this.ai.importRecipeVideo(request, video) : this.ai.importRecipe(request);
            return await firstValueFrom(recognition.pipe(takeUntilDestroyed(this.destroyRef)));
        } catch (error: unknown) {
            if (!this.destroyRef.destroyed) {
                this.errorKey.set(recipeImportErrorKey(error));
            }
            return null;
        } finally {
            this.busy.set(false);
        }
    }

    private async ensureAccessAsync(): Promise<boolean> {
        if (!this.auth.isPremium()) {
            this.errorKey.set('RECIPE_MANAGE.IMPORT.PREMIUM_REQUIRED');
            return false;
        }
        const acceptedAt = this.user.user()?.aiConsentAcceptedAt;
        if (acceptedAt !== null && acceptedAt !== undefined) {
            return true;
        }
        const accepted = await firstValueFrom(
            this.dialogs
                .open<AiConsentDialogComponent, never, boolean>(AiConsentDialogComponent, { size: 'lg', disableClose: true })
                .afterClosed()
                .pipe(takeUntilDestroyed(this.destroyRef)),
        );
        if (accepted !== true || this.destroyRef.destroyed) {
            return false;
        }
        await firstValueFrom(this.user.acceptAiConsent().pipe(takeUntilDestroyed(this.destroyRef)));
        return true;
    }
}

function nullableText(text: string): string | null {
    return text.trim().length > 0 ? text.trim() : null;
}

function recipeInputError(sourceUrl: string, text: string, videoMode: boolean, video: File | null): string | null {
    if (videoMode) {
        return videoInputError(sourceUrl, text, video);
    }
    return sourceUrl.trim().length === 0 && text.trim().length === 0 ? 'RECIPE_MANAGE.IMPORT.EMPTY_ERROR' : null;
}

function videoInputError(sourceUrl: string, text: string, video: File | null): string | null {
    if (video === null && sourceUrl.trim().length === 0) {
        return 'RECIPE_MANAGE.IMPORT.VIDEO_REQUIRED';
    }
    if (text.length > MAXIMUM_VIDEO_CAPTION_CHARACTERS) {
        return 'RECIPE_MANAGE.IMPORT.VIDEO_INVALID';
    }
    if (video !== null && video.size > MAXIMUM_VIDEO_BYTES) {
        return 'RECIPE_MANAGE.IMPORT.VIDEO_INVALID';
    }
    return null;
}

function recipeImportErrorKey(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
        const body: unknown = error.error;
        if (typeof body === 'object' && body !== null && 'error' in body) {
            if (body.error === 'Ai.InvalidRecipeVideo') {
                return 'RECIPE_MANAGE.IMPORT.VIDEO_INVALID';
            }
            if (body.error === 'Ai.VideoBusy') {
                return 'RECIPE_MANAGE.IMPORT.VIDEO_BUSY';
            }
        }
        const key = recipeImportHttpErrors[String(error.status)];
        if (key !== undefined) {
            return key;
        }
    }
    return 'RECIPE_MANAGE.IMPORT.RECOGNITION_ERROR';
}

const recipeImportHttpErrors: Readonly<Partial<Record<string, string>>> = {
    '429': 'RECIPE_MANAGE.IMPORT.QUOTA_ERROR',
    '502': 'RECIPE_MANAGE.IMPORT.SOURCE_ERROR',
    '400': 'RECIPE_MANAGE.IMPORT.INVALID_ERROR',
    '403': 'RECIPE_MANAGE.IMPORT.ACCESS_ERROR',
    '413': 'RECIPE_MANAGE.IMPORT.VIDEO_INVALID',
};
