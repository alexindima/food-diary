import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'fd-admin-unauthorized',
  standalone: true,
  imports: [FdUiButtonComponent],
  templateUrl: './unauthorized.component.html',
  styleUrl: './unauthorized.component.scss',
})
export class UnauthorizedComponent {
  private readonly route = inject(ActivatedRoute);

  public get reason(): string | null {
    return this.route.snapshot.queryParamMap.get('reason');
  }

  public get returnUrl(): string {
    return this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
  }

  public goToLogin(): void {
    const url = new URL('/auth/login', environment.mainAppUrl);
    const returnUrl = this.normalizeReturnUrl(this.returnUrl);
    if (returnUrl) {
      url.searchParams.set('returnUrl', returnUrl);
    }
    window.location.href = url.toString();
  }

  private normalizeReturnUrl(value: string): string {
    if (!value) {
      return '/';
    }

    const decoded = this.safeDecode(value);
    if (decoded.includes('returnUrl=')) {
      return '/';
    }

    return decoded.startsWith('/') ? decoded : '/';
  }

  private safeDecode(value: string): string {
    try {
      return decodeURIComponent(value);
    } catch {
      return value;
    }
  }
}
