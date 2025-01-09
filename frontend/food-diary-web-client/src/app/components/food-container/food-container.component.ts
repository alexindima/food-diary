import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-food-container',
    templateUrl: './food-container.component.html',
    styleUrls: ['./food-container.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet]
})
export class FoodContainerComponent {}
