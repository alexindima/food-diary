import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { map } from 'rxjs';

import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import type { GoalsMacroDraft } from './goals-editor.models';

@Component({
    selector: 'fd-goals-summary-card',
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiIconComponent],
    templateUrl: './goals-summary-card.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsSummaryCardComponent {
    private readonly translateService = inject(TranslateService);
    public readonly calories = input.required<number>();
    public readonly macros = input.required<GoalsMacroDraft[]>();
    protected readonly calorieMacros = computed(() => this.macros().filter(macro => macro.key !== 'fiber'));
    protected readonly language = toSignal(this.translateService.onLangChange.pipe(map(event => event.lang)), {
        initialValue: resolveTranslateLanguage(this.translateService),
    });
}
