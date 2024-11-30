import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Food } from '../../../types/food.data';
import { TuiDialogContext } from '@taiga-ui/core';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'app-base-food-detail',
    templateUrl: './base-food-detail.component.html',
    styleUrls: ['./base-food-detail.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe]
})
export class BaseFoodDetailComponent<T> {
    public readonly context = injectContext<TuiDialogContext<T, Food>>();

    public food: Food;

    public constructor() {
        this.food = this.context.data;
    }
}
