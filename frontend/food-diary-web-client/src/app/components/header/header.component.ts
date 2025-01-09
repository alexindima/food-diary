import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { TuiChevron, TuiTab, TuiTabsHorizontal } from '@taiga-ui/kit';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { TuiDataListComponent, TuiDropdownDirective, TuiDropdownOpen, TuiOption } from '@taiga-ui/core';
import { NavigationService } from '../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'fd-header',
    imports: [
        TuiTabsHorizontal,
        TuiTab,
        TranslateModule,
        RouterModule,
        TuiChevron,
        TuiDropdownOpen,
        TuiDropdownDirective,
        TuiDataListComponent,
        TuiOption,
    ],
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.less']
})
export class HeaderComponent implements OnInit {
    private readonly router = inject(Router);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    public isAuthenticated = this.authService.isAuthenticated;

    protected activeItemIndex = 0;
    public ngOnInit(): void {
        this.router.events
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                filter(event => event instanceof NavigationEnd),
            )
            .subscribe(() => {
                if (this.router.url.startsWith('/profile')) {
                    this.activeItemIndex = 4;
                }
            });
    }

    protected async goToProfile(): Promise<void> {
        await this.navigationService.navigateToProfile();
    }

    protected stop(event: Event): void {
        event.stopPropagation();
    }

    protected async logout(): Promise<void> {
        await this.authService.onLogout();
    }
}
