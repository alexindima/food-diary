import { Component, OnInit, inject } from '@angular/core';
import { HeroComponent } from '../hero/hero.component';
import { FeaturesComponent } from '../features/features.component';
import { TodayConsumptionComponent } from '../today-consumption/today-consumption.component';
import { AuthService } from '../../services/auth.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../auth/auth-dialog.component';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'fd-main',
    imports: [
        HeroComponent,
        FeaturesComponent,
        TodayConsumptionComponent,
        FdUiButtonComponent,
        TranslateModule,
    ],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
    private readonly authService = inject(AuthService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly route = inject(ActivatedRoute);

    public isAuthenticated = this.authService.isAuthenticated;

    public openAuthDialog(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode },
        });
    }

    public ngOnInit(): void {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.params['mode'];
            const mode: 'login' | 'register' = modeParam === 'register' ? 'register' : 'login';
            this.openAuthDialog(mode);
        }
    }
}
