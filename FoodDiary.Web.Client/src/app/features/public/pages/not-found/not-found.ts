import { ChangeDetectionStrategy, Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { localizedPublicPath } from '../../../../config/public-site-paths';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';

@Component({
    selector: 'fd-not-found',
    imports: [RouterLink, FdUiButtonComponent, TranslatePipe],
    templateUrl: './not-found.html',
    styleUrl: './not-found.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {
    private readonly language = injectCurrentLanguage();
    protected readonly homePath = computed(() => localizedPublicPath('/', this.language().startsWith('ru') ? 'ru' : 'en'));
    protected readonly diaryPath = computed(() => localizedPublicPath('/food-diary', this.language().startsWith('ru') ? 'ru' : 'en'));
}
