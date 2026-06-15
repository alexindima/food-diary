import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { ErrorStateComponent } from './error-state';

describe('ErrorStateComponent', () => {
    it('renders default text keys and emits retry', () => {
        const fixture = createComponent();
        const retry = vi.fn();
        fixture.componentInstance['retry'].subscribe(retry);

        getHost(fixture).querySelector<HTMLButtonElement>('button')?.click();

        expect(getHost(fixture).textContent).toContain('ERRORS.LOAD_FAILED_TITLE');
        expect(getHost(fixture).textContent).toContain('ERRORS.LOAD_FAILED_MESSAGE');
        expect(retry).toHaveBeenCalledOnce();
    });

    it('hides retry button when disabled', () => {
        const fixture = createComponent({ showRetry: false });

        expect(getHost(fixture).querySelector('button')).toBeNull();
    });
});

function createComponent(inputs: { showRetry?: boolean } = {}): ComponentFixture<ErrorStateComponent> {
    TestBed.configureTestingModule({
        imports: [ErrorStateComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(ErrorStateComponent);
    if (inputs.showRetry !== undefined) {
        fixture.componentRef.setInput('showRetry', inputs.showRetry);
    }
    fixture.detectChanges();
    return fixture;
}

function getHost(fixture: ComponentFixture<ErrorStateComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}
