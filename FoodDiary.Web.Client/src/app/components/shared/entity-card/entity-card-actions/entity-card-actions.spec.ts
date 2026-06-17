import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { EntityCardActionsComponent } from './entity-card-actions';

async function setupEntityCardActionsAsync(): Promise<ComponentFixture<EntityCardActionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardActionsComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardActionsComponent);
    fixture.componentRef.setInput('actionIcon', 'add');
    return fixture;
}

describe('EntityCardActionsComponent', () => {
    it('stops propagation and emits action', async () => {
        const fixture = await setupEntityCardActionsAsync();
        const component = fixture.componentInstance;
        const stopPropagationSpy = vi.fn();
        const event = { stopPropagation: stopPropagationSpy } as unknown as Event;
        const actionSpy = vi.fn();
        component['action'].subscribe(actionSpy);
        fixture.detectChanges();

        component['emitCardAction'](event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });
});
