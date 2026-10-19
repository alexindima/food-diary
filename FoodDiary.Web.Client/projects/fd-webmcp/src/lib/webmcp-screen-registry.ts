import { WebmcpError } from './webmcp-error';
import type { WebmcpScreen, WebmcpScreenContext } from './webmcp-screen';

/** An application-owned stack preserves the underlying page when a dialog detaches. */
export class WebmcpScreenRegistry<TScreen extends WebmcpScreen<never> = WebmcpScreen> {
    private screens: TScreen[] = [];
    private previous = '';
    private revision = 0;

    public constructor(private readonly error: (message: string) => Error = message => new WebmcpError(message)) {}

    public attach(screen: TScreen): () => void {
        this.screens.push(screen);
        return () => {
            this.screens = this.screens.filter(candidate => candidate !== screen);
            this.previous = '';
        };
    }

    public current(): TScreen | undefined {
        return this.screens.at(-1);
    }

    public isDirty(): boolean {
        return this.screens.some(screen => screen.isDirty?.() === true);
    }

    public prepareNavigation(hasDialog = false): void {
        if (this.isDirty()) {
            throw this.error('Unsaved draft exists.');
        }
        if (hasDialog && this.current()?.beforeNavigate === undefined) {
            throw this.error('Close the current dialog in the interface before navigation.');
        }
        for (const screen of [...this.screens].reverse()) {
            screen.beforeNavigate?.();
        }
    }

    public read(): WebmcpScreenContext | null {
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
            throw this.error('Draft changed or cannot be cleared. Read the screen and confirm its current revision.');
        }
        screen.clearDraft();
    }

    public requireRevision(expectedRevision: number): void {
        this.read();
        if (!Number.isInteger(expectedRevision) || expectedRevision !== this.revision) {
            throw this.error('Screen changed. Read its current revision before preparing the draft.');
        }
    }
}
