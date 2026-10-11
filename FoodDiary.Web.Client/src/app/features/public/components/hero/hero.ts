import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { DayNutritionSummaryComponent } from '../../../../components/shared/day-nutrition-summary/day-nutrition-summary';
import type { PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';
import { buildLandingPreviewContent } from '../landing-preview-tour/landing-preview-tour-data.mapper';

@Component({
    selector: 'fd-hero',
    imports: [RouterLink, FdUiButtonComponent, DayNutritionSummaryComponent, TranslatePipe],
    templateUrl: './hero.html',
    styleUrl: './hero.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeroComponent {
    private readonly authNavigationService = inject(PublicAuthNavigationService);
    protected readonly sampleSummary = buildLandingPreviewContent(key => key).heroSummaryCard;

    protected async navigateToAuthAsync(mode: PublicAuthMode): Promise<void> {
        await this.authNavigationService.navigateAsync(mode);
    }
}
