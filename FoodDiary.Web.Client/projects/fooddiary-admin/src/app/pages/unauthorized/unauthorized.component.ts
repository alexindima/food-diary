import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../../services/admin-auth.service';

@Component({
  selector: 'fd-admin-unauthorized',
  standalone: true,
  imports: [FdUiButtonComponent],
  templateUrl: './unauthorized.component.html',
  styleUrl: './unauthorized.component.scss',
})
export class UnauthorizedComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AdminAuthService);

  public get reason(): string | null {
    return this.route.snapshot.queryParamMap.get('reason');
  }

  public get returnUrl(): string {
    return this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
  }

  public ngOnInit(): void {
    if (this.reason !== 'unauthenticated') {
      return;
    }

    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (!returnUrl) {
      return;
    }

    void this.tryRecoverFromSso(returnUrl);
  }

  public goToLogin(): void {
    const url = new URL('/auth/login', environment.mainAppUrl);
    const returnUrl = this.normalizeReturnUrl(this.returnUrl);
    if (returnUrl) {
      url.searchParams.set('returnUrl', returnUrl);
    }
    window.location.href = url.toString();
  }

  private async tryRecoverFromSso(returnUrl: string): Promise<void> {
    const cleanedUrl = await this.authService.tryApplySsoFromReturnUrl(returnUrl);
    if (!cleanedUrl) {
      return;
    }

    await this.router.navigateByUrl(cleanedUrl, { replaceUrl: true });
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
