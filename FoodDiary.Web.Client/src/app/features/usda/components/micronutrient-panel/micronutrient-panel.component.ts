import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Micronutrient } from '../../models/usda.data';

@Component({
    selector: 'fd-micronutrient-panel',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    template: `
        <div class="micronutrient-panel">
            <h3 class="panel-title">{{ 'MICRONUTRIENTS.TITLE' | translate }}</h3>

            @if (vitamins().length > 0) {
                <div class="section">
                    <h4 class="section-title">{{ 'MICRONUTRIENTS.VITAMINS' | translate }}</h4>
                    @for (nutrient of vitamins(); track nutrient.nutrientId) {
                        <div class="nutrient-row">
                            <span class="nutrient-name">{{ nutrient.name }}</span>
                            <span class="nutrient-amount">{{ nutrient.amountPer100g | number: '1.1-1' }} {{ nutrient.unit }}</span>
                            @if (nutrient.percentDailyValue !== null) {
                                <div class="dv-bar-container">
                                    <div
                                        class="dv-bar"
                                        [style.width.%]="Math.min(nutrient.percentDailyValue, 100)"
                                        [class.dv-low]="nutrient.percentDailyValue < 15"
                                        [class.dv-good]="nutrient.percentDailyValue >= 15 && nutrient.percentDailyValue <= 100"
                                        [class.dv-high]="nutrient.percentDailyValue > 100"
                                    ></div>
                                </div>
                                <span class="dv-percent">{{ nutrient.percentDailyValue }}%</span>
                            }
                        </div>
                    }
                </div>
            }

            @if (minerals().length > 0) {
                <div class="section">
                    <h4 class="section-title">{{ 'MICRONUTRIENTS.MINERALS' | translate }}</h4>
                    @for (nutrient of minerals(); track nutrient.nutrientId) {
                        <div class="nutrient-row">
                            <span class="nutrient-name">{{ nutrient.name }}</span>
                            <span class="nutrient-amount">{{ nutrient.amountPer100g | number: '1.1-1' }} {{ nutrient.unit }}</span>
                            @if (nutrient.percentDailyValue !== null) {
                                <div class="dv-bar-container">
                                    <div
                                        class="dv-bar"
                                        [style.width.%]="Math.min(nutrient.percentDailyValue, 100)"
                                        [class.dv-low]="nutrient.percentDailyValue < 15"
                                        [class.dv-good]="nutrient.percentDailyValue >= 15 && nutrient.percentDailyValue <= 100"
                                        [class.dv-high]="nutrient.percentDailyValue > 100"
                                    ></div>
                                </div>
                                <span class="dv-percent">{{ nutrient.percentDailyValue }}%</span>
                            }
                        </div>
                    }
                </div>
            }

            @if (vitamins().length === 0 && minerals().length === 0) {
                <p class="empty-state">{{ 'MICRONUTRIENTS.NO_DATA' | translate }}</p>
            }
        </div>
    `,
    styles: [
        `
            .micronutrient-panel {
                padding: 16px 0;
            }

            .panel-title {
                font-size: 16px;
                font-weight: 600;
                margin: 0 0 16px;
            }

            .section {
                margin-bottom: 16px;
            }

            .section-title {
                font-size: 13px;
                font-weight: 600;
                text-transform: uppercase;
                color: var(--fd-text-secondary, #666);
                margin: 0 0 8px;
                letter-spacing: 0.5px;
            }

            .nutrient-row {
                display: grid;
                grid-template-columns: 1fr auto 80px auto;
                align-items: center;
                gap: 8px;
                padding: 4px 0;
                font-size: 13px;
            }

            .nutrient-name {
                font-weight: 500;
            }

            .nutrient-amount {
                color: var(--fd-text-secondary, #666);
                text-align: right;
                min-width: 60px;
            }

            .dv-bar-container {
                height: 6px;
                background: var(--fd-surface-variant, #e0e0e0);
                border-radius: 3px;
                overflow: hidden;
            }

            .dv-bar {
                height: 100%;
                border-radius: 3px;
                transition: width 0.3s ease;
            }

            .dv-low {
                background-color: var(--fd-warning, #ff9800);
            }
            .dv-good {
                background-color: var(--fd-success, #4caf50);
            }
            .dv-high {
                background-color: var(--fd-primary, #1976d2);
            }

            .dv-percent {
                font-size: 12px;
                color: var(--fd-text-secondary, #666);
                min-width: 36px;
                text-align: right;
            }

            .empty-state {
                text-align: center;
                color: var(--fd-text-secondary, #666);
                padding: 24px;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MicronutrientPanelComponent {
    protected readonly Math = Math;

    public readonly nutrients = input<Micronutrient[]>([]);

    private static readonly VITAMIN_IDS = new Set([1106, 1165, 1166, 1167, 1170, 1175, 1177, 1178, 1162, 1110, 1109, 1185]);
    private static readonly MINERAL_IDS = new Set([1087, 1089, 1090, 1091, 1092, 1093, 1095, 1098, 1101, 1103]);

    public readonly vitamins = computed(() => this.nutrients().filter(n => MicronutrientPanelComponent.VITAMIN_IDS.has(n.nutrientId)));

    public readonly minerals = computed(() => this.nutrients().filter(n => MicronutrientPanelComponent.MINERAL_IDS.has(n.nutrientId)));
}
