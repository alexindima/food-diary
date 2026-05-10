import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DEFAULT_SATIETY_LEVELS, FdUiSatietyScaleComponent } from './fd-ui-satiety-scale.component';

describe('FdUiSatietyScaleComponent', () => {
    let component: FdUiSatietyScaleComponent;
    let fixture: ComponentFixture<FdUiSatietyScaleComponent>;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const buttons = (): NodeListOf<HTMLButtonElement> => host().querySelectorAll<HTMLButtonElement>('.satiety-scale__option');
    const container = (): HTMLElement => {
        const element = host().querySelector<HTMLElement>('.satiety-scale');
        if (element === null) {
            throw new Error('Expected satiety scale container to exist.');
        }

        return element;
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiSatietyScaleComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiSatietyScaleComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render 5 levels', () => {
        const levelButtons = buttons();
        expect(levelButtons.length).toBe(5);
        expect(DEFAULT_SATIETY_LEVELS.length).toBe(5);
    });

    it('should write value via CVA', () => {
        component.writeValue(5);
        fixture.detectChanges();

        expect(component['value']).toBe(5);

        const levelButtons = buttons();
        const selectedButton = levelButtons[4];
        expect(selectedButton.classList).toContain('satiety-scale__option--selected');
    });

    it('should select level on click', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const levelButtons = buttons();
        levelButtons[3].click();
        fixture.detectChanges();

        expect(onChangeSpy).toHaveBeenCalledWith(4);
        expect(component['value']).toBe(4);
    });

    it('should mark selected level', () => {
        component.writeValue(4);
        fixture.detectChanges();

        const levelButtons = buttons();
        for (let i = 0; i < levelButtons.length; i++) {
            if (i === 3) {
                expect(levelButtons[i].classList).toContain('satiety-scale__option--selected');
            } else {
                expect(levelButtons[i].classList).not.toContain('satiety-scale__option--selected');
            }
        }
    });

    it('should not select when disabled', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);
        fixture.detectChanges();

        const levelButtons = buttons();
        levelButtons[2].click();
        fixture.detectChanges();

        expect(onChangeSpy).not.toHaveBeenCalled();
        expect(component['value']).toBeNull();
    });

    it('should apply grid layout class by default', () => {
        const scale = container();
        expect(scale.classList).toContain('satiety-scale--grid');
        expect(scale.classList).not.toContain('satiety-scale--vertical');
    });

    it('should apply vertical layout class', () => {
        fixture.componentRef.setInput('layout', 'vertical');
        fixture.detectChanges();

        const scale = container();
        expect(scale.classList).toContain('satiety-scale--vertical');
        expect(scale.classList).not.toContain('satiety-scale--grid');
    });

    it('should call onTouched when level is selected', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const levelButtons = buttons();
        levelButtons[1].click();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should emit levelSelected output on click', () => {
        const emitSpy = vi.spyOn(component['levelSelected'], 'emit');

        const levelButtons = buttons();
        levelButtons[4].click();

        expect(emitSpy).toHaveBeenCalledWith(5);
    });

    it('should write null value via CVA', () => {
        component.writeValue(5);
        fixture.detectChanges();
        expect(component['value']).toBe(5);

        component.writeValue(null);
        fixture.detectChanges();
        expect(component['value']).toBeNull();

        const selected = host().querySelectorAll<HTMLButtonElement>('.satiety-scale__option--selected');
        expect(selected.length).toBe(0);
    });
});
