import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'fd-meal-container',
    templateUrl: './meal-container.html',
    styleUrl: './meal-container.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class MealContainerComponent {}
