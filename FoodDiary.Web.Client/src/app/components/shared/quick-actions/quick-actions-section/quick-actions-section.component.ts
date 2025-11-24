import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { QuickActionCardComponent } from '../quick-action-card/quick-action-card.component';
import { NavigationService } from '../../../../services/navigation.service';

@Component({
    selector: 'fd-quick-actions-section',
    standalone: true,
    imports: [QuickActionCardComponent],
    templateUrl: './quick-actions-section.component.html',
    styleUrls: ['./quick-actions-section.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickActionsSectionComponent {
    private readonly navigationService = inject(NavigationService);

    public async addConsumption(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public async addProduct(): Promise<void> {
        await this.navigationService.navigateToProductAdd();
    }

    public async addRecipe(): Promise<void> {
        await this.navigationService.navigateToRecipeAdd();
    }

    public async openStatistics(): Promise<void> {
        await this.navigationService.navigateToStatistics();
    }
}
