import { ChangeDetectionStrategy, Component, output } from '@angular/core';

@Component({
  selector: 'fd-card',
  templateUrl: './card.component.html',
  styleUrl: './card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardComponent {
    public cardClick = output<void>();

    public onCardClick(): void {
        this.cardClick.emit();
    }
}
