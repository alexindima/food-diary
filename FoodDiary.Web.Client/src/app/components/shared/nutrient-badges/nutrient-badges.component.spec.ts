import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { NutrientBadgesComponent } from './nutrient-badges.component';

describe('NutrientBadgesComponent', () => {
    let component: NutrientBadgesComponent;
    let fixture: ComponentFixture<NutrientBadgesComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [NutrientBadgesComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(NutrientBadgesComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('proteins', 25);
        fixture.componentRef.setInput('fats', 10);
        fixture.componentRef.setInput('carbs', 50);
        fixture.componentRef.setInput('fiber', 8);
        fixture.componentRef.setInput('alcohol', 0);
    });

    it('should create', () => {
        fixture.detectChanges();
        expect(component).toBeTruthy();
    });

    it('should render all nutrient values', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const chips = el.querySelectorAll('.nutrient-badges__chip-value');
        expect(chips.length).toBe(5);
    });

    it('should render protein value', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const proteinChip = el.querySelector('.nutrient-badges__chip--protein .nutrient-badges__chip-value');
        expect(proteinChip?.textContent).toContain('25');
    });

    it('should render fat value', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const fatChip = el.querySelector('.nutrient-badges__chip--fat .nutrient-badges__chip-value');
        expect(fatChip?.textContent).toContain('10');
    });

    it('should render carb value', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const carbChip = el.querySelector('.nutrient-badges__chip--carb .nutrient-badges__chip-value');
        expect(carbChip?.textContent).toContain('50');
    });

    it('should render fiber value', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const fiberChip = el.querySelector('.nutrient-badges__chip--fiber .nutrient-badges__chip-value');
        expect(fiberChip?.textContent).toContain('8');
    });

    it('should render alcohol value', () => {
        fixture.detectChanges();
        const el: HTMLElement = fixture.nativeElement;
        const alcoholChip = el.querySelector('.nutrient-badges__chip--alcohol .nutrient-badges__chip-value');
        expect(alcoholChip?.textContent).toContain('0');
    });

    it('should expose input values', () => {
        fixture.detectChanges();
        expect(component.proteins()).toBe(25);
        expect(component.fats()).toBe(10);
        expect(component.carbs()).toBe(50);
        expect(component.fiber()).toBe(8);
        expect(component.alcohol()).toBe(0);
    });
});
