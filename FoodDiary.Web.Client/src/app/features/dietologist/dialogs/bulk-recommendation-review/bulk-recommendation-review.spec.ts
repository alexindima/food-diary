import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { BulkRecommendationReviewComponent } from './bulk-recommendation-review';

describe('BulkRecommendationReviewComponent', () => {
    it('shows all recipients and multiline text as text, and provides cancel and confirm', () => {
        const close = vi.fn();
        TestBed.configureTestingModule({
            imports: [BulkRecommendationReviewComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: { close } },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        recipients: [
                            { id: entityId<'user'>('first'), label: 'Анна', email: 'first@example.test' },
                            { id: entityId<'user'>('second'), label: 'John', email: 'second@example.test' },
                        ],
                        text: 'First line\n<script>test</script>',
                    },
                },
            ],
        });
        const fixture = TestBed.createComponent(BulkRecommendationReviewComponent);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(Array.from(root.querySelectorAll('li')).map(item => item.textContent)).toEqual([
            expect.stringContaining('Анна'),
            expect.stringContaining('John'),
        ]);
        expect(root.textContent).toContain('first@example.test');
        expect(root.textContent).toContain('second@example.test');
        expect(root.querySelector('.bulk-review__text')?.textContent).toBe('First line\n<script>test</script>');
        expect(root.querySelector('script')).toBeNull();
        fixture.componentInstance['close'](false);
        expect(close).toHaveBeenLastCalledWith(false);
        fixture.componentInstance['close'](true);
        expect(close).toHaveBeenLastCalledWith(true);
    });
});
