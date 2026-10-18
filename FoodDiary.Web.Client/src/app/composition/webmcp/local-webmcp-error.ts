/** Only explicitly safe pilot messages are returned through the browser tool bridge. */
export class LocalWebmcpError extends Error {
    public override readonly name = 'LocalWebmcpError';
}
