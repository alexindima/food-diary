import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent } from 'fd-ui-kit';

import { environment } from '../../../../../environments/environment';
import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';

@Component({
    selector: 'fd-public-article-page',
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './public-article-page.html',
    styleUrl: './public-article-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicArticlePageComponent {
    public readonly publicPageKey = input.required<string>();

    private readonly router = inject(Router);
    protected readonly home = localizedPublicPath('/', publicSiteLocale(this.router.url));
    protected readonly pricingPath = localizedPublicPath('/pricing', publicSiteLocale(this.router.url));
    protected readonly supportEmail = environment.supportEmail;
    protected readonly isPricing = computed(() => this.publicPageKey() === 'PUBLIC_PAGES.PRICING');
    protected readonly isContact = computed(() => this.publicPageKey() === 'PUBLIC_PAGES.CONTACT');
    protected readonly sections = computed(() => {
        const baseKey = this.publicPageKey();
        return ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => `${baseKey}.SECTIONS.${key}`);
    });
    protected readonly comparisonRows = ['DIARY', 'RECIPES', 'GOALS', 'PHOTO', 'VOICE', 'LIMITS'];
}
