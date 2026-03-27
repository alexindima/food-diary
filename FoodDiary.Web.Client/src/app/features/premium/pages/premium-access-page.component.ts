import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';

@Component({
    selector: 'fd-premium-access-page',
    standalone: true,
    templateUrl: './premium-access-page.component.html',
    styleUrls: ['./premium-access-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdPageContainerDirective, PageHeaderComponent, FdUiCardComponent, TranslatePipe],
})
export class PremiumAccessPageComponent {}
