import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service, signal } from '@angular/core';

@Service()
export class SpeechRecognitionService {
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly browserWindow = inject(DOCUMENT).defaultView as SpeechRecognitionWindow | null;
    private recognition: SpeechRecognitionLike | null = null;

    public readonly isListening = signal(false);
    public readonly isSupported =
        this.isBrowser &&
        this.browserWindow !== null &&
        (this.browserWindow.SpeechRecognition !== undefined || this.browserWindow.webkitSpeechRecognition !== undefined);

    public start(locale: string, onTranscript: (transcript: string) => void): boolean {
        const Recognition =
            this.isBrowser && this.browserWindow !== null
                ? (this.browserWindow.SpeechRecognition ?? this.browserWindow.webkitSpeechRecognition)
                : undefined;
        if (Recognition === undefined || this.isListening()) {
            return false;
        }

        const recognition = new Recognition();
        recognition.lang = locale;
        recognition.interimResults = false;
        recognition.maxAlternatives = 1;
        recognition.onresult = (event: SpeechRecognitionResultEventLike): void => {
            const transcript = event.results[0][0].transcript;
            if (transcript.length > 0) {
                onTranscript(transcript);
            }
        };
        recognition.onerror = (): void => {
            this.finish();
        };
        recognition.onend = (): void => {
            this.finish();
        };
        this.recognition = recognition;
        this.isListening.set(true);
        recognition.start();
        return true;
    }

    public stop(): void {
        this.recognition?.stop();
        this.finish();
    }

    private finish(): void {
        this.recognition = null;
        this.isListening.set(false);
    }
}

type SpeechRecognitionAlternativeLike = {
    transcript: string;
};

type SpeechRecognitionResultEventLike = {
    results: ArrayLike<ArrayLike<SpeechRecognitionAlternativeLike>>;
};

type SpeechRecognitionLike = {
    lang: string;
    interimResults: boolean;
    maxAlternatives: number;
    onresult: (event: SpeechRecognitionResultEventLike) => void;
    onerror: () => void;
    onend: () => void;
    start: () => void;
    stop: () => void;
};

type SpeechRecognitionConstructorLike = new () => SpeechRecognitionLike;

type SpeechRecognitionWindow = Window & {
    SpeechRecognition?: SpeechRecognitionConstructorLike;
    webkitSpeechRecognition?: SpeechRecognitionConstructorLike;
};
