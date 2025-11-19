import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'app-consumption-container',
    templateUrl: './consumption-container.component.html',
    styleUrl: './consumption-container.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet]
})
export class ConsumptionContainerComponent {}
