import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { MealAddComponent } from './meal-add.component';

describe('MealAddComponent', () => {
    it('should create add page wrapper', () => {
        const { component } = setupComponent();

        expect(component).toBeTruthy();
    });
});

function setupComponent(): {
    component: MealAddComponent;
    fixture: ComponentFixture<MealAddComponent>;
} {
    TestBed.configureTestingModule({
        imports: [MealAddComponent],
    });
    TestBed.overrideComponent(MealAddComponent, {
        set: { template: '' },
    });

    const fixture = TestBed.createComponent(MealAddComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
