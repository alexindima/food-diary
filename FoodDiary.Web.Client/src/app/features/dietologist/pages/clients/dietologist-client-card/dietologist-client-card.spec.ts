import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import { Gender } from '../../../../../shared/models/user.data';
import { getClientInitials, getClientTitle } from '../dietologist-clients-lib/dietologist-clients.mapper';
import { createClient } from '../dietologist-clients-lib/dietologist-clients.test-data';
import type { ClientCardViewModel } from '../dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientCardComponent } from './dietologist-client-card';

describe('DietologistClientCardComponent', () => {
    it('renders client card data and emits open event', () => {
        const fixture = createComponent();
        const open = vi.fn();
        fixture.componentInstance['clientOpen'].subscribe(open);

        getHost(fixture).querySelector<HTMLElement>('fd-ui-card')?.click();

        expect(getHost(fixture).textContent).toContain('Alex Ivanov');
        expect(getHost(fixture).textContent).toContain('client@example.com');
        expect(open).toHaveBeenCalledWith(fixture.componentInstance['item']().client);
    });

    it('opens the client card from the keyboard', () => {
        const fixture = createComponent();
        const open = vi.fn();
        fixture.componentInstance['clientOpen'].subscribe(open);
        const card = getHost(fixture).querySelector<HTMLElement>('fd-ui-card');

        card?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

        expect(card?.getAttribute('role')).toBe('button');
        expect(card?.getAttribute('tabindex')).toBe('0');
        expect(card?.getAttribute('aria-label')).toBe('Alex Ivanov');
        expect(open).toHaveBeenCalledWith(fixture.componentInstance['item']().client);
    });

    it('renders height in the dietologist preferred unit system', () => {
        const fixture = createComponent();
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        fixture.detectChanges();

        expect(getHost(fixture).textContent).toContain('5 ft 10.9 in');
    });

    it('updates height units when the language changes', () => {
        const fixture = createComponent();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('ru', { GENERAL: { UNITS: { CM: 'см', FT: 'фут', IN: 'дюйм' } } });
        translate.use('ru');
        fixture.detectChanges();

        expect(getHost(fixture).textContent).toContain('180 см');
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        fixture.detectChanges();
        expect(getHost(fixture).textContent).toContain('5 фут 10,9 дюйм');
    });
});

describe('Dietologist client card shared profile', () => {
    it.each([
        [Gender.Male, 'Male', 'Мужской'],
        [Gender.Female, 'Female', 'Женский'],
        [Gender.Other, 'Other', 'Другой'],
    ])('translates shared gender code %s in English and Russian', async (gender, english, russian) => {
        const fixture = createComponent();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { USER_MANAGE: { GENDER_OPTIONS: { [gender]: english } } }, true);
        translate.setTranslation('ru', { USER_MANAGE: { GENDER_OPTIONS: { [gender]: russian } } });
        fixture.componentRef.setInput('item', { ...createViewModel(), client: createClient({ gender }) });
        fixture.detectChanges();

        expect(getProfileChips(fixture)).toContain(english);

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(getProfileChips(fixture)).toContain(russian);
    });

    it('hides shared profile data when a refreshed client withdraws profile access', () => {
        const fixture = createComponent();
        expect(getProfileChips(fixture)).toEqual(['180 cm', 'Male', 'Moderate']);

        const shared = createClient();
        const client = createClient({
            firstName: null,
            lastName: null,
            gender: null,
            heightCm: null,
            activityLevel: null,
            permissions: { ...shared.permissions, shareProfile: false },
        });
        fixture.componentRef.setInput('item', {
            ...createViewModel(),
            client,
            title: getClientTitle(client),
            initials: getClientInitials(client),
        });
        fixture.detectChanges();

        const host = getHost(fixture);
        expect(host.querySelector('.dietologist-clients__meta')).toBeNull();
        expect(host.querySelector('fd-ui-card')?.getAttribute('aria-label')).toBe('client@example.com');
        expect(host.textContent).not.toContain('Alex Ivanov');
    });
});

function getProfileChips(fixture: ComponentFixture<DietologistClientCardComponent>): string[] {
    return Array.from(getHost(fixture).querySelectorAll('.dietologist-clients__chip'), chip => chip.textContent.trim());
}

function createComponent(): ComponentFixture<DietologistClientCardComponent> {
    TestBed.configureTestingModule({
        imports: [DietologistClientCardComponent],
        providers: [provideTranslateTesting()],
    });

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
        GENERAL: { UNITS: { CM: 'cm', FT: 'ft', IN: 'in' } },
        USER_MANAGE: { GENDER_OPTIONS: { M: 'Male' }, ACTIVITY_LEVEL_OPTIONS: { MODERATE: 'Moderate' } },
    });
    translate.use('en');

    const fixture = TestBed.createComponent(DietologistClientCardComponent);
    fixture.componentRef.setInput('item', createViewModel());
    fixture.detectChanges();

    return fixture;
}

function createViewModel(): ClientCardViewModel {
    return {
        client: createClient(),
        title: 'Alex Ivanov',
        initials: 'AI',
        connectedDateLabel: 'May 16, 2026',
    };
}

function getHost(fixture: ComponentFixture<DietologistClientCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
