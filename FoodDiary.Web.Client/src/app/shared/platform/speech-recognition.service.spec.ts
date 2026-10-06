import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { SpeechRecognitionService } from './speech-recognition.service';

let recognition: RecognitionMock;
const originalConstructors = new Map([
    ['SpeechRecognition', Object.getOwnPropertyDescriptor(window, 'SpeechRecognition')],
    ['webkitSpeechRecognition', Object.getOwnPropertyDescriptor(window, 'webkitSpeechRecognition')],
]);

beforeEach(() => {
    recognition = createRecognitionMock();
    installRecognitionConstructor();
    Object.defineProperty(window, 'webkitSpeechRecognition', { configurable: true, value: undefined });
    TestBed.configureTestingModule({ providers: [SpeechRecognitionService] });
});

afterEach(() => {
    for (const [property, descriptor] of originalConstructors) {
        if (descriptor === undefined) {
            Reflect.deleteProperty(window, property);
        } else {
            Object.defineProperty(window, property, descriptor);
        }
    }
});

describe('SpeechRecognitionService', () => {
    it('ignores a duplicate start without interrupting the active recording', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        service.start('en-US', vi.fn());

        expect(service.start('en-US', vi.fn(), onError)).toBe(false);
        expect(recognition.start).toHaveBeenCalledOnce();
        expect(service.isListening()).toBe(true);
        expect(onError).not.toHaveBeenCalled();
    });

    it('configures recognition and emits the final transcript', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onTranscript = vi.fn();

        expect(service.start('ru-RU', onTranscript)).toBe(true);
        recognition.onresult({ results: [[{ transcript: 'яблоко' }]] });

        expect(recognition.lang).toBe('ru-RU');
        expect(recognition.start).toHaveBeenCalledTimes(1);
        expect(onTranscript).toHaveBeenCalledWith('яблоко');
        expect(service.isListening()).toBe(true);
    });

    it('stops the active recognition and resets listening state', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        service.start('en-US', vi.fn(), onError);

        service.stop();

        expect(recognition.stop).toHaveBeenCalledTimes(1);
        expect(service.isListening()).toBe(false);
        recognition.onerror({ error: 'network' });
        expect(onError).not.toHaveBeenCalled();
    });

    it('can receive the final transcript after an intentional stop', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onTranscript = vi.fn();
        const onError = vi.fn();
        service.start('en-US', onTranscript, onError);

        service.stop();
        recognition.onresult({ results: [[{ transcript: 'apple' }]] });
        recognition.onend();

        expect(service.isListening()).toBe(false);
        expect(onTranscript).toHaveBeenCalledExactlyOnceWith('apple');
        expect(onError).not.toHaveBeenCalled();
    });
});

describe('Speech recognition failures', () => {
    it.each([
        ['audio-capture', 'microphone-unavailable'],
        ['not-allowed', 'permission-denied'],
        ['service-not-allowed', 'permission-denied'],
        ['network', 'network'],
        ['no-speech', 'failed'],
        ['language-not-supported', 'failed'],
        ['private-provider-detail', 'failed'],
    ])('reports %s as a safe %s failure and resets listening', (error, failure) => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        service.start('en-US', vi.fn(), onError);

        recognition.onerror({ error });
        recognition.onend();

        expect(service.isListening()).toBe(false);
        expect(onError).toHaveBeenCalledExactlyOnceWith(failure);
    });

    it('treats an aborted recording as cancellation without an error', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        service.start('en-US', vi.fn(), onError);
        recognition.onerror({ error: 'aborted' });

        expect(service.isListening()).toBe(false);
        expect(onError).not.toHaveBeenCalled();
    });

    it('preserves a retry when the failed recording emits late events', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        const onTranscript = vi.fn();
        service.start('en-US', onTranscript, onError);
        const failedRecognition = recognition;
        failedRecognition.onerror({ error: 'network' });
        recognition = createRecognitionMock();
        expect(service.start('en-US', onTranscript, onError)).toBe(true);

        failedRecognition.onend();
        failedRecognition.onerror({ error: 'audio-capture' });
        failedRecognition.onresult({ results: [[{ transcript: 'stale' }]] });
        expect(service.isListening()).toBe(true);
        recognition.onresult({ results: [[{ transcript: 'fresh' }]] });
        expect(onTranscript).toHaveBeenCalledExactlyOnceWith('fresh');
        expect(onError).toHaveBeenCalledExactlyOnceWith('network');
    });
});

