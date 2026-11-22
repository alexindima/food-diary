import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { RecipePublicationStatus } from '../../../../../shared/models/recipe.data';

@Component({
    selector: 'fd-recipe-publication-notice',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './recipe-publication-notice.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipePublicationNoticeComponent {
    public readonly status = input.required<RecipePublicationStatus | 'Published' | null>();
    public readonly reason = input<string | null>(null);
    public readonly loading = input(false);
    public readonly refreshFailed = input(false);
    public readonly refreshRequested = output();
}
