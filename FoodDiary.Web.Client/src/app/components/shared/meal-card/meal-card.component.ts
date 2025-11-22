import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { TranslatePipe } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { Consumption } from '../../../types/consumption.data';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';

@Component({
    selector: 'fd-meal-card',
    standalone: true,
    imports: [CommonModule, FdUiEntityCardComponent, TranslatePipe, LocalizedDatePipe],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    public readonly meal = input.required<Consumption>();
    public readonly open = output<Consumption>();

    public handleOpen(): void {
        this.open.emit(this.meal());
    }
}
