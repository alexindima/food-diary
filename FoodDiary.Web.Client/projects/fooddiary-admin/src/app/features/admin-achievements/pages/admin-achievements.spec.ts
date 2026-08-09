import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAchievementsFacade } from '../lib/admin-achievements.facade';
import type { AdminAchievementDefinition } from '../models/admin-achievement.data';
import { AdminAchievementsComponent } from './admin-achievements';

const DEFINITION: AdminAchievementDefinition = {
    id: 'definition-id',
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
            providers: [provideTranslateTesting(), { provide: AdminAchievementsFacade, useValue: facade }],
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

        const keyInput = host.querySelector<HTMLInputElement>('input');
        expect(keyInput?.disabled).toBe(true);
        expect(host.textContent).toContain('ADMIN_ACHIEVEMENTS.EDIT');
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
});
