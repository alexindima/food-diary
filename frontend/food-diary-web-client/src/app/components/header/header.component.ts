import { Component } from '@angular/core';
import { TuiAvatar, TuiBadge, TuiFade, TuiTab, TuiTabsHorizontal } from '@taiga-ui/kit';
import { TuiButton, TuiIcon, TuiLink, TuiSurface, TuiTitle } from '@taiga-ui/core';
import { RouterLink } from '@angular/router';
import { TuiRepeatTimes } from '@taiga-ui/cdk';
import { TuiAsideComponent, TuiHeaderComponent, TuiMainComponent, TuiNavComponent, TuiNavigation } from '@taiga-ui/layout';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-header',
    standalone: true,
    imports: [
        TuiNavigation,
        TuiLink,
        TuiAvatar,
        TuiButton,
        TuiTabsHorizontal,
        TuiTab,
        TuiIcon,
        RouterLink,
        TuiBadge,
        TuiFade,
        TuiRepeatTimes,
        TuiSurface,
        TuiTitle,
        TuiHeaderComponent,
        TuiMainComponent,
        TuiAsideComponent,
        TuiNavComponent,
        TranslateModule,
    ],
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.less'],
})
export class HeaderComponent {
    protected activeItemIndex = 0;

    protected onTabClick(item: number): void {
        //this.alerts.open(item).subscribe();
    }
}
