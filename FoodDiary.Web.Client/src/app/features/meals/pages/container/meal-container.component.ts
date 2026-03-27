import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-meal-container',
    templateUrl: './meal-container.component.html',
    styleUrl: './meal-container.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class MealContainerComponent {}
