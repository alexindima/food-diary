import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { expect, it } from 'vitest';

import { stubScrollIntoView } from '../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminCatalogFilterComponent, matchesAdminCatalog } from './admin-catalog-filter';

const records = [
    { text: 'Каша с яблоком', locale: 'ru', category: 'Breakfast', isActive: true },
    { text: 'Каша на воде', locale: 'ru', category: 'Breakfast', isActive: false },
    { text: 'Каша с тыквой', locale: 'ru', category: 'Dinner', isActive: false },
    { text: 'Baked oats', locale: 'en', category: 'Breakfast', isActive: false },
];

@Component({
    imports: [AdminCatalogFilterComponent],
    template: `
        <fd-admin-catalog-filter [categories]="['Breakfast', 'Dinner']" [showState]="true" />
        <ul>
            @for (record of filtered(); track record.text) {
                <li>{{ record.text }}</li>
            }
        </ul>
    `,
})
class CatalogFilterHost {
    private readonly params = toSignal(inject(ActivatedRoute).queryParamMap, { requireSync: true });
    protected readonly filtered = computed(() => records.filter(record => matchesAdminCatalog(this.params(), record)));
}

async function openCatalogAsync(url: string): Promise<{ harness: RouterTestingHarness; root: HTMLElement; router: Router }> {
    stubScrollIntoView();
    await TestBed.configureTestingModule({
        providers: [provideRouter([{ path: 'list', component: CatalogFilterHost }]), ...provideTranslateTesting()],
    }).compileComponents();
    const harness = await RouterTestingHarness.create(url);
    const root = harness.routeNativeElement;
    if (root === null) {
        throw new Error('Missing catalog route');
    }
    return { harness, root, router: TestBed.inject(Router) };
}

function chooseCatalogOption(harness: RouterTestingHarness, control: HTMLButtonElement, label: string): void {
    control.click();
    harness.detectChanges();
    const option = Array.from(document.querySelectorAll<HTMLElement>('[role="option"]')).find(item => item.textContent.trim() === label);
    if (option === undefined) {
        throw new Error(`Missing catalog option ${label}`);
    }
    option.click();
    harness.detectChanges();
}

it('restores catalog filters and applies changed search and status while clearing a stale page', async () => {
    const url = '/list?locale=ru&category=Breakfast&state=active&page=3&section=kept';
    const { harness, root, router } = await openCatalogAsync(url);
    const search = root.querySelector<HTMLInputElement>('input');
    const state = root.querySelector<HTMLButtonElement>('button#admin-catalog-filter-field-3');
    const apply = Array.from(root.querySelectorAll('button')).find(button => button.textContent.trim() === 'ADMIN_COMMON.APPLY');
    if (search === null || state === null || apply === undefined) {
        throw new Error('Missing catalog filter control');
    }
    expect(root.querySelector('li')?.textContent).toBe('Каша с яблоком');
    search.value = 'КАША';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    chooseCatalogOption(harness, state, 'ADMIN_PROMPTS.INACTIVE');
    expect(router.url).toBe(url);
    apply.click();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(router.parseUrl(router.url).queryParams).toEqual({
        locale: 'ru',
        category: 'Breakfast',
        state: 'inactive',
        search: 'КАША',
        section: 'kept',
    });
    expect(Array.from(root.querySelectorAll('li'), row => row.textContent)).toEqual(['Каша на воде']);
});

it('clears applied catalog criteria and restores the full list', async () => {
    const url = '/list?locale=ru&category=Breakfast&state=active&search=каша&page=3&section=kept';
    const { harness, root, router } = await openCatalogAsync(url);
    const search = root.querySelector<HTMLInputElement>('input');
    const apply = Array.from(root.querySelectorAll('button')).find(button => button.textContent.trim() === 'ADMIN_COMMON.APPLY');
    if (search === null || apply === undefined) {
        throw new Error('Missing catalog filter control');
    }
    expect(search.value).toBe('каша');
    expect(Array.from(root.querySelectorAll('li'), row => row.textContent)).toEqual(['Каша с яблоком']);
    search.value = '';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    for (const control of root.querySelectorAll<HTMLButtonElement>('fd-admin-catalog-filter fd-ui-select button')) {
        chooseCatalogOption(harness, control, 'ADMIN_COMMON.ALL');
    }
    expect(router.parseUrl(router.url).queryParamMap.get('state')).toBe('active');
    apply.click();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(router.parseUrl(router.url).queryParams).toEqual({ section: 'kept' });
    expect(Array.from(root.querySelectorAll('li'), row => row.textContent)).toEqual(records.map(record => record.text));
});
