import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-recipe-container',
    imports: [
        RouterOutlet
    ],
    templateUrl: './recipe-container.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecipeContainerComponent {

}
