import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { FastingFacade } from '../lib/fasting.facade';
import { FASTING_PROTOCOLS, FastingProtocol } from '../models/fasting.data';

@Component({
    selector: 'fd-fasting-page',
    standalone: true,
    imports: [
        DatePipe,
        DecimalPipe,
        FormsModule,
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './fasting-page.component.html',
    styleUrl: './fasting-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FastingFacade],
})
export class FastingPageComponent implements OnInit {
    private readonly facade = inject(FastingFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly isStarting = this.facade.isStarting;
    public readonly isEnding = this.facade.isEnding;
    public readonly isActive = this.facade.isActive;
    public readonly currentSession = this.facade.currentSession;
    public readonly stats = this.facade.stats;
    public readonly history = this.facade.history;
    public readonly selectedProtocol = this.facade.selectedProtocol;
    public readonly customHours = this.facade.customHours;
    public readonly progressPercent = this.facade.progressPercent;
    public readonly elapsedFormatted = this.facade.elapsedFormatted;
    public readonly remainingFormatted = this.facade.remainingFormatted;
    public readonly isOvertime = this.facade.isOvertime;
    public readonly protocols = FASTING_PROTOCOLS;

    protected readonly Math = Math;

    public ngOnInit(): void {
        this.facade.initialize();
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.facade.selectProtocol(protocol);
    }

    public onCustomHoursChange(value: string): void {
        const hours = parseInt(value, 10);
        if (!isNaN(hours)) {
            this.facade.setCustomHours(hours);
        }
    }

    public startFasting(): void {
        this.facade.startFasting();
    }

    public endFasting(): void {
        this.facade.endFasting();
    }
}
