import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-recipe-container',
    templateUrl: './recipe-container.html',
    styleUrls: ['./recipe-container.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class RecipeContainerComponent {}
