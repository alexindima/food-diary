import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { ImageGalleryEditorComponent } from '../../../../../components/shared/image-gallery-editor/image-gallery-editor';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { injectRecipeCategoryOptions } from '../../../lib/recipe-category-options';
import type { RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';

const ERROR_FIELDS = ['name', 'cookTime', 'prepTime', 'servings', 'description', 'visibility', 'comment'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-recipe-basic-info',
    imports: [
        FormField,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        ImageGalleryEditorComponent,
    ],
    templateUrl: './recipe-basic-info.html',
    styleUrls: ['./recipe-basic-info.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeBasicInfoComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly languageVersion = signal(0);

    public readonly form = input.required<FieldTree<RecipeFormValues>>();
    public readonly photosUploading = output<boolean>();
    protected readonly categoryOptions = injectRecipeCategoryOptions();
    protected readonly photos = computed(() => {
        const value = this.form()().value();
        return value.images ?? (value.imageUrl !== null && (value.imageUrl.url?.length ?? 0) > 0 ? [value.imageUrl] : []);
    });
    protected readonly selectedVisibility = computed(
        () => this.visibilitySelectOptions().find(option => option.value === this.form().visibility().value())?.label ?? '',
    );
    protected readonly visibilitySelectOptions = computed<Array<FdUiSelectOption<RecipeVisibility>>>(() => {
        this.languageVersion();

        return Object.values(RecipeVisibility).map(option => ({
            value: option,
            label: this.translateService.instant(`RECIPE_VISIBILITY.${option}`),
        }));
    });
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageVersion();

        return this.buildFieldErrors();
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected setPhotos(images: ImageSelection[]): void {
        const current = this.photos();
        const changed =
            current.length !== images.length ||
            current.some((image, index) => image.url !== images[index]?.url || image.assetId !== images[index]?.assetId);
        this.form()().value.update(value => ({ ...value, images, imageUrl: images[0] ?? null }));
        if (changed) {
            this.form()().markAsDirty();
        }
    }
    protected setCover(image: ImageSelection | null): void {
        if (image !== null) {
            this.setPhotos([image, ...this.photos().filter(item => item !== image)]);
        }
    }

    private buildFieldErrors(): FieldErrors {
        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = this.getControlError(field);
            return errors;
        }, this.createEmptyFieldErrors());
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            name: null,
            cookTime: null,
            prepTime: null,
            servings: null,
            description: null,
            visibility: null,
            comment: null,
        };
    }

    private getControlError(controlName: ErrorField): string | null {
        return resolveSignalFormFieldError(this.form()[controlName], this.validationErrors, this.translateService);
    }
}
