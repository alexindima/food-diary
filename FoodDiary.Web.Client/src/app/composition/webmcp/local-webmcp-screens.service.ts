import { Service } from '@angular/core';

import { type DevelopmentScreen, DevelopmentToolError } from '../../shared/development/development-tools';

/** Stack preserves the underlying page when a published dialog is opened and closed. */
@Service()
export class LocalWebmcpScreensService {
    private screens: DevelopmentScreen[] = [];
    private previous = '';
    private revision = 0;

    public attach(screen: DevelopmentScreen): () => void {
        this.screens.push(screen);
        return () => {
            this.screens = this.screens.filter(candidate => candidate !== screen);
            this.previous = '';
        };
    }

    public current(): DevelopmentScreen | undefined {
        return this.screens.at(-1);
    }

    public isDirty(): boolean {
        return this.screens.some(screen => screen.isDirty?.() === true);
    }

    public prepareNavigation(hasDialog = false): void {
        if (this.isDirty()) {
            throw new DevelopmentToolError('Unsaved draft exists.');
        }
        if (hasDialog && this.current()?.beforeNavigate === undefined) {
            throw new DevelopmentToolError('Close the current dialog in the interface before navigation.');
        }
        for (const screen of [...this.screens].reverse()) {
            screen.beforeNavigate?.();
        }
    }

    public read(): unknown {
        const screen = this.current();
        if (screen === undefined) {
            return null;
        }
        const data = screen.read();
        const serialized = JSON.stringify({ kind: screen.kind, data });
        if (serialized !== this.previous) {
            this.revision++;
            this.previous = serialized;
        }
        return {
            kind: screen.kind,
            revision: this.revision,
            data,
            dirty: screen.isDirty?.() ?? false,
            draftSchema: screen.draftSchema ?? null,
            actions: {
                fillDraft: screen.fillDraft !== undefined,
                clearDraft: screen.clearDraft !== undefined,
                setFiltersAsync: screen.setFiltersAsync !== undefined || screen.setFilters !== undefined,
                openEntityAsync: screen.openEntityAsync !== undefined,
                openDraft: screen.openDraft !== undefined,
            },
        };
    }

    public clear(expectedRevision: number): void {
        this.read();
        const screen = this.current();
        if (expectedRevision !== this.revision || screen?.clearDraft === undefined) {
            throw new DevelopmentToolError('Draft changed or cannot be cleared. Read the screen and confirm its current revision.');
        }
        screen.clearDraft();
    }

    public requireRevision(expectedRevision: number): void {
        this.read();
        if (!Number.isInteger(expectedRevision) || expectedRevision !== this.revision) {
            throw new DevelopmentToolError('Screen changed. Read its current revision before preparing the draft.');
        }
    }
}
