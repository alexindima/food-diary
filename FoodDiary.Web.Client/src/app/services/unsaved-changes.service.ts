import { Injectable } from '@angular/core';

export interface UnsavedChangesHandler {
    hasChanges: () => boolean;
    save: () => void | boolean | Promise<boolean>;
    discard: () => void;
}

@Injectable({ providedIn: 'root' })
export class UnsavedChangesService {
    private handler: UnsavedChangesHandler | null = null;

    public register(handler: UnsavedChangesHandler): void {
        this.handler = handler;
    }

    public clear(handler?: UnsavedChangesHandler): void {
        if (!handler || this.handler === handler) {
            this.handler = null;
        }
    }

    public getHandler(): UnsavedChangesHandler | null {
        return this.handler;
    }
}
