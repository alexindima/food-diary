import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthMode } from '../types/auth.data';

@Injectable({
    providedIn: 'root'
})
export class NavigationService {
    public constructor(private readonly router: Router) {}

    public navigateToAuth(mode: AuthMode): void {
        this.router.navigate(['/auth', mode]);
    }
}
