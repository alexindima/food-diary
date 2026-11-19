import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { type FdUiTab, FdUiTabsComponent } from './fd-ui-tabs';

const TAB_COUNT = 3;

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
        imports: [FdUiTabsComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
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
    component['selectedValue'].subscribe(value => emitted.push(value));

    const tabButtons = tabs();
    tabButtons[1].click();
    fixture.detectChanges();

    expect(component['selectedValue']()).toBe('recipes');
    expect(emitted).toEqual(['recipes']);
});

it('should support keyboard navigation', () => {
    const tabButtons = fixture.debugElement.queryAll(By.css('.fd-ui-tabs__tab'));
    const secondTab = tabs()[1];

    tabButtons[0].triggerEventHandler('keydown', new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(component['selectedValue']()).toBe('recipes');
    expect(document.activeElement).toBe(secondTab);
});

it.each(['ArrowUp', 'ArrowDown'])('leaves %s available for page scrolling', key => {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    tabs()[0].dispatchEvent(event);
    fixture.detectChanges();
    expect(event.defaultPrevented).toBe(false);
    expect(component.selectedValue()).toBe('products');
});

it.each([
    { key: 'ArrowLeft', from: 0, to: 2 },
    { key: 'ArrowRight', from: 2, to: 0 },
    { key: 'Home', from: 1, to: 0 },
    { key: 'End', from: 1, to: 2 },
])('moves focus and selection with $key from tab $from to tab $to', ({ key, from, to }) => {
    const buttons = tabs();
    fixture.componentRef.setInput('selectedValue', testTabs[from].value);
    fixture.detectChanges();
    buttons[from].focus();

    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    buttons[from].dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
    expect(component.selectedValue()).toBe(testTabs[to].value);
    expect(document.activeElement).toBe(buttons[to]);
    expect(buttons[to].getAttribute('aria-selected')).toBe('true');
    expect(buttons[from].tabIndex).toBe(-1);
});

it.each(['Enter', ' '])('activates the focused tab with %j without the native button action', key => {
    const [, recipes] = tabs();
    recipes.focus();
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    recipes.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(recipes);
    expect(component.selectedValue()).toBe('recipes');
    expect(recipes.getAttribute('aria-selected')).toBe('true');
});

it('updates translated tab names after switching language while preserving selection', () => {
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { RECIPES: 'Recipes' });
    translate.setTranslation('fr', { RECIPES: 'Recettes' });
    translate.use('en');
    fixture.componentRef.setInput('tabs', [{ value: 'recipes', labelKey: 'RECIPES' }]);
    fixture.componentRef.setInput('selectedValue', 'recipes');
    fixture.detectChanges();
    expect(tabs()[0].textContent.trim()).toBe('Recipes');

    translate.use('fr');
    fixture.detectChanges();

    expect(tabs()[0].textContent.trim()).toBe('Recettes');
    expect(tabs()[0].getAttribute('aria-selected')).toBe('true');
});

it('names a tablist and points each tab at its associated panel', () => {
    fixture.componentRef.setInput('ariaLabel', 'Food details');
    fixture.detectChanges();
    expect(host().querySelector('[role="tablist"]')?.getAttribute('aria-label')).toBe('Food details');
    for (const tab of tabs()) {
        expect(tab.getAttribute('aria-controls')).toBeTruthy();
    }
});

it('uses pressed buttons for selection without claiming tab panels', () => {
    fixture.componentRef.setInput('mode', 'selection');
    fixture.componentRef.setInput('ariaLabel', 'Period');
    fixture.detectChanges();
    expect(host().querySelector('[role="tablist"]')).toBeNull();
    expect(host().querySelector('[role="group"]')?.getAttribute('aria-label')).toBe('Period');
    expect(tabs()[0].getAttribute('aria-pressed')).toBe('true');
    expect(tabs()[1].tabIndex).toBe(0);
    const arrow = new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true });
    tabs()[0].dispatchEvent(arrow);
    expect(arrow.defaultPrevented).toBe(false);
    expect(component.selectedValue()).toBe('products');
    tabs()[1].click();
    fixture.detectChanges();
    expect(tabs()[1].getAttribute('aria-pressed')).toBe('true');
    expect(tabs()[1].hasAttribute('aria-controls')).toBe(false);
});

it('ignores outstanding interactions from rendered tabs when the source list is cleared', () => {
    const [, staleTab] = tabs();
    fixture.componentRef.setInput('tabs', []);

    const key = new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true });
    staleTab.dispatchEvent(key);
    staleTab.click();
    expect(key.defaultPrevented).toBe(false);
    expect(component.selectedValue()).toBe('products');

    fixture.detectChanges();
    expect(tabs()).toHaveLength(0);
});

it('keeps stable panel associations while an option waits for its label', () => {
    const value = 'meals/current';
    fixture.componentRef.setInput('tabs', [{ value }]);
    fixture.componentRef.setInput('selectedValue', value);
    fixture.detectChanges();
    const [tab] = tabs();
    expect(tab.id).toBe(component.tabId(value));
    expect(tab.id).toContain('meals%2Fcurrent');
    expect(tab.getAttribute('aria-controls')).toBe(component.panelId(value));
    expect(tab.textContent.trim()).toBe('');

    fixture.componentRef.setInput('tabs', [{ value, label: 'Current meals' }]);
    fixture.detectChanges();
    expect(tabs()[0]).toBe(tab);
    expect(tab.textContent.trim()).toBe('Current meals');
    expect(tab.getAttribute('aria-selected')).toBe('true');
});

it('exposes real route links and identifies the current page', () => {
    fixture.componentRef.setInput('mode', 'navigation');
    fixture.componentRef.setInput('tabs', [
        { value: '/inbox', label: 'Inbox' },
        { value: '/sent', label: 'Sent' },
    ]);
    fixture.componentRef.setInput('selectedValue', '/inbox');
    fixture.detectChanges();
    const links = host().querySelectorAll('a');
    expect(links[0].getAttribute('href')).toBe('/inbox');
    expect(links[0].getAttribute('aria-current')).toBe('page');
    expect(links[1].hasAttribute('aria-current')).toBe(false);
    expect(host().querySelector('[role="tab"]')).toBeNull();
});
