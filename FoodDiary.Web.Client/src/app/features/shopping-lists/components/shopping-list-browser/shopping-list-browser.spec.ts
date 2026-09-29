import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { ShoppingListService } from '../../api/shopping-list.service';
import { ShoppingListBrowserComponent } from './shopping-list-browser';

afterEach(() => vi.useRealTimers());
describe('ShoppingListBrowserComponent', () => {
    it('does not request the first page when overview data is available', async () => {
        vi.useFakeTimers();
        const getPage = vi.fn();
        await TestBed.configureTestingModule({
            imports: [ShoppingListBrowserComponent],
            providers: [provideTranslateTesting(), { provide: ShoppingListService, useValue: { getPage } }],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingListBrowserComponent);
        fixture.componentRef.setInput('lists', []);
        fixture.componentRef.setInput('initialPage', { items: [], hasMore: false, nextPage: null });
        fixture.detectChanges();
        await vi.runAllTimersAsync();
        expect(getPage).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('.list-browser__load-more')).toBeNull();
    });
    it('appends the next page on click and removes the button after the last page', async () => {
        const next = { id: 'next', name: 'Next list', itemsCount: 0, remainingCount: 0, createdAt: '' };
        const first = { ...next, id: 'first', name: 'First list' };
        const getPage = vi.fn().mockReturnValue(of([next]));
        await TestBed.configureTestingModule({
            imports: [ShoppingListBrowserComponent],
            providers: [provideTranslateTesting(), { provide: ShoppingListService, useValue: { getPage } }],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingListBrowserComponent);
        fixture.componentRef.setInput('lists', [first]);
        fixture.componentRef.setInput('initialPage', { items: [first], hasMore: true, nextPage: 2 });
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector<HTMLButtonElement>('.list-browser__load-more button');
        expect(button).not.toBeNull();
        button?.click();
        fixture.detectChanges();
        expect(getPage).toHaveBeenCalledExactlyOnceWith(2, '');
        expect(element.textContent).toContain('First list');
        expect(element.textContent).toContain('Next list');
        expect(element.querySelector('.list-browser__load-more')).toBeNull();
    });
    it('allows creating a list after an empty search without focusing search on mobile', async () => {
        vi.useFakeTimers();
        await TestBed.configureTestingModule({
            imports: [ShoppingListBrowserComponent],
            providers: [
                provideTranslateTesting(),
                { provide: ShoppingListService, useValue: { getPage: vi.fn().mockReturnValue(of([])) } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(ShoppingListBrowserComponent);
        fixture.componentRef.setInput('lists', []);
        fixture.componentRef.setInput('focusSearch', false);
        const create = vi.fn();
        fixture.componentInstance.createRequested.subscribe(create);
        fixture.componentInstance['search'].set('  Покупки на Дачу  ');
        fixture.detectChanges();
        const searchDelay = 300;
        await vi.advanceTimersByTimeAsync(searchDelay);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        element.querySelector<HTMLButtonElement>('.list-browser__create button')?.click();
        expect(create).toHaveBeenCalledWith('Покупки на Дачу');
        expect(element.querySelector('input')).not.toBe(element.ownerDocument.activeElement);
    });
});
