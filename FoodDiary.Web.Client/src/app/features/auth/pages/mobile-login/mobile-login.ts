import { afterNextRender, ChangeDetectionStrategy, Component, ElementRef, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AuthComponent } from '../../components/auth/auth';

@Component({
    selector: 'fd-mobile-login',
    imports: [AuthComponent, TranslatePipe],
    templateUrl: './mobile-login.html',
    styleUrl: './mobile-login.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MobileLoginComponent {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    protected readonly dashboardReturnUrl = '/dashboard';

    public constructor() {
        afterNextRender(() => {
            this.host.nativeElement.querySelector('.mobile-login__content')?.scrollIntoView({ block: 'start' });
        });
    }
}
