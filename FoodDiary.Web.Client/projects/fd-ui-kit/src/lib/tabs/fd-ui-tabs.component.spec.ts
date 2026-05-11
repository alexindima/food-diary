import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { type FdUiTab, FdUiTabsComponent } from './fd-ui-tabs.component';

const TAB_COUNT = 3;

describe('FdUiTabsComponent', () => {
    let fixture: ComponentFixture<FdUiTabsComponent>;
    let component: FdUiTabsComponent;

    const testTabs: FdUiTab[] = [
        { value: 'products', label: 'Products' },
        { value: 'recipes', label: 'Recipes' },
        { value: 'meals', label: 'Meals' },
    ];

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const tabs = (): HTMLButtonElement[] => Array.from(host().querySelectorAll<HTMLButtonElement>('.fd-ui-tabs__tab'));

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiTabsComponent, TranslateModule.forRoot()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiTabsComponent);
        component = fixture.componentInstance;

        fixture.componentRef.setInput('tabs', testTabs);
        fixture.componentRef.setInput('selectedValue', 'products');
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render tabs', () => {
        const tabLabels = tabs();
        expect(tabLabels.length).toBe(TAB_COUNT);
    });

    it('should set selected tab based on selectedValue', () => {
        let tabLabels = tabs();
        expect(tabLabels[0].getAttribute('aria-selected')).toBe('true');

        fixture.componentRef.setInput('selectedValue', 'recipes');
        fixture.detectChanges();

        tabLabels = tabs();
        expect(tabLabels[1].getAttribute('aria-selected')).toBe('true');
    });

    it('should emit selected value when clicking a tab', () => {
        const emitted: string[] = [];
        component.selectedValueChange.subscribe(value => emitted.push(value));

        const tabButtons = tabs();
        tabButtons[1].click();
        fixture.detectChanges();

        expect(component.selectedValue()).toBe('recipes');
        expect(emitted).toEqual(['recipes']);
    });

    it('should support keyboard navigation', () => {
        const tabButtons = fixture.debugElement.queryAll(By.css('.fd-ui-tabs__tab'));
        const secondTab = tabs()[1];

        tabButtons[0].triggerEventHandler('keydown', new KeyboardEvent('keydown', { key: 'ArrowRight' }));
        fixture.detectChanges();

        expect(component.selectedValue()).toBe('recipes');
        expect(document.activeElement).toBe(secondTab);
    });
});
