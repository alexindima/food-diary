import { Component, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'fd-sidebar',
    imports: [
        TranslateModule,
        RouterModule,
        FdUiIconModule,
        MatIconModule
    ],
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
    private readonly authService = inject(AuthService);

    public isAuthenticated = this.authService.isAuthenticated;
    protected isFoodTrackingOpen = signal(true);
    protected isBodyTrackingOpen = signal(false);

    protected toggleFoodTracking(): void {
        const next = !this.isFoodTrackingOpen();
        this.isFoodTrackingOpen.set(next);
        if (next) {
            this.isBodyTrackingOpen.set(false);
        }
    }

    protected toggleBodyTracking(): void {
        const next = !this.isBodyTrackingOpen();
        this.isBodyTrackingOpen.set(next);
        if (next) {
            this.isFoodTrackingOpen.set(false);
        }
    }
}
