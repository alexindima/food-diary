import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import { AdminAiPromptWorkbenchComponent } from './admin-ai-prompt-workbench';

describe('AdminAiPromptWorkbenchComponent', () => {
    const api = { preview: vi.fn(), test: vi.fn(), uploadImage: vi.fn() };
    beforeEach(async () => {
        vi.clearAllMocks();
        api.preview.mockReturnValue(of({ text: 'Preview' }));
        api.test.mockReturnValue(of({ text: '{"items":[]}' }));
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptWorkbenchComponent],
            providers: [...provideTranslateTesting(), { provide: AdminAiPromptsFacade, useValue: api }],
        }).compileComponents();
    });
    function create(): ComponentFixture<AdminAiPromptWorkbenchComponent> {
        const fixture = TestBed.createComponent(AdminAiPromptWorkbenchComponent);
        fixture.componentRef.setInput('scenarioKey', 'text-parse');
        fixture.componentRef.setInput('locale', 'en');
        fixture.componentRef.setInput('promptText', 'Parse {{userText}}');
        fixture.detectChanges();
        fixture.componentInstance['text'].set('an apple');
        fixture.detectChanges();
        return fixture;
    }

    it('ignores preview responses for a draft edited while the request was in flight', async () => {
        const pending = new Subject<{ text: string }>();
        api.preview.mockReturnValue(pending);
        const fixture = create();
        const verified = vi.fn();
        fixture.componentInstance.verified.subscribe(verified);
        const request = fixture.componentInstance['inspectAsync'](false);
        fixture.componentRef.setInput('promptText', 'New {{userText}}');
        fixture.detectChanges();
        pending.next({ text: 'Old preview' });
        pending.complete();
        await request;
        expect(fixture.componentInstance['preview']()).toBe('');
        expect(verified).not.toHaveBeenCalled();
        expect(fixture.componentInstance['canTest']()).toBe(false);
    });

    it('requires a fresh preview after the sample changes and runs tests only on explicit action', async () => {
        const fixture = create();
        const component = fixture.componentInstance;
        await component['inspectAsync'](true);
        expect(api.test).not.toHaveBeenCalled();
        await component['inspectAsync'](false);
        expect(component['canTest']()).toBe(true);
        await component['inspectAsync'](true);
        expect(api.test).toHaveBeenCalledWith(
            expect.objectContaining({ text: 'an apple', promptText: 'Parse {{userText}}', locale: 'en' }),
        );
        component['text'].set('two eggs');
        fixture.detectChanges();
        expect(component['canTest']()).toBe(false);
        expect(component['result']()).toBe('');
    });
});
