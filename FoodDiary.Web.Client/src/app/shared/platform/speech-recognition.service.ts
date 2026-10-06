import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service, signal } from '@angular/core';

export type SpeechRecognitionFailure = 'unsupported' | 'microphone-unavailable' | 'permission-denied' | 'network' | 'failed';

@Service()
export class SpeechRecognitionService {
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly browserWindow = inject(DOCUMENT).defaultView as SpeechRecognitionWindow | null;
    private recognition: SpeechRecognitionLike | null = null;
    private stopRequested = false;
    private owner: object | undefined;

    public readonly isListening = signal(false);
    public readonly isSupported =
        this.isBrowser &&
        this.browserWindow !== null &&
        (this.browserWindow.SpeechRecognition !== undefined || this.browserWindow.webkitSpeechRecognition !== undefined);

    public start(
        locale: string,
        onTranscript: (transcript: string) => void,
        onError?: (failure: SpeechRecognitionFailure) => void,
        owner?: object,
    ): boolean {
        const Recognition =
            this.isBrowser && this.browserWindow !== null
                ? (this.browserWindow.SpeechRecognition ?? this.browserWindow.webkitSpeechRecognition)
                : undefined;
        if (this.isListening()) {
            return false;
        }
        if (Recognition === undefined) {
            onError?.('unsupported');
            return false;
        }

        this.finish();
        try {
            const recognition = new Recognition();
            recognition.lang = locale;
            recognition.interimResults = false;
            recognition.maxAlternatives = 1;
            recognition.onresult = (event: SpeechRecognitionResultEventLike): void => {
                if (this.recognition !== recognition) {
                    return;
                }
                const transcript = event.results[0][0].transcript;
                if (transcript.length > 0) {
                    onTranscript(transcript);
                }
            };
            recognition.onerror = (event: SpeechRecognitionErrorEventLike): void => {
                if (this.recognition !== recognition) {
                    return;
                }
                const reportError = !this.stopRequested && event.error !== 'aborted';
                this.finish(recognition);
                if (reportError) {
                    onError?.(classifySpeechFailure(event.error));
                }
            };
            recognition.onend = (): void => {
                this.finish(recognition);
            };
            this.recognition = recognition;
            this.owner = owner;
            this.isListening.set(true);
            recognition.start();
            return true;
        } catch (error: unknown) {
            this.finish();
            onError?.(classifySpeechException(error));
            return false;
        }
    }

    public isOwnedBy(owner: object): boolean {
        return this.owner === owner;
    }

    public stop(owner?: object): void {
        if (owner !== undefined && !this.isOwnedBy(owner)) {
            return;
        }
        const recognition = this.recognition;
        if (recognition === null) {
            return;
        }
        this.stopRequested = true;
        this.isListening.set(false);
        try {
            recognition.stop();
        } catch {
            this.finish(recognition);
        }
    }

    private finish(recognition?: SpeechRecognitionLike): void {
        if (recognition !== undefined && this.recognition !== recognition) {
            return;
        }
        this.recognition = null;
        this.stopRequested = false;
        this.owner = undefined;
        this.isListening.set(false);
    }
}

type SpeechRecognitionAlternativeLike = {
    transcript: string;
};

type SpeechRecognitionResultEventLike = {
    results: ArrayLike<ArrayLike<SpeechRecognitionAlternativeLike>>;
};

type SpeechRecognitionErrorEventLike = {
    error: string;
};

type SpeechRecognitionLike = {
    lang: string;
    interimResults: boolean;
    maxAlternatives: number;
    onresult: (event: SpeechRecognitionResultEventLike) => void;
    onerror: (event: SpeechRecognitionErrorEventLike) => void;
    onend: () => void;
    start: () => void;
    stop: () => void;
};

type SpeechRecognitionConstructorLike = new () => SpeechRecognitionLike;

type SpeechRecognitionWindow = Window & {
    SpeechRecognition?: SpeechRecognitionConstructorLike;
    webkitSpeechRecognition?: SpeechRecognitionConstructorLike;
};

function classifySpeechFailure(error: string): SpeechRecognitionFailure {
    const failures = new Map<string, SpeechRecognitionFailure>([
        ['audio-capture', 'microphone-unavailable'],
        ['not-allowed', 'permission-denied'],
        ['service-not-allowed', 'permission-denied'],
        ['network', 'network'],
        ['NotAllowedError', 'permission-denied'],
        ['SecurityError', 'permission-denied'],
        ['NotFoundError', 'microphone-unavailable'],
        ['NotReadableError', 'microphone-unavailable'],
    ]);
    return failures.get(error) ?? 'failed';
}

function classifySpeechException(error: unknown): SpeechRecognitionFailure {
    const name = typeof error === 'object' && error !== null && 'name' in error && typeof error.name === 'string' ? error.name : '';
    return classifySpeechFailure(name);
}
