
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-hero',
    imports: [FdUiButtonComponent, TranslateModule],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.scss'
})
export class HeroComponent {
    private readonly navigationService = inject(NavigationService);

    public async goToLogin(): Promise<void> {
        await this.navigationService.navigateToAuth('login');
    }

    public async goToRegister(): Promise<void> {
        await this.navigationService.navigateToAuth('register');
    }
}
