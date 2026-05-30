import { ChangeDetectionStrategy, Component, output } from '@angular/core';

@Component({
    selector: 'fd-card',
    templateUrl: './card.html',
    styleUrl: './card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardComponent {
    public readonly cardClick = output();

    protected onCardClick(): void {
        this.cardClick.emit();
    }
}
