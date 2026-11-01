import { ChangeDetectionStrategy, Component, computed, inject, input, type TemplateRef } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import {
    FdUiButtonComponent,
    FdUiDialogComponent,
    FdUiDialogService,
    FdUiInlineAlertComponent,
    FdUiMenuComponent,
    FdUiMenuItemComponent,
    FdUiMenuTriggerDirective,
} from 'fd-ui-kit';

import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';
import { PwaInstallService } from '../../../../shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from '../../../../shared/service-worker/pwa-install-promotion.service';

@Component({
    selector: 'fd-public-site-header',
    imports: [
        RouterLink,
        RouterLinkActive,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiInlineAlertComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    templateUrl: './public-site-header.html',
    styleUrl: './public-site-header.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicSiteHeaderComponent {
    private readonly dialogs = inject(FdUiDialogService);
    private readonly promotion = inject(PwaInstallPromotionService);
    protected readonly pwa = inject(PwaInstallService);
    public readonly path = input('/');
    protected readonly locale = computed(() => publicSiteLocale(this.path()));
    protected readonly home = computed(() => localizedPublicPath('/', this.locale()));
    protected readonly links = computed(() =>
        ['food-diary', 'ai-calorie-tracker', 'pricing', 'about'].map(path => ({
            path: localizedPublicPath(`/${path}`, this.locale()),
            label: `PUBLIC_SITE.NAV.${path.replaceAll('-', '_').toUpperCase()}`,
        })),
    );
    protected readonly languages = computed(() => [
        { label: 'EN', lang: 'en', path: localizedPublicPath(this.path(), 'en') },
        { label: 'RU', lang: 'ru', path: localizedPublicPath(this.path(), 'ru') },
    ]);

    protected async installAsync(instructions: TemplateRef<unknown>): Promise<void> {
        if (this.pwa.installMode() === 'ios') {
            this.dialogs.open(instructions, { size: 'sm' });
            return;
        }
        await this.promotion.installAsync();
    }
}
