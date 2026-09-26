import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import { AuthService } from '../../../../services/auth.service';
import { PublicAuthDialogService } from '../../../public/lib/public-auth-dialog.service';

@Component({
    selector: 'fd-public-recipe-navigation',
    host: { '[style.display]': "auth.isAuthenticated() ? 'none' : 'block'" },
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink, TranslatePipe, FdUiButtonComponent],
    templateUrl: './public-navigation.html',
    styleUrl: './public-navigation.scss',
})
export class PublicRecipeNavigationComponent {
    protected readonly auth = inject(AuthService);
    private readonly dialog = inject(PublicAuthDialogService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);

    protected login(): void {
        void this.dialog.openAsync({ mode: 'login', returnUrl: this.router.url, destroyRef: this.destroyRef });
    }
}
