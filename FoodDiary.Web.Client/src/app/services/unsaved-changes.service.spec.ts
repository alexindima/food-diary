import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { UnsavedChangesService, UnsavedChangesHandler } from './unsaved-changes.service';

describe('UnsavedChangesService', () => {
    let service: UnsavedChangesService;

    const createMockHandler = (): UnsavedChangesHandler => ({
        hasChanges: vi.fn().mockReturnValue(true),
        save: vi.fn(),
        discard: vi.fn(),
    });

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [UnsavedChangesService],
        });

        service = TestBed.inject(UnsavedChangesService);
    });

    it('should register handler', () => {
        const handler = createMockHandler();
        service.register(handler);
        expect(service.getHandler()).toBe(handler);
    });

    it('should return handler after registration', () => {
        const handler = createMockHandler();
        service.register(handler);
        const result = service.getHandler();
        expect(result).toBe(handler);
        expect(result!.hasChanges).toBeDefined();
    });

    it('should clear handler when called without argument', () => {
        const handler = createMockHandler();
        service.register(handler);
        service.clear();
        expect(service.getHandler()).toBeNull();
    });

    it('should clear only matching handler', () => {
        const handler1 = createMockHandler();
        const handler2 = createMockHandler();

        service.register(handler1);
        service.clear(handler2);
        expect(service.getHandler()).toBe(handler1);

        service.clear(handler1);
        expect(service.getHandler()).toBeNull();
    });

    it('should return null when no handler registered', () => {
        expect(service.getHandler()).toBeNull();
    });
});
