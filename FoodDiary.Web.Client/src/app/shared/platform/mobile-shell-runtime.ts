type CapacitorBridge = {
    getPlatform?: () => string;
    isNativePlatform?: () => boolean;
};

const nativePlatforms = new Set(['android', 'ios']);

export function isMobileShellWindow(windowRef: Window): boolean {
    const capacitor = getCapacitorBridge(windowRef);

    if (capacitor !== null && isCapacitorNativeRuntime(capacitor)) {
        return true;
    }

    return isCapacitorLocalOrigin(windowRef) && isAndroidWebView(windowRef);
}

function isCapacitorNativeRuntime(capacitor: CapacitorBridge): boolean {
    const isNativePlatform = capacitor.isNativePlatform;
    const getPlatform = capacitor.getPlatform;

    if (typeof isNativePlatform === 'function' && isNativePlatform()) {
        return true;
    }

    const platform = typeof getPlatform === 'function' ? getPlatform() : null;

    return typeof platform === 'string' && nativePlatforms.has(platform);
}

function getCapacitorBridge(windowRef: Window): CapacitorBridge | null {
    const capacitor = 'Capacitor' in windowRef ? windowRef.Capacitor : null;

    if (!isRecord(capacitor)) {
        return null;
    }

    return capacitor;
}

function isRecord(value: unknown): value is CapacitorBridge {
    return typeof value === 'object' && value !== null;
}

function isCapacitorLocalOrigin(windowRef: Window): boolean {
    const { hostname, protocol } = windowRef.location;

    return hostname === 'localhost' && (protocol === 'https:' || protocol === 'capacitor:');
}

function isAndroidWebView(windowRef: Window): boolean {
    const userAgent = windowRef.navigator.userAgent;

    return userAgent.includes('Android') && userAgent.includes('; wv');
}
