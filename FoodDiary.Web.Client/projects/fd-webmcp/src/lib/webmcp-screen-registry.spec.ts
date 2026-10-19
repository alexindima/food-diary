import { describe, expect, it, vi } from 'vitest';

import { WebmcpScreenRegistry } from './webmcp-screen-registry';

describe('WebMCP published screen state', () => {
    it('restores an underlying page and ignores disposal of a different page', () => {
        const screens = new WebmcpScreenRegistry();
        const disposeOld = screens.attach({ kind: 'old', read: () => ({}) });
        screens.attach({ kind: 'page', read: () => ({ selected: 'owned' }) });
        const disposeDialog = screens.attach({ kind: 'dialog', read: () => ({}) });
        disposeDialog();
        disposeOld();
        expect(screens.read()).toMatchObject({ kind: 'page', data: { selected: 'owned' } });
    });

    it('rejects stale edits and stale discard after owner state changes', () => {
        const screens = new WebmcpScreenRegistry();
        let value = 1;
        const clearDraft = vi.fn();
        screens.attach({ kind: 'draft', read: () => ({ value }), clearDraft });
        const revision = screens.read()?.revision ?? 0;
        screens.requireRevision(revision);
        expect(screens.read()?.revision).toBe(revision);
        value = 2;
        expect(() => {
            screens.requireRevision(revision);
        }).toThrow('Screen changed');
        expect(() => {
            screens.clear(revision);
        }).toThrow('Draft changed');
        expect(clearDraft).not.toHaveBeenCalled();
        screens.clear(screens.read()?.revision ?? 0);
        expect(clearDraft).toHaveBeenCalledOnce();
    });

    it('refuses navigation when any published screen is dirty', () => {
        const screens = new WebmcpScreenRegistry();
        screens.attach({ kind: 'page', read: () => ({}), isDirty: () => true });
        const beforeNavigate = vi.fn();
        screens.attach({ kind: 'dialog', read: () => ({}), beforeNavigate });
        expect(() => {
            screens.prepareNavigation(true);
        }).toThrow('Unsaved draft');
        expect(beforeNavigate).not.toHaveBeenCalled();
    });

    it('requires explicit dialog navigation support and unwinds clean screens in reverse order', () => {
        const screens = new WebmcpScreenRegistry();
        const order: string[] = [];
        screens.attach({
            kind: 'page',
            read: () => ({}),
            beforeNavigate: () => {
                order.push('page');
            },
        });
        const disposeDialog = screens.attach({ kind: 'dialog', read: () => ({}) });
        expect(() => {
            screens.prepareNavigation(true);
        }).toThrow('Close the current dialog');
        disposeDialog();
        screens.attach({
            kind: 'dialog',
            read: () => ({}),
            beforeNavigate: () => {
                order.push('dialog');
            },
        });
        screens.prepareNavigation(true);
        expect(order).toEqual(['dialog', 'page']);
    });

    it('keeps registries isolated and refuses unsupported discard', () => {
        const screens = new WebmcpScreenRegistry();
        screens.attach({ kind: 'read-only', read: () => ({}) });
        expect(new WebmcpScreenRegistry().read()).toBeNull();
        expect(() => {
            screens.clear(screens.read()?.revision ?? 0);
        }).toThrow('cannot be cleared');
    });
});
