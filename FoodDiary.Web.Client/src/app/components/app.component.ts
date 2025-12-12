import { Component, ViewEncapsulation, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar.component';
import { AuthService } from '../services/auth.service';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, SidebarComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    encapsulation: ViewEncapsulation.None
})
export class AppComponent {
    private readonly authService = inject(AuthService);

    public isAuthenticated = this.authService.isAuthenticated;
    public title = 'food-diary-web-client';
}
