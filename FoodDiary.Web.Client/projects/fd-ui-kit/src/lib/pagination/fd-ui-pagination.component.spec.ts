import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiPaginationComponent } from './fd-ui-pagination.component';

const SHORT_TOTAL_ITEMS = 50;
const LONG_TOTAL_ITEMS = 100;
const PAGE_SIZE = 10;
const PAGE_INDEX = 3;
const EXPECTED_BUTTON_COUNT = 7;
const ACTIVE_PAGE_LABEL = '4';

describe('FdUiPaginationComponent', () => {
    let fixture: ComponentFixture<FdUiPaginationComponent>;
    let component: FdUiPaginationComponent;

    const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const pageButtons = (): HTMLButtonElement[] => Array.from(host().querySelectorAll<HTMLButtonElement>('.fd-ui-pagination__button'));

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
        fixture.componentRef.setInput('length', SHORT_TOTAL_ITEMS);
        fixture.componentRef.setInput('pageSize', PAGE_SIZE);
        fixture.detectChanges();

        expect(pageButtons().length).toBe(EXPECTED_BUTTON_COUNT);
    });

    it('should set active page from pageIndex', () => {
        fixture.componentRef.setInput('length', LONG_TOTAL_ITEMS);
        fixture.componentRef.setInput('pageSize', PAGE_SIZE);
        fixture.componentRef.setInput('pageIndex', PAGE_INDEX);
        fixture.detectChanges();

        const activeButton = host().querySelector<HTMLButtonElement>('.fd-ui-pagination__button--active');
        expect(activeButton?.textContent.trim()).toBe(ACTIVE_PAGE_LABEL);
    });

    it('should update pageIndex when page clicked', () => {
        fixture.componentRef.setInput('length', LONG_TOTAL_ITEMS);
        fixture.componentRef.setInput('pageSize', PAGE_SIZE);
        fixture.detectChanges();

        const buttons = pageButtons();
        buttons[2].click();

        expect(component.pageIndex()).toBe(1);
    });
});
