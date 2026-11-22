import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { NAME_SEARCH_DEBOUNCE_MS } from '../../../../../config/runtime-ui.tokens';
import { type ProductSearchSuggestion, ProductVisibility } from '../../../../../shared/models/product.data';
import { ProductService } from '../../../api/product.service';
import { ProductNameSearchFacade } from '../../../lib/manage/product-name-search.facade';
import { createProductForm } from '../product-manage-lib/product-manage-form.mapper';
import type { ProductNameAutocompleteOption, ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';
import { ProductBasicInfoComponent } from './product-basic-info';

it('preserves a saved product name and sends an actual catalogue selection from the name field', async () => {
    stubScrollIntoView();
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    TestBed.configureTestingModule({
        imports: [ProductBasicInfoComponent],
        providers: [provideTranslateTesting(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const savedName = 'Stone-ground rye flour';
    const data = signal({ ...createProductForm(), name: savedName });
    const nameForm = TestBed.runInInjectionContext(() => form(data));
    const fixture = TestBed.createComponent(ProductBasicInfoComponent);
    fixture.componentRef.setInput('form', nameForm);
    fixture.componentRef.setInput('nameOptions', []);
    fixture.componentRef.setInput('isNameSearchLoading', false);
    const changed = vi.fn<(query: string) => void>();
    const selected = vi.fn<(suggestion: ProductNameSuggestion) => void>();
    fixture.componentInstance.nameQueryChange.subscribe(changed);
    fixture.componentInstance.nameSuggestionSelected.subscribe(selected);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const input = host.querySelector<HTMLInputElement>('fd-ui-autocomplete input[role="combobox"]');
    if (input === null) {
        throw new Error('Expected the product name field');
    }
    expect(input.value).toBe(savedName);
    input.focus();
    input.value = 'rye';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    expect(changed).toHaveBeenCalledExactlyOnceWith('rye');
    expect(data().name).toBe('rye');

    const suggestion: ProductNameSuggestion = { source: 'openFoodFacts', name: 'Rye flour', barcode: '4600000000000' };
    const option: ProductNameAutocompleteOption = { id: 'rye-flour', label: suggestion.name, value: suggestion.name, data: suggestion };
    fixture.componentRef.setInput('nameOptions', [option]);
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(document.querySelector('[role="option"]')?.textContent).toContain(suggestion.name);
    });
    document.querySelector<HTMLElement>('[role="option"]')?.click();
    fixture.componentRef.setInput('nameOptions', []);
    fixture.detectChanges();

    expect(selected).toHaveBeenCalledExactlyOnceWith(suggestion);
    expect(data().name).toBe(suggestion.name);
    expect(input.value).toBe(suggestion.name);
    expect(input.getAttribute('aria-expanded')).toBe('false');
});

it('keeps the visibility summary and selector in sync after a choice and a locale change', async () => {
    const { fixture } = renderNameSearch();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { PRODUCT_MANAGE: { VISIBILITY_OPTIONS: { PRIVATE: 'Private', PUBLIC: 'Public' } } });
    translate.setTranslation('ru', { PRODUCT_MANAGE: { VISIBILITY_OPTIONS: { PRIVATE: 'Личный', PUBLIC: 'Публичный' } } });
    await firstValueFrom(translate.use('en'));
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const summary = host.querySelector<HTMLElement>('.product-manage__additional summary');
    const select = host.querySelector<HTMLButtonElement>('.product-manage__additional button[aria-haspopup="listbox"]');
    if (summary === null || select === null) {
        throw new Error('Expected the product visibility summary and selector');
    }
    expect(summary.textContent).toContain('Private');
    summary.click();
    expect(summary.closest('details')?.open).toBe(true);
    select.click();
    fixture.detectChanges();
    const publicOption = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="option"]')).find(
        option => option.textContent.trim() === 'Public',
    );
    if (publicOption === undefined) {
        throw new Error('Expected the public visibility option');
    }
    publicOption.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.draft().visibility).toBe(ProductVisibility.Public);
    expect(summary.textContent).toContain('Public');
    expect(select.textContent).toContain('Public');

    await firstValueFrom(translate.use('ru'));
    fixture.detectChanges();
    expect(fixture.componentInstance.draft().visibility).toBe(ProductVisibility.Public);
    expect(select.textContent).toContain('Публичный');
    summary.click();
    expect(summary.closest('details')?.open).toBe(false);
    expect(summary.textContent).toContain('Публичный');
});

