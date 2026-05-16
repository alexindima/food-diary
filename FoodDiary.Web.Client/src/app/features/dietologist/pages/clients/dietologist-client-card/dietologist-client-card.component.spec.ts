import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { createClient } from '../dietologist-clients-lib/dietologist-clients.mapper.spec';
import type { ClientCardViewModel } from '../dietologist-clients-lib/dietologist-clients.types';
import { DietologistClientCardComponent } from './dietologist-client-card.component';

describe('DietologistClientCardComponent', () => {
    it('renders client card data and emits open event', () => {
        const fixture = createComponent();
        const open = vi.fn();
        fixture.componentInstance.clientOpen.subscribe(open);

        getHost(fixture).querySelector<HTMLElement>('fd-ui-card')?.click();

        expect(getHost(fixture).textContent).toContain('Alex Ivanov');
        expect(getHost(fixture).textContent).toContain('client@example.com');
        expect(open).toHaveBeenCalledWith(fixture.componentInstance.item().client);
    });
});

function createComponent(): ComponentFixture<DietologistClientCardComponent> {
    TestBed.configureTestingModule({
        imports: [DietologistClientCardComponent, TranslateModule.forRoot()],
    });

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
