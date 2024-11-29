import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
    selector: 'app-consumption-container',
    standalone: true,
    templateUrl: './consumption-container.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterOutlet],
})
export class ConsumptionContainerComponent {}
