import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { By } from '@angular/platform-browser';
import { FdUiTabsComponent, FdUiTab } from './fd-ui-tabs.component';

describe('FdUiTabsComponent', () => {
    let fixture: ComponentFixture<FdUiTabsComponent>;
    let component: FdUiTabsComponent;

    const testTabs: FdUiTab[] = [
        { value: 'products', label: 'Products' },
        { value: 'recipes', label: 'Recipes' },
        { value: 'meals', label: 'Meals' },
    ];

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
        const tabLabels = fixture.nativeElement.querySelectorAll('.fd-ui-tabs__tab');
        expect(tabLabels.length).toBe(3);
    });

    it('should set selected tab based on selectedValue', () => {
        let tabLabels = fixture.nativeElement.querySelectorAll('.fd-ui-tabs__tab');
        expect(tabLabels[0].getAttribute('aria-selected')).toBe('true');

        fixture.componentRef.setInput('selectedValue', 'recipes');
        fixture.detectChanges();

        tabLabels = fixture.nativeElement.querySelectorAll('.fd-ui-tabs__tab');
        expect(tabLabels[1].getAttribute('aria-selected')).toBe('true');
    });

    it('should emit selected value when clicking a tab', () => {
        const emitted: string[] = [];
        component.selectedValueChange.subscribe(value => emitted.push(value));

        const tabButtons = fixture.debugElement.queryAll(By.css('.fd-ui-tabs__tab'));
        tabButtons[1].nativeElement.click();
        fixture.detectChanges();

        expect(component.selectedValue()).toBe('recipes');
        expect(emitted).toEqual(['recipes']);
    });

    it('should support keyboard navigation', () => {
        const tabButtons = fixture.debugElement.queryAll(By.css('.fd-ui-tabs__tab'));
        const secondTab = tabButtons[1].nativeElement as HTMLButtonElement;

        tabButtons[0].triggerEventHandler('keydown', new KeyboardEvent('keydown', { key: 'ArrowRight' }));
        fixture.detectChanges();

        expect(component.selectedValue()).toBe('recipes');
        expect(document.activeElement).toBe(secondTab);
    });
});
