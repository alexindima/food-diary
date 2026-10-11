import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly baseKey = String(this.route.snapshot.data['publicPageKey']);
    protected readonly home = localizedPublicPath('/', publicSiteLocale(this.router.url));
    protected readonly pricingPath = localizedPublicPath('/pricing', publicSiteLocale(this.router.url));
    protected readonly supportEmail = environment.supportEmail;
    protected readonly isPricing = this.baseKey === 'PUBLIC_PAGES.PRICING';
    protected readonly isContact = this.baseKey === 'PUBLIC_PAGES.CONTACT';
    protected readonly sections = ['FIRST', 'SECOND', 'THIRD', 'FOURTH'].map(key => `${this.baseKey}.SECTIONS.${key}`);
    protected readonly comparisonRows = ['DIARY', 'RECIPES', 'GOALS', 'PHOTO', 'VOICE', 'LIMITS'];
}
