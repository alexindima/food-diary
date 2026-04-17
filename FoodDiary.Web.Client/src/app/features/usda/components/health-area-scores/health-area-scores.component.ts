import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { HealthAreaScores } from '../../models/usda.data';

interface HealthAreaDisplay {
    key: string;
    icon: string;
    score: number;
    grade: string;
}

@Component({
    selector: 'fd-health-area-scores',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    template: `
        <div class="health-scores">
            <h3 class="title">{{ 'HEALTH_SCORES.TITLE' | translate }}</h3>
            <div class="areas-grid">
                @for (area of areas(); track area.key) {
                    <div class="area-card" [class]="'area-card--' + area.grade">
                        <span class="area-icon">{{ area.icon }}</span>
                        <span class="area-name">{{ 'HEALTH_SCORES.' + area.key | translate }}</span>
                        <div class="area-score-ring">
                            <svg viewBox="0 0 36 36" class="ring-svg">
                                <path class="ring-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831" />
                                <path
                                    class="ring-fill"
                                    [attr.stroke-dasharray]="area.score + ', 100'"
                                    d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
                                />
                            </svg>
                            <span class="score-text">{{ area.score }}</span>
                        </div>
                        <span class="area-grade">{{ 'HEALTH_SCORES.GRADE_' + area.grade.toUpperCase() | translate }}</span>
                    </div>
                }
            </div>
        </div>
    `,
    styles: [
        `
            .health-scores {
                padding: 16px 0;
            }

            .title {
                font-size: 16px;
                font-weight: 600;
                margin: 0 0 16px;
            }

            .areas-grid {
                display: grid;
                grid-template-columns: repeat(5, 1fr);
                gap: 12px;
            }

            .area-card {
                display: flex;
                flex-direction: column;
                align-items: center;
                padding: 12px 8px;
                border-radius: 12px;
                background: var(--fd-surface-variant, color-mix(in srgb, var(--fd-color-slate-900) 2%, transparent));
                gap: 4px;
            }

            .area-icon {
                font-size: 20px;
            }

            .area-name {
                font-size: 11px;
                font-weight: 600;
                text-transform: uppercase;
                letter-spacing: 0.3px;
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .area-score-ring {
                position: relative;
                width: 48px;
                height: 48px;
            }

            .ring-svg {
                width: 100%;
                height: 100%;
                transform: rotate(-90deg);
            }

            .ring-bg {
                fill: none;
                stroke: var(--fd-surface-variant, var(--fd-color-slate-200));
                stroke-width: 3;
            }

            .ring-fill {
                fill: none;
                stroke-width: 3;
                stroke-linecap: round;
                transition: stroke-dasharray 0.5s ease;
            }

            .area-card--excellent .ring-fill {
                stroke: var(--fd-success, var(--fd-color-green-500));
            }
            .area-card--good .ring-fill {
                stroke: var(--fd-primary, var(--fd-color-primary-600));
            }
            .area-card--fair .ring-fill {
                stroke: var(--fd-warning, var(--fd-color-orange-500));
            }
            .area-card--low .ring-fill {
                stroke: var(--fd-error, var(--fd-color-danger));
            }
            .area-card--unknown .ring-fill {
                stroke: var(--fd-surface-variant, var(--fd-color-slate-200));
            }

            .score-text {
                position: absolute;
                inset: 0;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 14px;
                font-weight: 700;
            }

            .area-grade {
                font-size: 10px;
                font-weight: 600;
                text-transform: uppercase;
            }

            .area-card--excellent .area-grade {
                color: var(--fd-success, var(--fd-color-green-500));
            }
            .area-card--good .area-grade {
                color: var(--fd-primary, var(--fd-color-primary-600));
            }
            .area-card--fair .area-grade {
                color: var(--fd-warning, var(--fd-color-orange-500));
            }
            .area-card--low .area-grade {
                color: var(--fd-error, var(--fd-color-danger));
            }
            .area-card--unknown .area-grade {
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            @media (max-width: 600px) {
                .areas-grid {
                    grid-template-columns: repeat(3, 1fr);
                }
            }

            @media (max-width: 380px) {
                .areas-grid {
                    grid-template-columns: repeat(2, 1fr);
                }
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthAreaScoresComponent {
    public readonly scores = input<HealthAreaScores | null>(null);

    public readonly areas = computed<HealthAreaDisplay[]>(() => {
        const s = this.scores();
        if (!s) {
            return [];
        }
        return [
            { key: 'HEART', icon: '\u2764\uFE0F', score: s.heart.score, grade: s.heart.grade },
            { key: 'BONE', icon: '\uD83E\uDDB4', score: s.bone.score, grade: s.bone.grade },
            { key: 'IMMUNE', icon: '\uD83D\uDEE1\uFE0F', score: s.immune.score, grade: s.immune.grade },
            { key: 'ENERGY', icon: '\u26A1', score: s.energy.score, grade: s.energy.grade },
            { key: 'ANTIOXIDANT', icon: '\uD83E\uDDEC', score: s.antioxidant.score, grade: s.antioxidant.grade },
        ];
    });
}
