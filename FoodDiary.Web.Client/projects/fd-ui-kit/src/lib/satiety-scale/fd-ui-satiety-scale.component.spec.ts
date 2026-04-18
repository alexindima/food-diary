import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiSatietyScaleComponent, DEFAULT_SATIETY_LEVELS } from './fd-ui-satiety-scale.component';
import { TranslateModule } from '@ngx-translate/core';

describe('FdUiSatietyScaleComponent', () => {
    let component: FdUiSatietyScaleComponent;
    let fixture: ComponentFixture<FdUiSatietyScaleComponent>;

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

    it('should render 10 levels', () => {
        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        expect(buttons.length).toBe(10);
        expect(DEFAULT_SATIETY_LEVELS.length).toBe(10);
    });

    it('should write value via CVA', () => {
        component.writeValue(5);
        fixture.detectChanges();

        expect(component['value']).toBe(5);

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        const selectedButton = buttons[5] as HTMLButtonElement;
        expect(selectedButton.classList).toContain('satiety-scale__option--selected');
    });

    it('should select level on click', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        (buttons[3] as HTMLButtonElement).click();
        fixture.detectChanges();

        expect(onChangeSpy).toHaveBeenCalledWith(3);
        expect(component['value']).toBe(3);
    });

    it('should mark selected level', () => {
        component.writeValue(7);
        fixture.detectChanges();

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        for (let i = 0; i < buttons.length; i++) {
            if (i === 7) {
                expect(buttons[i].classList).toContain('satiety-scale__option--selected');
            } else {
                expect(buttons[i].classList).not.toContain('satiety-scale__option--selected');
            }
        }
    });

    it('should not select when disabled', () => {
        const onChangeSpy = vi.fn();
        component.registerOnChange(onChangeSpy);
        component.setDisabledState(true);
        fixture.detectChanges();

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        (buttons[2] as HTMLButtonElement).click();
        fixture.detectChanges();

        expect(onChangeSpy).not.toHaveBeenCalled();
        expect(component['value']).toBeNull();
    });

    it('should apply grid layout class by default', () => {
        const container = fixture.nativeElement.querySelector('.satiety-scale');
        expect(container.classList).toContain('satiety-scale--grid');
        expect(container.classList).not.toContain('satiety-scale--vertical');
    });

    it('should apply vertical layout class', () => {
        fixture.componentRef.setInput('layout', 'vertical');
        fixture.detectChanges();

        const container = fixture.nativeElement.querySelector('.satiety-scale');
        expect(container.classList).toContain('satiety-scale--vertical');
        expect(container.classList).not.toContain('satiety-scale--grid');
    });

    it('should call onTouched when level is selected', () => {
        const onTouchedSpy = vi.fn();
        component.registerOnTouched(onTouchedSpy);

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        (buttons[1] as HTMLButtonElement).click();

        expect(onTouchedSpy).toHaveBeenCalled();
    });

    it('should emit levelSelected output on click', () => {
        const emitSpy = vi.spyOn(component['levelSelected'], 'emit');

        const buttons = fixture.nativeElement.querySelectorAll('.satiety-scale__option');
        (buttons[4] as HTMLButtonElement).click();

        expect(emitSpy).toHaveBeenCalledWith(4);
    });

    it('should write null value via CVA', () => {
        component.writeValue(5);
        fixture.detectChanges();
        expect(component['value']).toBe(5);

        component.writeValue(null);
        fixture.detectChanges();
        expect(component['value']).toBeNull();

        const selected = fixture.nativeElement.querySelectorAll('.satiety-scale__option--selected');
        expect(selected.length).toBe(0);
    });
});
