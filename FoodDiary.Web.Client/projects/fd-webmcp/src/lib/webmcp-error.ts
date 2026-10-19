/** Only intentional, safe diagnostic messages may cross the browser tool boundary. */
export class WebmcpError extends Error {
    public override readonly name: string = 'WebmcpError';
}
