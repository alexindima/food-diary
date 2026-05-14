import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { HealthAreaScores } from '../../models/usda.data';
import { HealthAreaScoresComponent } from './health-area-scores.component';

const HEART_SCORE = 82;
const BONE_SCORE = 71;
const IMMUNE_SCORE = 64;
const ENERGY_SCORE = 55;
const ANTIOXIDANT_SCORE = 47;
const AREA_COUNT = 5;

describe('HealthAreaScoresComponent', () => {
    it('renders no area cards when scores are missing', () => {
        const { component } = setupComponent(null);

        expect(component.areas()).toEqual([]);
    });

    it('renders all health area cards', () => {
        const { component, fixture } = setupComponent({
            heart: { score: HEART_SCORE, grade: 'excellent' },
            bone: { score: BONE_SCORE, grade: 'good' },
            immune: { score: IMMUNE_SCORE, grade: 'fair' },
            energy: { score: ENERGY_SCORE, grade: 'low' },
            antioxidant: { score: ANTIOXIDANT_SCORE, grade: 'unknown' },
        });
        const text = getText(fixture);

        expect(component.areas()).toHaveLength(AREA_COUNT);
        expect(text).toContain('HEALTH_SCORES.HEART');
        expect(text).toContain(String(HEART_SCORE));
    });
});

function setupComponent(scores: HealthAreaScores | null): {
    component: HealthAreaScoresComponent;
    fixture: ComponentFixture<HealthAreaScoresComponent>;
} {
    TestBed.configureTestingModule({
        imports: [HealthAreaScoresComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(HealthAreaScoresComponent);
    fixture.componentRef.setInput('scores', scores);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<HealthAreaScoresComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
