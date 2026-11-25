import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import { AdminMealPlansComponent } from './admin-meal-plans';

it('loads and saves a selected catalog draft through the page facade, then refreshes the list', async () => {
    TestBed.configureTestingModule({
        imports: [AdminMealPlansComponent],
        providers: [provideTranslateTesting(), provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(AdminMealPlansComponent);
    const host = fixture.nativeElement as HTMLElement;
    const url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/meal-plans`;
    const id = 'owned-plan';
    const summary = { id, name: 'Starter plan', dietType: 'Balanced', durationDays: 1, isCurated: false };
    const days = [{ dayNumber: 1, meals: [] }];
    fixture.detectChanges();
    http.expectOne(request => request.url === url && request.params.get('page') === '1').flush([summary]);
    await fixture.whenStable();
    fixture.detectChanges();

    const edit = host.querySelector<HTMLButtonElement>('article button');
    expect(edit).not.toBeNull();
    edit?.click();
    fixture.detectChanges();
    http.expectOne({ method: 'GET', url: `${url}/${id}` }).flush({ ...summary, days });
    fixture.detectChanges();
    http.expectOne(request => request.url === `${url}/recipes` && request.params.get('search') === '').flush([]);
    fixture.detectChanges();

    const name = host.querySelector<HTMLInputElement>('form input');
    if (name === null) {
        throw new Error('Expected a plan name field');
    }
    name.value = 'Revised plan';
    name.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    const save = [...host.querySelectorAll<HTMLButtonElement>('form button')].find(
        control => control.textContent.trim() === 'ADMIN_MEAL_PLANS.SAVE',
    );
    expect(save).toBeDefined();
    save?.click();

    const update = http.expectOne({ method: 'PUT', url: `${url}/${id}` });
    const requestBody = update.request.serializeBody();
    if (typeof requestBody !== 'string') {
        throw new Error('Expected JSON meal plan request');
    }
    expect(JSON.parse(requestBody)).toEqual({
        name: 'Revised plan',
        description: null,
        dietType: 'Balanced',
        durationDays: 1,
        targetCaloriesPerDay: null,
        isPublished: false,
        days,
    });
    const revised = { ...summary, name: 'Revised plan' };
    update.flush({ ...revised, days });
    fixture.detectChanges();
    http.expectOne(request => request.url === url && request.params.get('page') === '1').flush([revised]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.querySelector('article h2')?.textContent).toBe(revised.name);
    expect(host.querySelector('form')).toBeNull();
    http.verify();
});
