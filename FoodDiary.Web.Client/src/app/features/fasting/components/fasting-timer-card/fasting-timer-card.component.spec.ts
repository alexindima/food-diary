import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { type FastingOccurrenceKind } from '../../models/fasting.data';
import { FastingTimerCardComponent } from './fasting-timer-card.component';

describe('FastingTimerCardComponent', () => {
    it('renders projected fasting controls in stacked layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('stacked');
        fixture.detectChanges();

        expect(getProjectedControl(fixture)).not.toBeNull();
    });

    it('renders projected fasting controls in summary layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('summary');
        fixture.detectChanges();

        expect(getProjectedControl(fixture)).not.toBeNull();
    });

    it('renders projected fasting controls in setup layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('setup');
        fixture.detectChanges();

        expect(getProjectedControl(fixture)).not.toBeNull();
    });

    it('renders projected fasting controls in page summary layout', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('pageSummary');
        fixture.detectChanges();

        expect(getProjectedControl(fixture)).not.toBeNull();
    });

    it.each(['summary', 'pageSummary'] as const)('does not render fasting stage details for eating phases in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        fixture.componentInstance.isActive.set(true);
        fixture.componentInstance.occurrenceKind.set('EatDay');
        fixture.componentInstance.stageTitleKey.set('FASTING.STAGES.INITIAL.TITLE');
        fixture.componentInstance.stageDescriptionKey.set('FASTING.STAGES.INITIAL.DESCRIPTION');
        fixture.componentInstance.stageIndex.set(1);
        fixture.detectChanges();

        expect(fixture.debugElement.query(By.css('.fasting-timer-card__stage-title'))).toBeNull();
        expect(fixture.debugElement.query(By.css('.fasting-timer-card__next-stage-label'))).toBeNull();
    });

    it.each(['summary', 'pageSummary'] as const)('renders protocol separator without mojibake in %s layout', async layout => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set(layout);
        fixture.componentInstance.isActive.set(true);
        fixture.componentInstance.detailLabel.set('16:8');
        fixture.detectChanges();

        const separator = fixture.nativeElement.querySelector('.fasting-timer-card__summary-protocol-separator') as HTMLElement;
        expect(separator.textContent.trim()).toBe('·');
        expect(fixture.nativeElement.textContent).not.toContain('Â');
    });

    it('clamps rendered progress percent to the valid ring range', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('summary');
        fixture.componentInstance.isActive.set(true);
        fixture.componentInstance.progressPercent.set(140);
        fixture.detectChanges();

        const percent = fixture.nativeElement.querySelector('.fasting-timer-card__percent') as HTMLElement;
        expect(percent.textContent.trim()).toBe('100%');
    });

    it('marks progress rings as decorative', async () => {
        const fixture = await createHostFixtureAsync();

        fixture.componentInstance.layout.set('summary');
        fixture.detectChanges();

        const ringSvg = fixture.nativeElement.querySelector('.fasting-timer-card__ring-svg') as SVGElement;
        expect(ringSvg.getAttribute('aria-hidden')).toBe('true');
        expect(ringSvg.getAttribute('focusable')).toBe('false');
    });
});

@Component({
    imports: [FastingTimerCardComponent],
    template: `
        <fd-fasting-timer-card
            [layout]="layout()"
            [isActive]="isActive()"
            [occurrenceKind]="occurrenceKind()"
            [detailLabel]="detailLabel()"
            [progressPercent]="progressPercent()"
            [stageTitleKey]="stageTitleKey()"
            [stageDescriptionKey]="stageDescriptionKey()"
            [stageIndex]="stageIndex()"
        >
            <button fastingControls type="button" class="projected-control">Start fasting</button>
        </fd-fasting-timer-card>
    `,
})
class FastingTimerCardHostComponent {
    public readonly layout = signal<'stacked' | 'summary' | 'setup' | 'pageSummary'>('stacked');
    public readonly isActive = signal(false);
    public readonly occurrenceKind = signal<FastingOccurrenceKind | null>(null);
    public readonly detailLabel = signal<string | null>(null);
    public readonly progressPercent = signal(0);
    public readonly stageTitleKey = signal<string | null>(null);
    public readonly stageDescriptionKey = signal<string | null>(null);
    public readonly stageIndex = signal<number | null>(null);
}

async function createHostFixtureAsync(): Promise<ComponentFixture<FastingTimerCardHostComponent>> {
    await TestBed.configureTestingModule({
        imports: [FastingTimerCardHostComponent, TranslateModule.forRoot()],
    }).compileComponents();

    return TestBed.createComponent(FastingTimerCardHostComponent);
}

function getProjectedControl(fixture: ComponentFixture<FastingTimerCardHostComponent>): HTMLElement | null {
    return fixture.nativeElement.querySelector('.projected-control') as HTMLElement | null;
}
