export async function waitForAsyncTasksAsync(): Promise<void> {
    await new Promise<void>(resolve => {
        resolve();
    });
}
