import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';

@Component({
    selector: 'fd-cycle-tracking-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ReactiveFormsModule,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiDateInputComponent,
        FdUiCheckboxComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './cycle-tracking-page.component.html',
    styleUrl: './cycle-tracking-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [CycleTrackingFacade],
})
export class CycleTrackingPageComponent implements OnInit {
    private readonly facade = inject(CycleTrackingFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly isSavingCycle = this.facade.isSavingCycle;
    public readonly isSavingDay = this.facade.isSavingDay;
    public readonly cycle = this.facade.cycle;
    public readonly startCycleForm = this.facade.startCycleForm;
    public readonly dayForm = this.facade.dayForm;

    public readonly symptomFields = [
        { key: 'pain', labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
        { key: 'mood', labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
        { key: 'edema', labelKey: 'CYCLE_TRACKING.SYMPTOM_EDEMA' },
        { key: 'headache', labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
        { key: 'energy', labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
        { key: 'sleepQuality', labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
        { key: 'libido', labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
    ] as const;

    public readonly predictions = this.facade.predictions;
    public readonly days = this.facade.days;
    public readonly currentCycleTitle = this.facade.currentCycleTitle;

    public ngOnInit(): void {
        this.facade.initialize();
    }

    public startCycle(): void {
        this.facade.startCycle();
    }

    public saveDay(): void {
        this.facade.saveDay();
    }
}
