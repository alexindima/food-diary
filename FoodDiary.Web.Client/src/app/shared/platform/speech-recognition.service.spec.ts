import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { SpeechRecognitionService } from './speech-recognition.service';

describe('SpeechRecognitionService', () => {
    let recognition: RecognitionMock;

    beforeEach(() => {
        recognition = createRecognitionMock();
        function RecognitionConstructor(): RecognitionMock {
            return recognition;
        }
        Object.defineProperty(window, 'SpeechRecognition', { configurable: true, value: RecognitionConstructor });
        TestBed.configureTestingModule({ providers: [SpeechRecognitionService] });
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
        service.start('en-US', vi.fn());

        service.stop();

        expect(recognition.stop).toHaveBeenCalledTimes(1);
        expect(service.isListening()).toBe(false);
    });

    it('resets listening state after recognition errors', () => {
        const service = TestBed.inject(SpeechRecognitionService);
        service.start('en-US', vi.fn());

        recognition.onerror();

        expect(service.isListening()).toBe(false);
    });
});

type RecognitionMock = {
    lang: string;
    interimResults: boolean;
    maxAlternatives: number;
    onresult: (event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void;
    onerror: () => void;
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
