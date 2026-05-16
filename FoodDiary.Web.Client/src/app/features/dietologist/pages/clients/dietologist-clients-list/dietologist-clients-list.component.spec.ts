import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { createClient } from '../dietologist-clients-lib/dietologist-clients.test-data';
import type { ClientCardViewModel } from '../dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientsListComponent } from './dietologist-clients-list.component';

describe('DietologistClientsListComponent', () => {
    it('renders loading state', () => {
        const fixture = createComponent({ loading: true, items: [] });

        expect(getHost(fixture).textContent).toContain('DIETOLOGIST.CLIENTS.LOADING');
    });

    it('renders empty state', () => {
        const fixture = createComponent({ loading: false, items: [] });

        expect(getHost(fixture).textContent).toContain('DIETOLOGIST.CLIENTS.EMPTY');
    });

    it('renders client cards and forwards open event', () => {
        const fixture = createComponent({ loading: false, items: [createViewModel()] });
        const open = vi.fn();
        fixture.componentInstance.clientOpen.subscribe(open);

        getHost(fixture).querySelector<HTMLElement>('fd-ui-card')?.click();

        expect(getHost(fixture).textContent).toContain('Alex Ivanov');
        expect(open).toHaveBeenCalledWith(fixture.componentInstance.items()[0].client);
    });
});

function createComponent(input: { loading: boolean; items: ClientCardViewModel[] }): ComponentFixture<DietologistClientsListComponent> {
    TestBed.configureTestingModule({
        imports: [DietologistClientsListComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(DietologistClientsListComponent);
    fixture.componentRef.setInput('loading', input.loading);
    fixture.componentRef.setInput('items', input.items);
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

function getHost(fixture: ComponentFixture<DietologistClientsListComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
