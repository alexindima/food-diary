import { Component, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../services/user.service';
import { User } from '../../types/user.data';
import { SlicePipe, UpperCasePipe } from '@angular/common';

@Component({
    selector: 'fd-sidebar',
    imports: [
        TranslateModule,
        RouterModule,
        FdUiIconModule,
        MatIconModule,
        SlicePipe,
        UpperCasePipe,
    ],
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
    private readonly authService = inject(AuthService);
    private readonly userService = inject(UserService);

    public isAuthenticated = this.authService.isAuthenticated;
    protected currentUser = signal<User | null>(null);
    protected isFoodTrackingOpen = signal(true);
    protected isBodyTrackingOpen = signal(false);

    public constructor() {
        if (this.isAuthenticated()) {
            this.userService.getInfo().subscribe(user => this.currentUser.set(user));
        }
    }

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
