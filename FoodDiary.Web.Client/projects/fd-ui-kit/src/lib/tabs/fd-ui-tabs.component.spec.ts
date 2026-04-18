import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { MatTabGroup } from '@angular/material/tabs';
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
            providers: [provideNoopAnimations()],
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
        const tabGroup = fixture.debugElement.query(By.directive(MatTabGroup));
        expect(tabGroup).toBeTruthy();

        const tabLabels = fixture.nativeElement.querySelectorAll('.mat-mdc-tab');
        expect(tabLabels.length).toBe(3);
    });

    it('should set selected tab based on selectedValue', () => {
        const tabGroup = fixture.debugElement.query(By.directive(MatTabGroup));
        expect(tabGroup.componentInstance.selectedIndex).toBe(0);

        fixture.componentRef.setInput('selectedValue', 'recipes');
        fixture.detectChanges();

        expect(tabGroup.componentInstance.selectedIndex).toBe(1);
    });
});
