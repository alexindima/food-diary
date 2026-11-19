import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminAchievementsService } from '../api/admin-achievements.service';
import { AdminAchievementsFacade } from '../lib/admin-achievements.facade';
import type { AdminAchievementDefinition } from '../models/admin-achievement.data';
import { AdminAchievementsComponent } from './admin-achievements';

const DEFINITION: AdminAchievementDefinition = {
    id: adminId<'achievement-definition'>('definition-id'),
    key: 'meals_20',
    category: 'meals',
    metric: 'TotalMeals',
    threshold: 20,
    titleRu: '20 приёмов',
    titleEn: '20 meals',
    descriptionRu: 'Описание',
    descriptionEn: 'Description',
    icon: 'restaurant',
    sortOrder: 20,
    isActive: true,
    version: 3,
};

const FRACTIONAL_THRESHOLD = 1.5;
const FRACTIONAL_ORDER = 0.5;
const ABOVE_INT32_MAXIMUM = 2147483648;

describe('AdminAchievementsComponent', () => {
    let fixture: ComponentFixture<AdminAchievementsComponent>;
    const facade = {
        getAll: vi.fn(() => of([DEFINITION])),
        create: vi.fn(() => of(DEFINITION)),
        update: vi.fn(() => of({ ...DEFINITION, version: 4 })),
    };

    beforeEach(async () => {
        facade.getAll.mockClear();
        facade.create.mockClear();
        facade.update.mockClear();
        await TestBed.configureTestingModule({
            imports: [AdminAchievementsComponent],
            providers: [provideRouter([]), provideTranslateTesting(), { provide: AdminAchievementsFacade, useValue: facade }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAchievementsComponent);
        fixture.detectChanges();
    });

    it('renders definitions and switches to versioned edit mode', () => {
        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector<HTMLButtonElement>('.definition-card');
        expect(card?.textContent).toContain('20 meals');

        card?.click();
        fixture.detectChanges();

        const keyInput = host.querySelector<HTMLInputElement>('form input');
        expect(keyInput?.disabled).toBe(true);
        expect(host.textContent).toContain('ADMIN_ACHIEVEMENTS.EDIT');
    });

    it('shows a load error instead of an empty catalog and reloads through Retry', () => {
        facade.getAll.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        fixture.componentInstance['load']();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.definition-list [role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
        expect(host.querySelector('.definition-list')?.textContent).not.toContain('ADMIN_ACHIEVEMENTS.EMPTY');
        const retry = [...host.querySelectorAll('button')].find(button => button.textContent.includes('ADMIN_COMMON.RETRY'));
        retry?.click();
        fixture.detectChanges();
        expect(host.querySelector('.definition-list [role="alert"]')).toBeNull();
        expect(host.querySelector('.definition-card')?.textContent).toContain('20 meals');
    });

    it('submits an update with the current version and without the immutable key', () => {
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('.definition-card')?.click();
        fixture.detectChanges();

        host.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));

        expect(facade.update).toHaveBeenCalledWith('definition-id', {
            category: 'meals',
            metric: 'TotalMeals',
            threshold: 20,
            titleRu: '20 приёмов',
            titleEn: '20 meals',
            descriptionRu: 'Описание',
            descriptionEn: 'Description',
            icon: 'restaurant',
            sortOrder: 20,
            isActive: true,
            version: 3,
        });
        expect(facade.getAll).toHaveBeenCalledTimes(2);
    });

    it('retains a refreshed definition when an older list request completes or fails', () => {
        const previous = new Subject<AdminAchievementDefinition[]>();
        const refresh = new Subject<AdminAchievementDefinition[]>();
        facade.getAll.mockReturnValueOnce(previous).mockReturnValueOnce(refresh);
        const component = fixture.componentInstance;
        component['load']();
        component['edit'](DEFINITION);
        component['save']();
        const updated = { ...DEFINITION, version: 4 };
        refresh.next([updated]);
        previous.next([]);
        previous.error(new Error('obsolete request failed'));

        expect(component['definitions']()).toEqual([updated]);
        expect(component['isLoading']()).toBe(false);
        expect(component['loadFailed']()).toBe(false);
    });
});

describe('AdminAchievementsComponent editor validation', () => {
    let fixture: ComponentFixture<AdminAchievementsComponent>;
    const facade = {
        getAll: vi.fn(() => of([DEFINITION])),
        create: vi.fn(() => of(DEFINITION)),
        update: vi.fn(() => of({ ...DEFINITION, version: 4 })),
    };

    beforeEach(async () => {
        facade.update.mockClear();
        await TestBed.configureTestingModule({
            imports: [AdminAchievementsComponent],
            providers: [provideRouter([]), provideTranslateTesting(), { provide: AdminAchievementsFacade, useValue: facade }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAchievementsComponent);
        fixture.detectChanges();
    });

    it.each([
        { field: 'threshold', invalid: [0, FRACTIONAL_THRESHOLD, ABOVE_INT32_MAXIMUM], valid: 1 },
        { field: 'sortOrder', invalid: [-1, FRACTIONAL_ORDER, ABOVE_INT32_MAXIMUM], valid: 0 },
    ] as const)('rejects invalid integer values in $field before submission', ({ field, invalid, valid }) => {
        const component = fixture.componentInstance;
        component['edit'](DEFINITION);
        const host = fixture.nativeElement as HTMLElement;
        const saveButton = (): HTMLButtonElement | undefined =>
            [...host.querySelectorAll<HTMLButtonElement>('button')].find(button => button.textContent.includes('ADMIN_ACHIEVEMENTS.SAVE'));

        for (const value of invalid) {
            component['form'][field]().value.set(value);
            fixture.detectChanges();
            expect(saveButton()?.disabled).toBe(true);
            component['save']();
        }
        expect(facade.update).not.toHaveBeenCalled();

        component['form'][field]().value.set(valid);
        fixture.detectChanges();
        expect(saveButton()?.disabled).toBe(false);
    });

    it('clears a previous save error when resetting or selecting another definition', () => {
        const component = fixture.componentInstance;
        facade.update.mockReturnValueOnce(throwError(() => new Error('unavailable')));
        component['edit'](DEFINITION);
        component['save']();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();

        component['createNew']();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();

        component['error'].set('ADMIN_ACHIEVEMENTS.ERRORS.SAVE');
        component['edit'](DEFINITION);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
    });

    it('clears touched validation when a successful save opens the next blank editor', () => {
        const component = fixture.componentInstance;
        component['edit'](DEFINITION);
        component['form']().markAsTouched();

        component['save']();
        fixture.detectChanges();

        expect(component['editingId']()).toBeNull();
        expect(component['form']().touched(), 'New achievement editor must be untouched').toBe(false);
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
    });
});

describe('AdminAchievementsComponent pending mutation ownership', () => {
    it.each(['success', 'error'])('locks the submitted editor until its %s response arrives', async outcome => {
        const pending = new Subject<AdminAchievementDefinition>();
        const other = { ...DEFINITION, id: adminId<'achievement-definition'>('other-definition'), key: 'other_key' };
        const facade = { getAll: vi.fn(() => of([DEFINITION, other])), update: vi.fn(() => pending) };
        await TestBed.configureTestingModule({
            imports: [AdminAchievementsComponent],
            providers: [provideRouter([]), provideTranslateTesting(), { provide: AdminAchievementsFacade, useValue: facade }],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminAchievementsComponent);
        fixture.detectChanges();
        const component = fixture.componentInstance;
        component['edit'](DEFINITION);
        component['save']();
        fixture.detectChanges();
        component['edit'](other);
        component['createNew']();
        expect(component['editingId']()).toBe(DEFINITION.id);
        expect(component['formModel']().key).toBe(DEFINITION.key);
        expect(component['form'].titleEn().disabled()).toBe(true);
        const host = fixture.nativeElement as HTMLElement;
        expect([...host.querySelectorAll<HTMLButtonElement>('button.definition-card')].every(button => button.disabled)).toBe(true);
        expect(host.querySelector<HTMLInputElement>('form input')?.disabled).toBe(true);
        if (outcome === 'error') {
            pending.error(new Error('offline'));
            expect(component['editingId']()).toBe(DEFINITION.id);
            expect(component['error']()).toBe('ADMIN_ACHIEVEMENTS.ERRORS.SAVE');
        } else {
            pending.next(DEFINITION);
            expect(component['editingId']()).toBeNull();
            expect(component['formModel']().key).toBe('');
        }
        fixture.detectChanges();
        expect(component['isSaving']()).toBe(false);
        expect(component['form'].titleEn().disabled()).toBe(false);
    });
});

function writeAchievementField(root: HTMLElement, key: string, value: string): void {
    const label = Array.from(root.querySelectorAll<HTMLLabelElement>('form label')).find(
        item => item.firstElementChild?.textContent.trim() === `ADMIN_ACHIEVEMENTS.FIELDS.${key}`,
    );
    const field = label?.control;
    if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement)) {
        throw new Error(`Missing achievement control ${key}`);
    }
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
}

it('creates a new bilingual achievement from the rendered draft and displays the saved definition', async () => {
    const created: AdminAchievementDefinition = {
        ...DEFINITION,
        key: 'first_week',
        category: 'habits',
        metric: 'LongestStreak',
        threshold: 1,
        sortOrder: 0,
        icon: 'emoji_events',
        titleRu: 'Начало',
        titleEn: 'Getting started',
        version: 1,
    };
    const service = {
        getAll: vi
            .fn<AdminAchievementsService['getAll']>()
            .mockReturnValueOnce(of([]))
            .mockReturnValueOnce(of([created])),
        create: vi.fn<AdminAchievementsService['create']>().mockReturnValue(of(created)),
    };
    await TestBed.configureTestingModule({
        imports: [AdminAchievementsComponent],
        providers: [provideRouter([]), provideTranslateTesting(), { provide: AdminAchievementsService, useValue: service }],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminAchievementsComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const save = root.querySelector<HTMLButtonElement>('form button[type="submit"]');
    if (save === null) {
        throw new Error('Missing achievement save button');
    }
    expect(root.querySelector('.definition-list')?.textContent).toContain('ADMIN_ACHIEVEMENTS.EMPTY');
    expect(save.disabled).toBe(true);
    writeAchievementField(root, 'KEY', created.key);
    writeAchievementField(root, 'TITLE_RU', created.titleRu);
    writeAchievementField(root, 'TITLE_EN', created.titleEn);
    writeAchievementField(root, 'DESCRIPTION_RU', created.descriptionRu);
    writeAchievementField(root, 'DESCRIPTION_EN', created.descriptionEn);
    fixture.detectChanges();

    save.click();
    fixture.detectChanges();
    const { id: _id, version: _version, ...definition } = created;
    expect(service.create).toHaveBeenCalledExactlyOnceWith(definition);
    expect(root.querySelector('.definition-card strong')?.textContent).toBe(created.titleEn);
    expect(root.querySelector<HTMLInputElement>('form input')?.value).toBe('');
    expect(save.disabled).toBe(true);
});

it('keeps the catalog filter when paging and updates the selected definition through its versioned API', async () => {
    const definitions = Array.from({ length: 21 }, (_, index) => ({
        ...DEFINITION,
        id: `definition-${index + 1}`,
        key: `meals_${index + 1}`,
        titleEn: `Achievement ${index + 1}`,
    }));
    const inactive = { ...DEFINITION, id: 'inactive-definition', titleEn: 'Inactive achievement', isActive: false };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'achievements', component: AdminAchievementsComponent }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create('/achievements?state=active');
    const refresh = (): void => {
        http.expectOne(request => request.method === 'GET' && request.url.endsWith('/admin/achievement-definitions')).flush([
            ...definitions,
            inactive,
        ]);
        harness.detectChanges();
    };
    refresh();
    const root = harness.routeNativeElement;
    const next = root?.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child');
    expect(next).not.toBeNull();
    next?.click();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/achievements?state=active&page=2');
    if (root === null) {
        throw new Error('Expected the achievements route');
    }
    expect(root.querySelectorAll('.definition-card')).toHaveLength(1);
    const selected = root.querySelector<HTMLButtonElement>('.definition-card');
    expect(selected?.textContent).toContain('Achievement 21');
    selected?.click();
    harness.detectChanges();
    writeAchievementField(root, 'TITLE_EN', 'Revised achievement');
    harness.detectChanges();
    root.querySelector<HTMLButtonElement>('form button[type="submit"]')?.click();

    const request = http.expectOne(item => item.method === 'PUT' && item.url.endsWith('/admin/achievement-definitions/definition-21'));
    expect(request.request.body).toMatchObject({ titleEn: 'Revised achievement', version: DEFINITION.version });
    expect(request.request.body).not.toHaveProperty('key');
    const previous = definitions.at(-1);
    if (previous === undefined) {
        throw new Error('Missing the achievement on the second page');
    }
    const updated = { ...previous, titleEn: 'Revised achievement', version: DEFINITION.version + 1 };
    definitions[definitions.length - 1] = updated;
    request.flush(updated);
    refresh();
    expect(root.querySelector('.definition-card')?.textContent).toContain(updated.titleEn);
    http.verify();
});

it('keeps the metric readable when a translation entry is an object rather than a label', async () => {
    TestBed.configureTestingModule({
        imports: [AdminAchievementsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            {
                provide: AdminAchievementsService,
                useValue: { getAll: vi.fn<AdminAchievementsService['getAll']>().mockReturnValue(of([])) },
            },
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { ADMIN_ACHIEVEMENTS: { METRICS: { LONGEST_STREAK: { label: 'Unexpected nested label' } } } });
    await firstValueFrom(translate.use('en'));
    const fixture = TestBed.createComponent(AdminAchievementsComponent);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('form fd-ui-select')?.textContent).toContain(
        'ADMIN_ACHIEVEMENTS.METRICS.LONGEST_STREAK',
    );
});