describe('Speech recognition startup recovery', () => {
    it.each([
        [new DOMException('Private browser detail', 'NotAllowedError'), 'permission-denied'],
        [new DOMException('Private browser detail', 'NotFoundError'), 'microphone-unavailable'],
        [new Error('Private provider detail'), 'failed'],
    ])('recovers a synchronous start exception with safe failure %s', (error, failure) => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        recognition.start.mockImplementationOnce(() => {
            throw error;
        });

        expect(service.start('en-US', vi.fn(), onError)).toBe(false);
        expect(service.isListening()).toBe(false);
        expect(onError).toHaveBeenCalledExactlyOnceWith(failure);
        expect(service.start('en-US', vi.fn(), onError)).toBe(true);
    });

    it('recovers a recognition constructor failure', () => {
        function FailingRecognitionConstructor(): never {
            throw new Error('Private constructor detail');
        }
        Object.defineProperty(window, 'SpeechRecognition', {
            configurable: true,
            value: FailingRecognitionConstructor,
        });
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();

        expect(service.start('en-US', vi.fn(), onError)).toBe(false);
        expect(service.isListening()).toBe(false);
        expect(onError).toHaveBeenCalledExactlyOnceWith('failed');
    });

    it('keeps stop exceptions silent and releases listening state', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();
        service.start('en-US', vi.fn(), onError);
        recognition.stop.mockImplementationOnce(() => {
            throw new Error('Private stop detail');
        });

        service.stop();

        expect(service.isListening()).toBe(false);
        expect(onError).not.toHaveBeenCalled();
    });
});

describe('Speech recognition availability', () => {
    it('reports an unavailable API without throwing', () => {
        Object.defineProperty(window, 'SpeechRecognition', { configurable: true, value: undefined });
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();

        expect(service.isSupported).toBe(false);
        expect(service.start('en-US', vi.fn(), onError)).toBe(false);
        expect(onError).toHaveBeenCalledExactlyOnceWith('unsupported');
        expect(service.isListening()).toBe(false);
    });

    it('preserves the webkit constructor fallback', () => {
        Object.defineProperty(window, 'SpeechRecognition', { configurable: true, value: undefined });
        installRecognitionConstructor('webkitSpeechRecognition');
        const service = TestBed.inject(SpeechRecognitionService);

        expect(service.isSupported).toBe(true);
        expect(service.start('en-US', vi.fn())).toBe(true);
        expect(recognition.start).toHaveBeenCalledOnce();
    });

    it('does not start browser recognition during server rendering', () => {
        TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        const service = TestBed.inject(SpeechRecognitionService);
        const onError = vi.fn();

        expect(service.isSupported).toBe(false);
        expect(service.start('en-US', vi.fn(), onError)).toBe(false);
        expect(recognition.start).not.toHaveBeenCalled();
        expect(onError).toHaveBeenCalledExactlyOnceWith('unsupported');
    });
});

describe('Speech recognition ownership', () => {
    it('keeps a recording running when another input requests cleanup', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        const owner = {};
        const otherOwner = {};
        service.start('en-US', vi.fn(), vi.fn(), owner);

        service.stop(otherOwner);

        expect(service.isListening()).toBe(true);
        expect(service.isOwnedBy(owner)).toBe(true);
        expect(recognition.stop).not.toHaveBeenCalled();
        service.stop(owner);
        expect(recognition.stop).toHaveBeenCalledOnce();
        expect(service.isListening()).toBe(false);
    });
});

type RecognitionMock = {
    lang: string;
    interimResults: boolean;
    maxAlternatives: number;
    onresult: (event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void;
    onerror: (event: { error: string }) => void;
    onend: () => void;
    start: ReturnType<typeof vi.fn>;
    stop: ReturnType<typeof vi.fn>;
};

function createRecognitionMock(): RecognitionMock {
    return {
        lang: '',
        interimResults: true,
        maxAlternatives: 0,
        onresult: vi.fn(),
        onerror: vi.fn(),
        onend: vi.fn(),
        start: vi.fn(),
        stop: vi.fn(),
    };
}

function installRecognitionConstructor(property = 'SpeechRecognition'): void {
    function RecognitionConstructor(): RecognitionMock {
        return recognition;
    }
    Object.defineProperty(window, property, { configurable: true, value: RecognitionConstructor });
}
