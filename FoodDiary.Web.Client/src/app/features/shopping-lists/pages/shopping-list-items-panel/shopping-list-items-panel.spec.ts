import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form, required } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { ShoppingListItem } from '../../../../shared/models/shopping-list.data';
import type { ShoppingListItemFormModel } from '../../lib/shopping-list-form.types';
import { ShoppingListItemsPanelComponent } from './shopping-list-items-panel';

const CHECKED_SETTLE_MS = 300;
const CHECKED_ITEM: ShoppingListItem = {
    id: entityId<'shopping-list-item'>('item-1'),
    shoppingListId: entityId<'shopping-list'>('list-1'),
    name: 'Milk',
    amount: 2,
    unit: 'l',
    category: 'Dairy',
    aisle: 'Dairy',
    note: null,
    isChecked: true,
    checkedOnUtc: null,
    sources: [],
    sortOrder: 1,
};

function createItemForm(): FieldTree<ShoppingListItemFormModel> {
    const model = signal<ShoppingListItemFormModel>({
        name: '',
        amount: null,
        unit: null,
        category: null,
        note: null,
    });

    return TestBed.runInInjectionContext(() =>
        form(model, path => {
            required(path.name);
        }),
    );
}

async function setupItemsPanelAsync(items: ShoppingListItem[] = [CHECKED_ITEM]): Promise<{
    component: ShoppingListItemsPanelComponent;
    fixture: ComponentFixture<ShoppingListItemsPanelComponent>;
}> {
    await TestBed.configureTestingModule({
        imports: [ShoppingListItemsPanelComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(ShoppingListItemsPanelComponent);
    fixture.componentRef.setInput('itemForm', createItemForm());
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

describe('ShoppingListItemsPanelComponent', () => {
    it('renders exactly one purchased summary and only makes it interactive for purchased items', async () => {
        const { fixture } = await setupItemsPanelAsync([{ ...CHECKED_ITEM, isChecked: false }]);
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelectorAll('.shopping-list__purchased-summary')).toHaveLength(1);
        expect(element.querySelector('.shopping-list__purchased-toggle')).toBeNull();

        fixture.componentRef.setInput('items', [CHECKED_ITEM]);
        fixture.detectChanges();
        expect(element.querySelector('.shopping-list__purchased-summary')).toBeNull();
        expect(element.querySelectorAll('.shopping-list__purchased-toggle')).toHaveLength(1);

        fixture.componentRef.setInput('items', [{ ...CHECKED_ITEM, isChecked: false }]);
        fixture.detectChanges();
        expect(element.querySelectorAll('.shopping-list__purchased-summary')).toHaveLength(1);
        expect(element.querySelector('.shopping-list__purchased-toggle')).toBeNull();
    });

    it('separates purchased items and returns unchecked items to the shopping rows', async () => {
        const { component, fixture } = await setupItemsPanelAsync([
            CHECKED_ITEM,
            { ...CHECKED_ITEM, id: entityId<'shopping-list-item'>('pending'), isChecked: false },
        ]);
        expect(component['purchasedItems']().map(item => item.id)).toEqual(['item-1']);
        expect(component['pendingItems']().map(item => item.id)).toEqual(['pending']);
        fixture.componentRef.setInput('items', [{ ...CHECKED_ITEM, isChecked: false }]);
        fixture.detectChanges();
        expect(component['purchasedItems']()).toEqual([]);
        expect(component['pendingItems']()[0].id).toBe('item-1');
    });

    it('exposes purchased group expansion on the focusable toggle', async () => {
        const { fixture } = await setupItemsPanelAsync();
        const element = fixture.nativeElement as HTMLElement;
        const toggle = element.querySelector<HTMLButtonElement>('.shopping-list__purchased-toggle button');
        if (toggle === null) {
            throw new Error('Purchased group toggle is missing');
        }
        expect(toggle.getAttribute('aria-expanded')).toBe('true');
        expect(element.querySelectorAll('.shopping-list__purchased li')).toHaveLength(1);
        toggle.click();
        fixture.detectChanges();
        expect(toggle.getAttribute('aria-expanded')).toBe('false');
        expect(element.querySelectorAll('.shopping-list__purchased li')).toHaveLength(0);
    });

    it('builds localized unit options and item view models', async () => {
        const { component } = await setupItemsPanelAsync();

        expect(component['unitOptions']().length).toBeGreaterThan(0);
        expect(component['itemViewModels']()[0]).toMatchObject({
            id: CHECKED_ITEM.id,
            name: CHECKED_ITEM.name,
            isChecked: true,
        });
        expect(component['itemViewModels']()[0].meta).toContain('2');
    });

    it('emits add, remove, and checked change events', async () => {
        const { component } = await setupItemsPanelAsync();
        const addSpy = vi.fn();
        const removeSpy = vi.fn();
        const checkedSpy = vi.fn();
        component.itemAdd.subscribe(addSpy);
        component.itemRemove.subscribe(removeSpy);
        component.itemCheckedChange.subscribe(checkedSpy);

        component.itemAdd.emit();
        component.itemRemove.emit(CHECKED_ITEM.id);
        component.itemCheckedChange.emit({ itemId: CHECKED_ITEM.id, checked: false });

        expect(addSpy).toHaveBeenCalledOnce();
        expect(removeSpy).toHaveBeenCalledWith(CHECKED_ITEM.id);
        expect(checkedSpy).toHaveBeenCalledWith({ itemId: CHECKED_ITEM.id, checked: false });
    });

    it('renders empty state view model for empty items', async () => {
        const { component, fixture } = await setupItemsPanelAsync([]);

        expect(component['itemViewModels']()).toEqual([]);
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.shopping-list__purchased-toggle')).toBeNull();
        expect(element.querySelector<HTMLInputElement>('.shopping-list__quick-add-input input')?.placeholder).toBe(
            'SHOPPING_LIST.ADD_ITEM_NAME_PLACEHOLDER',
        );
    });
});

describe('ShoppingListItemsPanelComponent localization', () => {
    it('updates fractional quantities when the interface language changes', async () => {
        const { fixture } = await setupItemsPanelAsync([{ ...CHECKED_ITEM, amount: 2.5, isChecked: false }]);
        const translateService = TestBed.inject(TranslateService);
        const element = fixture.nativeElement as HTMLElement;
        translateService.use('ru');
        fixture.detectChanges();
        expect(element.querySelector('.shopping-list__item-quantity')?.textContent).toBe('2,5 l');
        translateService.use('en');
        fixture.detectChanges();
        expect(element.querySelector('.shopping-list__item-quantity')?.textContent).toBe('2.5 l');
    });
});

describe('ShoppingListItemsPanelComponent focus after moving an item', () => {
    it.each([true, false])('focuses the same item when its purchased state becomes %s', async checked => {
        const { fixture, component } = await setupItemsPanelAsync([{ ...CHECKED_ITEM, isChecked: !checked }]);
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        onTestFinished(() => {
            vi.useRealTimers();
        });
        const host = fixture.nativeElement as HTMLElement;
        component.itemCheckedChange.subscribe(event => {
            fixture.componentRef.setInput('items', [{ ...CHECKED_ITEM, isChecked: event.checked }]);
        });
        const source = checkboxIn(checked ? '.shopping-list__items' : '.shopping-list__purchased', host);
        source.focus();
        source.click();
        fixture.detectChanges();
        vi.advanceTimersByTime(CHECKED_SETTLE_MS);
        fixture.detectChanges();

        const destination = checkboxIn(checked ? '.shopping-list__purchased' : '.shopping-list__items', host);
        expect(source.isConnected).toBe(false);
        expect(destination.checked).toBe(checked);
        expect(document.activeElement).toBe(destination);
    });

    it('leaves focus where the user moved it before the checked item settles', async () => {
        const { fixture, component } = await setupItemsPanelAsync([{ ...CHECKED_ITEM, isChecked: false }]);
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        onTestFinished(() => {
            vi.useRealTimers();
        });
        const host = fixture.nativeElement as HTMLElement;
        component.itemCheckedChange.subscribe(() => {
            fixture.componentRef.setInput('items', [CHECKED_ITEM]);
        });
        const source = checkboxIn('.shopping-list__items', host);
        source.focus();
        source.click();
        fixture.detectChanges();
        const outside = host.querySelector<HTMLInputElement>('.shopping-list__quick-add-input input');
        outside?.focus();
        vi.advanceTimersByTime(CHECKED_SETTLE_MS);
        fixture.detectChanges();

        expect(source.isConnected).toBe(false);
        expect(document.activeElement).toBe(outside);
    });
});

it('focuses the purchased toggle when the moved checkbox is in a collapsed group', async () => {
    const pending = { ...CHECKED_ITEM, id: entityId<'shopping-list-item'>('pending'), isChecked: false };
    const { fixture, component } = await setupItemsPanelAsync([pending, CHECKED_ITEM]);
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    onTestFinished(() => {
        vi.useRealTimers();
    });
    const host = fixture.nativeElement as HTMLElement;
    const toggle = host.querySelector<HTMLButtonElement>('.shopping-list__purchased-toggle button');
    toggle?.click();
    fixture.detectChanges();
    component.itemCheckedChange.subscribe(() => {
        fixture.componentRef.setInput('items', [{ ...pending, isChecked: true }, CHECKED_ITEM]);
    });

    const source = checkboxIn('.shopping-list__items', host);
    source.focus();
    source.click();
    fixture.detectChanges();
    vi.advanceTimersByTime(CHECKED_SETTLE_MS);
    fixture.detectChanges();

    expect(source.isConnected).toBe(false);
    expect(toggle?.getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(toggle);
});

function checkboxIn(selector: string, host: HTMLElement): HTMLInputElement {
    const checkbox = host.querySelector<HTMLInputElement>(`${selector} input[type="checkbox"]`);
    if (checkbox === null) {
        throw new Error(`Expected a checkbox in ${selector}`);
    }
    return checkbox;
}
