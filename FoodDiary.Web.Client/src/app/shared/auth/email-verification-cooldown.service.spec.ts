import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { EmailVerificationCooldownService } from './email-verification-cooldown.service';

const RESEND_PAUSE_SECONDS = 60;
const SECONDS_AFTER_RELOAD = 40;

describe('EmailVerificationCooldownService', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-10-12T10:00:00Z'));
        TestBed.inject(BrowserStorageService).removeItem('session', 'email-verification-cooldown:cooldown-user');
    });

    afterEach(() => vi.useRealTimers());

    it('counts down from registration, survives service recreation and expires', () => {
        let cooldown = TestBed.inject(EmailVerificationCooldownService);
        cooldown.markSent('cooldown-user');
        expect(cooldown.remainingSeconds('cooldown-user')).toBe(RESEND_PAUSE_SECONDS);
        vi.setSystemTime(new Date('2026-10-12T10:00:20.001Z'));
        TestBed.resetTestingModule();
        cooldown = TestBed.inject(EmailVerificationCooldownService);
        expect(cooldown.remainingSeconds('cooldown-user')).toBe(SECONDS_AFTER_RELOAD);
        vi.setSystemTime(new Date('2026-10-12T10:01:00Z'));
        expect(cooldown.remainingSeconds('cooldown-user')).toBe(0);
    });

    it('keeps other accounts independent and ignores invalid stored deadlines', () => {
        const cooldown = TestBed.inject(EmailVerificationCooldownService);
        cooldown.markSent('cooldown-user');
        expect(cooldown.remainingSeconds('another-user')).toBe(0);
        expect(cooldown.remainingSeconds(null)).toBe(0);
        TestBed.inject(BrowserStorageService).setItem('session', 'email-verification-cooldown:another-user', 'invalid');
        expect(cooldown.remainingSeconds('another-user')).toBe(0);
    });
});
