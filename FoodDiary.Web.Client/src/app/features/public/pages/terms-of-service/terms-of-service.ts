import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { environment } from '../../../../../environments/environment';
import { localizedPublicPath, publicSiteLocale } from '../../../../config/public-site-paths';

@Component({
    selector: 'fd-terms-of-service',
    imports: [RouterLink, TranslatePipe],
    templateUrl: './terms-of-service.html',
    styleUrls: ['../public-article/public-article-page.scss', './terms-of-service.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsOfServiceComponent {
    private readonly locale = publicSiteLocale(inject(Router).url);
    protected readonly privacyPath = localizedPublicPath('/privacy-policy', this.locale);
    protected readonly supportEmail = environment.supportEmail;
    protected readonly operator = {
        name: 'DMITRII ALEKSIN',
        id: '304772173',
        address: 'Georgia, Tbilisi, Krtsanisi district, Nino and Ilia Nakashidzeebi street, N 1 (former Avlevi), flat N3, building N3',
    };
    protected readonly sections = ['ACCOUNT', 'CONTENT', 'SUBSCRIPTIONS', 'AI', 'CHATGPT', 'CHANGES', 'CONSUMER_RIGHTS'];
}
