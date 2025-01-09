import { Component, inject } from '@angular/core';
import { HeroComponent } from '../hero/hero.component';
import { FeaturesComponent } from '../features/features.component';
import { TodayConsumptionComponent } from '../today-consumption/today-consumption.component';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'fd-main',
    imports: [HeroComponent, FeaturesComponent, TodayConsumptionComponent],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.less']
})
export class MainComponent {
    private readonly authService = inject(AuthService);

    public isAuthenticated = this.authService.isAuthenticated;
}
