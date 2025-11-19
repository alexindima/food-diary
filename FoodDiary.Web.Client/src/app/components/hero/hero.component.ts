import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-hero',
    imports: [CommonModule, FdUiButtonComponent, TranslateModule],
    templateUrl: './hero.component.html',
    styleUrl: './hero.component.scss'
})
export class HeroComponent implements OnInit, OnDestroy {
    private readonly navigationService = inject(NavigationService);
    private slideIntervalId: ReturnType<typeof setInterval> | null = null;

    protected currentSlide = 0;
    protected readonly slideDuration = 5000;
    protected slides: string[] = ['slide1', 'slide2', 'slide3', 'slide4', 'slide5'];

    public ngOnInit(): void {
        this.startAutoSlide();
    }

    public ngOnDestroy(): void {
        this.stopAutoSlide();
    }

    public async goToLogin(): Promise<void> {
        await this.navigationService.navigateToAuth('login');
    }

    public async goToRegister(): Promise<void> {
        await this.navigationService.navigateToAuth('register');
    }

    public goToSlide(index: number): void {
        this.currentSlide = index;
        this.restartAutoSlide();
    }

    private startAutoSlide(): void {
        this.slideIntervalId = setInterval(() => {
            this.currentSlide = (this.currentSlide + 1) % this.slides.length;
        }, this.slideDuration);
    }

    private stopAutoSlide(): void {
        if (this.slideIntervalId) {
            clearInterval(this.slideIntervalId);
            this.slideIntervalId = null;
        }
    }

    private restartAutoSlide(): void {
        this.stopAutoSlide();
        this.startAutoSlide();
    }
}
