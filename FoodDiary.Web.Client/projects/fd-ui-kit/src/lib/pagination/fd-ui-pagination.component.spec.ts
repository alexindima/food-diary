import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiPaginationComponent } from './fd-ui-pagination.component';

describe('FdUiPaginationComponent', () => {
    let fixture: ComponentFixture<FdUiPaginationComponent>;
    let component: FdUiPaginationComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiPaginationComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiPaginationComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should render page buttons from length and pageSize', () => {
        fixture.componentRef.setInput('length', 50);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.detectChanges();

        const pageButtons = fixture.debugElement.queryAll(By.css('.fd-ui-pagination__button'));
        expect(pageButtons.length).toBe(7);
    });

    it('should set active page from pageIndex', () => {
        fixture.componentRef.setInput('length', 100);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.componentRef.setInput('pageIndex', 3);
        fixture.detectChanges();

        const activeButton = fixture.debugElement.query(By.css('.fd-ui-pagination__button--active'));
        expect(activeButton.nativeElement.textContent.trim()).toBe('4');
    });

    it('should emit pageIndexChange when page clicked', () => {
        const emitted: number[] = [];
        component.pageIndexChange.subscribe(value => emitted.push(value));

        fixture.componentRef.setInput('length', 100);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.detectChanges();

        const pageButtons = fixture.debugElement.queryAll(By.css('.fd-ui-pagination__button'));
        pageButtons[2].nativeElement.click();

        expect(emitted).toEqual([1]);
    });
});
