import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DEFAULT_SATIETY_LEVELS, FdUiSatietyScaleComponent } from './fd-ui-satiety-scale.component';

const LEVEL_COUNT = 5;
const LOW_LEVEL_INDEX = 1;
const DISABLED_LEVEL_INDEX = 2;
const SELECTED_LEVEL_INDEX = 3;
const HIGH_LEVEL_INDEX = 4;
const SELECTED_LEVEL = 4;
const HIGH_LEVEL = 5;

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

describe('FdUiSatietyScaleComponent', () => {
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

    registerRenderingTests();
    registerValueAccessorTests();
    registerInteractionTests();
    registerLayoutTests();
});

function registerRenderingTests(): void {
    describe('rendering', () => {
        it('should render 5 levels', () => {
            const levelButtons = buttons();
            expect(levelButtons.length).toBe(LEVEL_COUNT);
            expect(DEFAULT_SATIETY_LEVELS.length).toBe(LEVEL_COUNT);
        });
    });
}

function registerValueAccessorTests(): void {
    describe('value accessor', () => {
        it('should write value via CVA', () => {
            component.writeValue(HIGH_LEVEL);
            fixture.detectChanges();

            expect(component['value']).toBe(HIGH_LEVEL);

            const levelButtons = buttons();
            const selectedButton = levelButtons[HIGH_LEVEL_INDEX];
            expect(selectedButton.classList).toContain('satiety-scale__option--selected');
        });

        it('should mark selected level', () => {
            component.writeValue(SELECTED_LEVEL);
            fixture.detectChanges();

            const levelButtons = buttons();
            for (let i = 0; i < levelButtons.length; i++) {
                if (i === SELECTED_LEVEL_INDEX) {
                    expect(levelButtons[i].classList).toContain('satiety-scale__option--selected');
                } else {
                    expect(levelButtons[i].classList).not.toContain('satiety-scale__option--selected');
                }
            }
        });

        it('should write null value via CVA', () => {
            component.writeValue(HIGH_LEVEL);
            fixture.detectChanges();
            expect(component['value']).toBe(HIGH_LEVEL);

            component.writeValue(null);
            fixture.detectChanges();
            expect(component['value']).toBeNull();

            const selected = host().querySelectorAll<HTMLButtonElement>('.satiety-scale__option--selected');
            expect(selected.length).toBe(0);
        });
    });
}

function registerInteractionTests(): void {
    describe('interaction', () => {
        it('should select level on click', () => {
            const onChangeSpy = vi.fn();
            component.registerOnChange(onChangeSpy);

            const levelButtons = buttons();
            levelButtons[SELECTED_LEVEL_INDEX].click();
            fixture.detectChanges();

            expect(onChangeSpy).toHaveBeenCalledWith(SELECTED_LEVEL);
            expect(component['value']).toBe(SELECTED_LEVEL);
        });

        it('should not select when disabled', () => {
            const onChangeSpy = vi.fn();
            component.registerOnChange(onChangeSpy);
            component.setDisabledState(true);
            fixture.detectChanges();

            const levelButtons = buttons();
            levelButtons[DISABLED_LEVEL_INDEX].click();
            fixture.detectChanges();

            expect(onChangeSpy).not.toHaveBeenCalled();
            expect(component['value']).toBeNull();
        });

        it('should call onTouched when level is selected', () => {
            const onTouchedSpy = vi.fn();
            component.registerOnTouched(onTouchedSpy);

            const levelButtons = buttons();
            levelButtons[LOW_LEVEL_INDEX].click();

            expect(onTouchedSpy).toHaveBeenCalled();
        });

        it('should emit levelSelected output on click', () => {
            const emitSpy = vi.spyOn(component['levelSelected'], 'emit');

            const levelButtons = buttons();
            levelButtons[HIGH_LEVEL_INDEX].click();

            expect(emitSpy).toHaveBeenCalledWith(HIGH_LEVEL);
        });
    });
}

function registerLayoutTests(): void {
    describe('layout', () => {
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
    });
}
