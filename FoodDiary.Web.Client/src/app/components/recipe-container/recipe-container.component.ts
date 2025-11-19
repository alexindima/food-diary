import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-recipe-container',
    templateUrl: './recipe-container.component.html',
    styleUrls: ['./recipe-container.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class RecipeContainerComponent {}
