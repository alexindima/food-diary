import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { type FdUiSectionState, FdUiSectionStateComponent } from './fd-ui-section-state.component';

@Component({
    standalone: true,
    imports: [FdUiSectionStateComponent],
    template: `
        <fd-ui-section-state
            [state]="state()"
            [emptyMessage]="emptyMessage"
            [errorTitle]="errorTitle"
            [errorMessage]="errorMessage"
            [retryLabel]="retryLabel"
            [appearance]="appearance"
            (retry)="retryCount.update(value => value + 1)"
        >
            <div class="projected-content">Ready</div>
        </fd-ui-section-state>
    `,
})
class TestHostComponent {
    public readonly state = signal<FdUiSectionState>('content');
    public readonly retryCount = signal(0);
    public readonly emptyMessage = 'Nothing to show yet.';
    public readonly errorTitle = 'Section failed';
    public readonly errorMessage = 'Please retry.';
    public readonly retryLabel = 'Retry';
    public appearance: 'default' | 'compact' = 'default';
}

describe('FdUiSectionStateComponent', () => {
    async function createComponentAsync(): Promise<ComponentFixture<TestHostComponent>> {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();
        return fixture;
    }

    let fixture: ComponentFixture<TestHostComponent>;
    let host: TestHostComponent;

    const nativeHost = (): HTMLElement => fixture.nativeElement as HTMLElement;
    const requireElement = (selector: string): HTMLElement => {
        const element = nativeHost().querySelector<HTMLElement>(selector);
        if (element === null) {
            throw new Error(`Expected element ${selector} to exist.`);
        }

        return element;
    };
    const requireButtonElement = (selector: string): HTMLButtonElement => {
        const element = nativeHost().querySelector<HTMLButtonElement>(selector);
        if (element === null) {
            throw new Error(`Expected button ${selector} to exist.`);
        }

        return element;
    };

    it('renders projected content in content state', async () => {
        fixture = await createComponentAsync();
        host = fixture.componentInstance;

        expect(requireElement('.projected-content').textContent).toContain('Ready');
    });

    it('renders loader in loading state', async () => {
        fixture = await createComponentAsync();
        host = fixture.componentInstance;
        host.state.set('loading');
        fixture.detectChanges();

        expect(nativeHost().querySelector('fd-ui-loader')).not.toBeNull();
    });

    it('renders empty state in empty state', async () => {
        fixture = await createComponentAsync();
        host = fixture.componentInstance;
        host.state.set('empty');
        fixture.detectChanges();

        expect(nativeHost().querySelector('fd-ui-empty-state')).not.toBeNull();
    });

    it('emits retry in error state', async () => {
        fixture = await createComponentAsync();
        host = fixture.componentInstance;
        host.state.set('error');
        fixture.detectChanges();

        requireButtonElement('button').click();
        expect(host.retryCount()).toBe(1);
    });

    it('applies compact host class', async () => {
        fixture = await createComponentAsync();
        host = fixture.componentInstance;
        host.appearance = 'compact';
        host.state.set('empty');
        fixture.detectChanges();

        const sectionState = requireElement('fd-ui-section-state');
        expect(sectionState.classList.contains('fd-ui-section-state--compact')).toBe(true);
    });
});
