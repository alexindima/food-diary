import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { ErrorStateComponent } from './error-state.component';

describe('ErrorStateComponent', () => {
    it('renders default text keys and emits retry', () => {
        const fixture = createComponent();
        const retry = vi.fn();
        fixture.componentInstance.retry.subscribe(retry);

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
        imports: [ErrorStateComponent, TranslateModule.forRoot()],
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
