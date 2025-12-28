import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { TranslatePipe } from '@ngx-translate/core';
import { DailyAdvice } from '../../../types/daily-advice.data';
import { FdCardHoverDirective } from '../../../directives/card-hover.directive';

@Component({
    selector: 'fd-daily-advice-card',
    standalone: true,
    imports: [CommonModule, FdUiIconModule, TranslatePipe, FdCardHoverDirective],
    templateUrl: './daily-advice-card.component.html',
    styleUrl: './daily-advice-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyAdviceCardComponent {
    public readonly advice = input<DailyAdvice | null>(null);
    public readonly isLoading = input<boolean>(false);

    public tagLabel(): string | null {
        const tag = this.advice()?.tag;
        if (!tag) {
            return null;
        }

        return tag.replace(/_/g, ' ').replace(/\b\w/g, letter => letter.toUpperCase());
    }
}
