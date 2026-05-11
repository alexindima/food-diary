import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { NutrientBadgesComponent } from './nutrient-badges.component';

const PROTEIN_VALUE = 25;
const FAT_VALUE = 10;
const CARB_VALUE = 50;
const FIBER_VALUE = 8;
const EXPECTED_CHIP_COUNT = 5;

describe('NutrientBadgesComponent', () => {
    let component: NutrientBadgesComponent;
    let fixture: ComponentFixture<NutrientBadgesComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [NutrientBadgesComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(NutrientBadgesComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('proteins', PROTEIN_VALUE);
        fixture.componentRef.setInput('fats', FAT_VALUE);
        fixture.componentRef.setInput('carbs', CARB_VALUE);
        fixture.componentRef.setInput('fiber', FIBER_VALUE);
        fixture.componentRef.setInput('alcohol', 0);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render all nutrient values', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const chips = el.querySelectorAll('.nutrient-badges__chip-value');
        expect(chips.length).toBe(EXPECTED_CHIP_COUNT);
    });

    it('should render protein value', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const proteinChip = el.querySelector('.nutrient-badges__chip--protein .nutrient-badges__chip-value');
        expect(proteinChip?.textContent).toContain(String(PROTEIN_VALUE));
    });

    it('should render fat value', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const fatChip = el.querySelector('.nutrient-badges__chip--fat .nutrient-badges__chip-value');
        expect(fatChip?.textContent).toContain(String(FAT_VALUE));
    });

    it('should render carb value', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const carbChip = el.querySelector('.nutrient-badges__chip--carb .nutrient-badges__chip-value');
        expect(carbChip?.textContent).toContain(String(CARB_VALUE));
    });

    it('should render fiber value', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const fiberChip = el.querySelector('.nutrient-badges__chip--fiber .nutrient-badges__chip-value');
        expect(fiberChip?.textContent).toContain(String(FIBER_VALUE));
    });

    it('should render alcohol value', () => {
        fixture.detectChanges();
        const el = fixture.nativeElement as HTMLElement;
        const alcoholChip = el.querySelector('.nutrient-badges__chip--alcohol .nutrient-badges__chip-value');
        expect(alcoholChip?.textContent).toContain('0');
    });

    it('should expose input values', () => {
        fixture.detectChanges();
        expect(component.proteins()).toBe(PROTEIN_VALUE);
        expect(component.fats()).toBe(FAT_VALUE);
        expect(component.carbs()).toBe(CARB_VALUE);
        expect(component.fiber()).toBe(FIBER_VALUE);
        expect(component.alcohol()).toBe(0);
    });
});
