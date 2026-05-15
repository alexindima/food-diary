import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { BodyTarget } from '../../goals-page-lib/goals-page.models';
import { GoalsBodyTargetsComponent } from './goals-body-targets.component';

const TARGET_WEIGHT = 72;
const TARGET_WAIST = 80;

describe('GoalsBodyTargetsComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [GoalsBodyTargetsComponent, TranslateModule.forRoot()],
        });
    });

    it('renders all body targets', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const inputs = element.querySelectorAll<HTMLInputElement>('.goals__body-input');

        expect(element.textContent).toContain('GOALS_PAGE.BODY_TARGET_WEIGHT');
        expect(element.textContent).toContain('GOALS_PAGE.BODY_TARGET_WAIST');
        expect(inputs).toHaveLength(2);
        expect(inputs[0].value).toBe(TARGET_WEIGHT.toString());
        expect(inputs[1].value).toBe(TARGET_WAIST.toString());
    });

    it('emits changed target key and DOM event', () => {
        const fixture = createComponent();
        const targetInput = vi.fn();
        fixture.componentInstance.targetInput.subscribe(targetInput);

        getElement(fixture).querySelector<HTMLInputElement>('.goals__body-input')?.dispatchEvent(new Event('input'));

        expect(targetInput).toHaveBeenCalledWith(expect.objectContaining({ key: 'weight' }));
    });
});

function createComponent(targets: BodyTarget[] = createTargets()): ComponentFixture<GoalsBodyTargetsComponent> {
    const fixture = TestBed.createComponent(GoalsBodyTargetsComponent);
    fixture.componentRef.setInput('targets', targets);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<GoalsBodyTargetsComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createTargets(): BodyTarget[] {
    return [
        {
            key: 'weight',
            titleKey: 'GOALS_PAGE.BODY_TARGET_WEIGHT',
            value: TARGET_WEIGHT,
            unit: 'kg',
        },
        {
            key: 'waist',
            titleKey: 'GOALS_PAGE.BODY_TARGET_WAIST',
            value: TARGET_WAIST,
            unit: 'cm',
        },
    ];
}
