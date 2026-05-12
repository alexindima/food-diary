import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-weekly-check-in-suggestions-card',
    standalone: true,
    imports: [TranslatePipe, FdUiIconComponent, FdUiCardComponent],
    templateUrl: './weekly-check-in-suggestions-card.component.html',
    styleUrl: './weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyCheckInSuggestionsCardComponent {
    public readonly suggestions = input.required<WeeklyCheckInSuggestionViewModel[]>();
}

interface WeeklyCheckInSuggestionViewModel {
    key: string;
    labelKey: string;
}