@Component({
    imports: [ProductBasicInfoComponent],
    providers: [ProductNameSearchFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
        <fd-product-basic-info
            [form]="fields"
            [nameOptions]="search.options()"
            [isNameSearchLoading]="search.isLoading()"
            (nameQueryChange)="search.search($event)"
            (nameSuggestionSelected)="selection.set($event)"
        />
    `,
})
class ProductNameSearchHostComponent {
    public readonly search = inject(ProductNameSearchFacade);
    public readonly draft = signal(createProductForm());
    public readonly fields = form(this.draft);
    public readonly selection = signal<ProductNameSuggestion | null>(null);
}

const RYE_SUGGESTION: ProductSearchSuggestion = { source: 'usda', name: 'Rye flour', usdaFdcId: 12345 };
const COFFEE_SUGGESTION: ProductSearchSuggestion = { source: 'openFoodFacts', name: 'Ground coffee', barcode: '4600000000000' };
const NAME_OPTION = 'fd-ui-autocomplete-menu button[role="option"]';

describe('Product name suggestions while typing', () => {
    it('removes the previous selection options immediately and opens the new matching option', async () => {
        const { fixture, input, searchSuggestions } = renderNameSearch();
        searchSuggestions.mockReturnValueOnce(of([RYE_SUGGESTION])).mockReturnValueOnce(of([COFFEE_SUGGESTION]));
        typeName(fixture, input, 'rye');
        await waitForOptionAsync(fixture, RYE_SUGGESTION.name);

        typeName(fixture, input, 'coffee');
        expect(document.querySelector(NAME_OPTION)).toBeNull();
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        fixture.detectChanges();
        expect(input.value).toBe('coffee');
        expect(fixture.componentInstance.selection()).toBeNull();

        const current = await waitForOptionAsync(fixture, COFFEE_SUGGESTION.name);
        current.click();
        fixture.detectChanges();
        expect(input.value).toBe(COFFEE_SUGGESTION.name);
        expect(fixture.componentInstance.selection()).toEqual(COFFEE_SUGGESTION);
        expect(searchSuggestions.mock.calls.map(([name]) => name)).toEqual(['rye', 'coffee']);
    });

    it('keeps an older pending response off the field after the user types a shorter name', async () => {
        const { fixture, input, searchSuggestions } = renderNameSearch();
        const pending = new Subject<ProductSearchSuggestion[]>();
        searchSuggestions.mockReturnValueOnce(pending);
        typeName(fixture, input, 'rye');
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });

        typeName(fixture, input, 'co');
        const stillSubscribed = pending.observed;
        pending.next([RYE_SUGGESTION]);
        pending.complete();
        fixture.detectChanges();

        expect(document.querySelector(NAME_OPTION)).toBeNull();
        expect(input.value).toBe('co');
        expect(fixture.componentInstance.draft().name).toBe('co');
        expect(fixture.componentInstance.selection()).toBeNull();
        expect(stillSubscribed).toBe(false);
        expect(searchSuggestions).toHaveBeenCalledOnce();
    });
});

type ProductNameSearchFixture = {
    fixture: ComponentFixture<ProductNameSearchHostComponent>;
    input: HTMLInputElement;
    searchSuggestions: Mock<ProductService['searchSuggestions']>;
};

function renderNameSearch(): ProductNameSearchFixture {
    stubScrollIntoView();
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const searchSuggestions = vi.fn<ProductService['searchSuggestions']>();
    TestBed.configureTestingModule({
        imports: [ProductNameSearchHostComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([]),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: ProductService, useValue: { searchSuggestions } },
            { provide: NAME_SEARCH_DEBOUNCE_MS, useValue: 0 },
        ],
    });
    const fixture = TestBed.createComponent(ProductNameSearchHostComponent);
    fixture.detectChanges();
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('fd-ui-autocomplete input[role="combobox"]');
    if (input === null) {
        throw new Error('Expected the product name field');
    }
    input.focus();
    return { fixture, input, searchSuggestions };
}

function typeName(fixture: ComponentFixture<ProductNameSearchHostComponent>, input: HTMLInputElement, name: string): void {
    input.value = name;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
}

async function waitForOptionAsync(fixture: ComponentFixture<ProductNameSearchHostComponent>, name: string): Promise<HTMLButtonElement> {
    return vi.waitFor(() => {
        fixture.detectChanges();
        const option = document.querySelector<HTMLButtonElement>(NAME_OPTION);
        if (option === null) {
            throw new Error(`Expected the ${name} autocomplete option`);
        }
        expect(option.textContent).toContain(name);
        return option;
    });
}
