import { Component } from '@angular/core';
import { TuiButton } from '@taiga-ui/core';
import { NgForOf, NgIf, NgOptimizedImage } from '@angular/common';
import { TuiFor, TuiItem } from '@taiga-ui/cdk';
import { TuiCarousel } from '@taiga-ui/kit';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';

@Component({
    selector: 'app-hero',
    standalone: true,
    imports: [TuiButton, NgForOf, NgIf, NgOptimizedImage, TuiItem, TuiCarousel, TuiFor, TranslateModule],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.less',
})
export class HeroComponent {
    protected currentSlide: number = 0;
    protected slides: string[] = ['slide1.webp', 'slide2.webp', 'slide3.webp', 'slide4.webp', 'slide5.webp'];

    public constructor(private readonly navigationService: NavigationService) {}

    public goToLogin(): void {
        this.navigationService.navigateToAuth('login');
    }

    public goToRegister(): void {
        this.navigationService.navigateToAuth('register');
    }
}
