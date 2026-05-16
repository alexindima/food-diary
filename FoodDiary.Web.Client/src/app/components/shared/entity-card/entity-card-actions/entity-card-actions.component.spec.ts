import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { EntityCardActionsComponent } from './entity-card-actions.component';

const CALORIES = 100;

async function setupEntityCardActionsAsync(): Promise<ComponentFixture<EntityCardActionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [EntityCardActionsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EntityCardActionsComponent);
    fixture.componentRef.setInput('calories', CALORIES);
    fixture.componentRef.setInput('showAction', true);
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
        component.action.subscribe(actionSpy);
        fixture.detectChanges();

        component.handleAction(event);

        expect(stopPropagationSpy).toHaveBeenCalledOnce();
        expect(actionSpy).toHaveBeenCalledOnce();
    });
});
