import { inject, Injector, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import type { FdTourDefinition, FdTourPlacement, FdTourStep } from 'fd-tour';

export type LocalizedTourConfig = {
    id: string;
    translationRoot: string;
    steps: readonly LocalizedTourStep[];
    version?: number;
};

export type LocalizedTourStep = {
    id: string;
    target: string;
    titleKey: string;
    descriptionKey: string;
    placement: FdTourPlacement;
};

@Service()
export class LocalizedTourDefinitionService {
    private readonly injector = inject(Injector);

    public build(config: LocalizedTourConfig): FdTourDefinition {
        return {
            id: config.id,
            version: config.version ?? 1,
            labels: {
                previous: this.translateTourText('PAGE_TOURS.LABELS.PREVIOUS'),
                next: this.translateTourText('PAGE_TOURS.LABELS.NEXT'),
                finish: this.translateTourText('PAGE_TOURS.LABELS.FINISH'),
                skip: this.translateTourText('PAGE_TOURS.LABELS.SKIP'),
                close: this.translateTourText('PAGE_TOURS.LABELS.CLOSE'),
            },
            steps: config.steps.map(step => this.buildTourStep(config.translationRoot, step)),
        };
    }

    private buildTourStep(translationRoot: string, step: LocalizedTourStep): FdTourStep {
        return {
            id: step.id,
            target: resolveTourTarget(step.target),
            title: this.translateTourText(`${translationRoot}.${step.titleKey}`),
            description: this.translateTourText(`${translationRoot}.${step.descriptionKey}`),
            placement: step.placement,
        };
    }

    private translateTourText(key: string): string {
        const translation = this.injector.get(TranslateService).instant(key);
        return typeof translation === 'string' ? translation : key;
    }
}

function resolveTourTarget(target: string): string {
    if (target.startsWith('[') || target.startsWith('.') || target.startsWith('#')) {
        return target;
    }

    return `[data-tour-id="${target}"]`;
}
