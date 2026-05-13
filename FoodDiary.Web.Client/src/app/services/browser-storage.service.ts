import { Injectable } from '@angular/core';

type StorageScope = 'local' | 'session';

@Injectable({ providedIn: 'root' })
export class BrowserStorageService {
    public getItem(scope: StorageScope, key: string): string | null {
        return this.getStorage(scope)?.getItem(key) ?? null;
    }

    public setItem(scope: StorageScope, key: string, value: string): void {
        this.getStorage(scope)?.setItem(key, value);
    }

    public removeItem(scope: StorageScope, key: string): void {
        this.getStorage(scope)?.removeItem(key);
    }

    public getJson(scope: StorageScope, key: string): unknown {
        const value = this.getItem(scope, key);
        if (value === null || value.length === 0) {
            return null;
        }

        try {
            return JSON.parse(value) as unknown;
        } catch {
            this.removeItem(scope, key);
            return null;
        }
    }

    public setJson(scope: StorageScope, key: string, value: unknown): void {
        try {
            this.setItem(scope, key, JSON.stringify(value));
        } catch {
            // Storage persistence is optional and should fail silently.
        }
    }

    private getStorage(scope: StorageScope): Storage | null {
        if (typeof window === 'undefined') {
            return null;
        }

        return scope === 'local' ? window.localStorage : window.sessionStorage;
    }
}
