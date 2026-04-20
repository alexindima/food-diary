import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { WeeklyCheckInFacade } from '../lib/weekly-check-in.facade';

@Component({
    selector: 'fd-weekly-check-in-page',
    standalone: true,
    imports: [
        DecimalPipe,
        TranslatePipe,
        FdUiIconComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdUiCardComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './weekly-check-in-page.component.html',
    styleUrl: './weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeeklyCheckInFacade],
})
export class WeeklyCheckInPageComponent {
    private readonly facade = inject(WeeklyCheckInFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly thisWeek = this.facade.thisWeek;
    public readonly lastWeek = this.facade.lastWeek;
    public readonly trends = this.facade.trends;
    public readonly suggestions = this.facade.suggestions;
    public readonly getTrendIcon = this.facade.getTrendIcon;
    public readonly getTrendColor = this.facade.getTrendColor;

    protected readonly Math = Math;

    public constructor() {
        this.facade.initialize();
    }
}
