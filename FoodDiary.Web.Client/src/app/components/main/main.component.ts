import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../auth/auth-dialog.component';
import { AuthService } from '../../services/auth.service';
import { HeroComponent } from '../hero/hero.component';
import { FeaturesComponent } from '../features/features.component';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { LandingPreviewTourComponent } from './preview-tour/landing-preview-tour.component';
import { LandingStepsComponent } from './steps/landing-steps.component';
import { LandingCtaComponent } from './cta/landing-cta.component';

@Component({
    selector: 'fd-main',
    imports: [HeroComponent, FeaturesComponent, DashboardComponent, LandingPreviewTourComponent, LandingStepsComponent, LandingCtaComponent],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
    private readonly authService = inject(AuthService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly route = inject(ActivatedRoute);

    public isAuthenticated = this.authService.isAuthenticated;

    public ngOnInit(): void {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.params['mode'];
            const mode: 'login' | 'register' = modeParam === 'register' ? 'register' : 'login';
            this.openAuthDialog(mode);
        }
    }

    private openAuthDialog(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode },
        });
    }
}
