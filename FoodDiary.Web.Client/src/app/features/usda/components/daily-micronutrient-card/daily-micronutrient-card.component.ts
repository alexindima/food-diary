import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { DailyMicronutrient } from '../../models/usda.data';

@Component({
    selector: 'fd-daily-micronutrient-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    template: `
        <div class="card">
            <div class="card-header">
                <h3 class="card-title fd-ui-card-title">{{ 'MICRONUTRIENTS.DAILY_TITLE' | translate }}</h3>
                @if (linkedCount() > 0) {
                    <span class="coverage-badge fd-ui-overline">
                        {{ linkedCount() }}/{{ totalCount() }} {{ 'MICRONUTRIENTS.LINKED' | translate }}
                    </span>
                }
            </div>

            @if (keyNutrients().length === 0) {
                <p class="empty-state fd-ui-body-sm">{{ 'MICRONUTRIENTS.NO_DAILY_DATA' | translate }}</p>
            } @else {
                <div class="nutrients-grid">
                    @for (nutrient of keyNutrients(); track nutrient.nutrientId) {
                        <div class="nutrient-item">
                            <div class="nutrient-header">
                                <span class="nutrient-name fd-ui-stat-label">{{ nutrient.name }}</span>
                                <span class="nutrient-value fd-ui-caption">
                                    {{ nutrient.totalAmount | number: '1.0-1' }}{{ nutrient.unit }}
                                    @if (nutrient.percentDailyValue !== null) {
                                        <span
                                            class="dv-text"
                                            [class.dv-low]="nutrient.percentDailyValue < 25"
                                            [class.dv-mid]="nutrient.percentDailyValue >= 25 && nutrient.percentDailyValue < 75"
                                            [class.dv-good]="nutrient.percentDailyValue >= 75"
                                        >
                                            {{ nutrient.percentDailyValue | number: '1.0-0' }}%
                                        </span>
                                    }
                                </span>
                            </div>
                            @if (nutrient.percentDailyValue !== null) {
                                <div class="progress-bar">
                                    <div
                                        class="progress-fill"
                                        [style.width.%]="Math.min(nutrient.percentDailyValue, 100)"
                                        [class.fill-low]="nutrient.percentDailyValue < 25"
                                        [class.fill-mid]="nutrient.percentDailyValue >= 25 && nutrient.percentDailyValue < 75"
                                        [class.fill-good]="nutrient.percentDailyValue >= 75"
                                    ></div>
                                </div>
                            }
                        </div>
                    }
                </div>
            }
        </div>
    `,
    styles: [
        `
            .card {
                padding: 16px;
            }

            .card-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 16px;
            }

            .card-title {
                margin: 0;
            }

            .coverage-badge {
                padding: 2px 8px;
                border-radius: 12px;
                background: var(--fd-surface-variant, var(--fd-color-slate-200));
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .nutrients-grid {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 12px;
            }

            .nutrient-item {
                padding: 8px;
                border-radius: 8px;
                background: var(--fd-surface-variant, color-mix(in srgb, var(--fd-color-slate-900) 2%, transparent));
            }

            .nutrient-header {
                display: flex;
                justify-content: space-between;
                align-items: baseline;
                margin-bottom: 6px;
            }

            .nutrient-value {
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .dv-text {
                font-weight: var(--fd-text-stat-value-weight, 600);
                margin-left: 4px;
            }

            .dv-low {
                color: var(--fd-error, var(--fd-color-danger));
            }
            .dv-mid {
                color: var(--fd-warning, var(--fd-color-orange-500));
            }
            .dv-good {
                color: var(--fd-success, var(--fd-color-green-500));
            }

            .progress-bar {
                height: 4px;
                background: var(--fd-surface-variant, var(--fd-color-slate-200));
                border-radius: 2px;
                overflow: hidden;
            }

            .progress-fill {
                height: 100%;
                border-radius: 2px;
                transition: width 0.3s ease;
            }

            .fill-low {
                background-color: var(--fd-error, var(--fd-color-danger));
            }
            .fill-mid {
                background-color: var(--fd-warning, var(--fd-color-orange-500));
            }
            .fill-good {
                background-color: var(--fd-success, var(--fd-color-green-500));
            }

            .empty-state {
                text-align: center;
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
                padding: 16px;
            }

            @media (max-width: 480px) {
                .nutrients-grid {
                    grid-template-columns: 1fr;
                }
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyMicronutrientCardComponent {
    protected readonly Math = Math;

    public readonly nutrients = input<DailyMicronutrient[]>([]);
    public readonly linkedCount = input(0);
    public readonly totalCount = input(0);

    // Show only key vitamins and minerals that have DRI values
    private static readonly KEY_NUTRIENT_IDS = new Set([
        1106,
        1162,
        1110,
        1109,
        1175,
        1178, // Vitamins A, C, D, E, B6, B12
        1087,
        1089,
        1090,
        1092,
        1095,
        1103, // Calcium, Iron, Magnesium, Potassium, Zinc, Selenium
    ]);

    public readonly keyNutrients = computed(() =>
        this.nutrients()
            .filter(n => DailyMicronutrientCardComponent.KEY_NUTRIENT_IDS.has(n.nutrientId))
            .sort((a, b) => a.name.localeCompare(b.name)),
    );
}
