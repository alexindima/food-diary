import { Component, inject } from '@angular/core';
import { TuiCarousel } from '@taiga-ui/kit';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';
import { FdUiButtonComponent } from '../../ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-hero',
    imports: [FdUiButtonComponent, TuiCarousel, TranslateModule],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.less'
})
export class HeroComponent {
    private readonly navigationService = inject(NavigationService);

    protected currentSlide: number = 0;
    protected slides: string[] = ['slide1', 'slide2', 'slide3', 'slide4', 'slide5'];

    public async goToLogin(): Promise<void> {
        await this.navigationService.navigateToAuth('login');
    }

    public async goToRegister(): Promise<void> {
        await this.navigationService.navigateToAuth('register');
    }
}
