import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-premium-access-page',
    standalone: true,
    templateUrl: './premium-access-page.component.html',
    styleUrls: ['./premium-access-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdPageContainerDirective, PageHeaderComponent, FdUiCardComponent, TranslatePipe],
})
export class PremiumAccessPageComponent {}
