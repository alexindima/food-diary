import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { finalize, firstValueFrom } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { isNutritionMetric, type NutritionDetailsAction, type NutritionDetailsData } from '../models/nutrition-details.types';
import { DashboardFacade } from './dashboard.facade';
import { DashboardLayoutService } from './dashboard-layout.service';

@Injectable()
export class DashboardNutritionDialogFacade {
    private readonly dashboard = inject(DashboardFacade);
    private readonly layout = inject(DashboardLayoutService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly navigation = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private opened = false;

    public async openAsync(metric: string): Promise<void> {
        if (this.layout.isEditingLayout() || this.opened || !isNutritionMetric(metric)) {
            return;
        }
        this.opened = true;
        const date = new Date(this.dashboard.selectedDate());
        try {
            const { NutritionDetailsDialogComponent } = await import('../dialogs/nutrition-details-dialog/nutrition-details-dialog');
            if (this.destroyRef.destroyed) {
                return;
            }
            const dialog = this.dialogs.open<
                InstanceType<typeof NutritionDetailsDialogComponent>,
                NutritionDetailsData,
                NutritionDetailsAction
            >(NutritionDetailsDialogComponent, {
                preset: 'detail',
                size: 'md',
                data: { date, metric },
            });
            const unregisterClose = this.destroyRef.onDestroy(() => {
                dialog.close();
            });
            const action = await firstValueFrom(dialog.afterClosed().pipe(takeUntilDestroyed(this.destroyRef), finalize(unregisterClose)), {
                defaultValue: undefined,
            });
            if (action !== undefined) {
                await this.handleActionAsync(action, date);
            }
        } finally {
            this.opened = false;
        }
    }

    private async handleActionAsync(action: NutritionDetailsAction, date: Date): Promise<void> {
        switch (action.kind) {
            case 'meal': {
                await this.dashboard.openMealDetailsAsync(action.meal.id, action.meal);
                break;
            }
            case 'goals': {
                await this.navigation.navigateToGoalsAsync();
                break;
            }
            case 'diary': {
                await this.navigation.navigateToMealListAsync(date);
                break;
            }
        }
    }
}
