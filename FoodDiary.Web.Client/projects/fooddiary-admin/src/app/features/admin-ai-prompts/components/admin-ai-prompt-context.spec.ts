import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAiPromptContextComponent } from './admin-ai-prompt-context';

describe('AdminAiPromptContextComponent', () => {
    it('switches input guidance and renders the response format supplied by the server', async () => {
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptContextComponent],
            providers: provideTranslateTesting(),
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminAiPromptContextComponent);
        fixture.componentRef.setInput('scenarioKey', 'vision');
        fixture.componentRef.setInput('responseFormatJson', '{"name":"food_vision","strict":true}');
        fixture.detectChanges();
        const context = fixture.nativeElement as HTMLElement;
        expect(context.textContent).toContain('ADMIN_PROMPTS.INPUTS_vision');
        expect(context.querySelector('pre')?.textContent).toContain('food_vision');
        expect(context.querySelector('details')?.open).toBe(false);

        fixture.componentRef.setInput('scenarioKey', 'nutrition');
        fixture.componentRef.setInput('responseFormatJson', '{"name":"food_nutrition","strict":true}');
        fixture.detectChanges();
        expect(context.textContent).toContain('ADMIN_PROMPTS.INPUTS_nutrition');
        expect(context.querySelector('pre')?.textContent).toContain('food_nutrition');
    });
});
