import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import type { PublicRecipe } from '../../models/public-recipe.data';
import { PublicRecipeGalleryComponent } from '../public-gallery/public-gallery';

@Component({
    selector: 'fd-public-steps',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-steps.html',
    styleUrl: './public-steps.scss',
    imports: [TranslatePipe, FdUiButtonComponent, PublicRecipeGalleryComponent],
})
export class PublicStepsComponent {
    public readonly recipe = input.required<PublicRecipe>();
    protected readonly cooking = signal(false);
    protected readonly stepIndex = signal(0);
    protected readonly visibleSteps = computed(() =>
        this.cooking() ? this.recipe().steps.slice(this.stepIndex(), this.stepIndex() + 1) : this.recipe().steps,
    );
    protected toggleCooking(): void {
        this.cooking.update(value => !value);
        this.stepIndex.set(0);
    }
    protected moveStep(delta: number): void {
        this.stepIndex.update(value => Math.max(0, Math.min(this.recipe().steps.length - 1, value + delta)));
    }
}
