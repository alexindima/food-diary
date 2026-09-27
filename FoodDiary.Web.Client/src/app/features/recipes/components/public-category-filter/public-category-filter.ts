import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiSelectComponent } from 'fd-ui-kit';

import { injectRecipeCategoryOptions } from '../../lib/recipe-category-options';

@Component({
    selector: 'fd-public-category-filter',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiSelectComponent],
    templateUrl: './public-category-filter.html',
})
export class PublicCategoryFilterComponent {
    public readonly category = model('');
    protected readonly options = injectRecipeCategoryOptions(true);
}
