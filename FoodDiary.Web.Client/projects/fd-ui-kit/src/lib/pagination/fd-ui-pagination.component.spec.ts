import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { FdUiPaginationComponent } from './fd-ui-pagination.component';

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
        fixture.componentRef.setInput('length', 50);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.detectChanges();

        expect(pageButtons().length).toBe(7);
    });

    it('should set active page from pageIndex', () => {
        fixture.componentRef.setInput('length', 100);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.componentRef.setInput('pageIndex', 3);
        fixture.detectChanges();

        const activeButton = host().querySelector<HTMLButtonElement>('.fd-ui-pagination__button--active');
        expect(activeButton?.textContent.trim()).toBe('4');
    });

    it('should update pageIndex when page clicked', () => {
        fixture.componentRef.setInput('length', 100);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.detectChanges();

        const buttons = pageButtons();
        buttons[2].click();

        expect(component.pageIndex()).toBe(1);
    });
});
