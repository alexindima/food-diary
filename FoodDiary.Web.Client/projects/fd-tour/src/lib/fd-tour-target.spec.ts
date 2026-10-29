import { afterEach, describe, expect, it } from 'vitest';

import { findTourTarget } from './fd-tour-target';

describe('Tour target layout boxes', () => {
    let host: HTMLElement | undefined;

    afterEach(() => host?.remove());

    it('uses the rendered child of a display contents component', () => {
        host = document.createElement('div');
        host.innerHTML =
            '<div data-target style="display:contents"><div hidden>Hidden</div><div style="display:contents"><section>Results</section></div></div>';
        document.body.append(host);
        expect(findTourTarget(document, '[data-target]')).toBe(host.querySelector('section'));
    });

    it('omits an empty display contents component', () => {
        host = document.createElement('div');
        host.innerHTML = '<div data-target style="display:contents"><div hidden>Unavailable</div></div>';
        document.body.append(host);
        expect(findTourTarget(document, '[data-target]')).toBeNull();
    });
});
