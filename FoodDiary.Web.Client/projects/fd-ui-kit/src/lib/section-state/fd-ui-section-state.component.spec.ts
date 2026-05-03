import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
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
    async function createComponent(): Promise<ComponentFixture<TestHostComponent>> {
        await TestBed.configureTestingModule({
            imports: [TestHostComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(TestHostComponent);
        fixture.detectChanges();
        return fixture;
    }

    let fixture: ComponentFixture<TestHostComponent>;
    let host: TestHostComponent;

    it('renders projected content in content state', async () => {
        fixture = await createComponent();
        host = fixture.componentInstance;

        expect(fixture.nativeElement.querySelector('.projected-content')?.textContent).toContain('Ready');
    });

    it('renders loader in loading state', async () => {
        fixture = await createComponent();
        host = fixture.componentInstance;
        host.state.set('loading');
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('fd-ui-loader')).not.toBeNull();
    });

    it('renders empty state in empty state', async () => {
        fixture = await createComponent();
        host = fixture.componentInstance;
        host.state.set('empty');
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('fd-ui-empty-state')).not.toBeNull();
    });

    it('emits retry in error state', async () => {
        fixture = await createComponent();
        host = fixture.componentInstance;
        host.state.set('error');
        fixture.detectChanges();

        fixture.debugElement.query(By.css('button')).nativeElement.click();
        expect(host.retryCount()).toBe(1);
    });

    it('applies compact host class', async () => {
        fixture = await createComponent();
        host = fixture.componentInstance;
        host.appearance = 'compact';
        host.state.set('empty');
        fixture.detectChanges();

        const sectionState = fixture.nativeElement.querySelector('fd-ui-section-state');
        expect(sectionState.classList.contains('fd-ui-section-state--compact')).toBe(true);
    });
});
