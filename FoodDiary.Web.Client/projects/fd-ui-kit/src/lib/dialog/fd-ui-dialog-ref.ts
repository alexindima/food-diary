import { type DialogCloseOptions, type DialogRef } from '@angular/cdk/dialog';
import { type OverlayRef } from '@angular/cdk/overlay';
import { type ComponentRef } from '@angular/core';
import { type Observable } from 'rxjs';

export class FdUiDialogRef<T = unknown, R = unknown> {
    public constructor(private readonly dialogRef: DialogRef<R, T>) {}

    public get id(): string {
        return this.dialogRef.id;
    }

    public get componentInstance(): T | null {
        return this.dialogRef.componentInstance;
    }

    public get componentRef(): ComponentRef<T> | null {
        return this.dialogRef.componentRef;
    }

    public get overlayRef(): OverlayRef {
        return this.dialogRef.overlayRef;
    }

    public get disableClose(): boolean | undefined {
        return this.dialogRef.disableClose;
    }

    public set disableClose(value: boolean | undefined) {
        this.dialogRef.disableClose = value;
    }

    public get backdropClick(): Observable<MouseEvent> {
        return this.dialogRef.backdropClick;
    }

    public get keydownEvents(): Observable<KeyboardEvent> {
        return this.dialogRef.keydownEvents;
    }

    public get closed(): Observable<R | undefined> {
        return this.dialogRef.closed;
    }

    public afterClosed(): Observable<R | undefined> {
        return this.dialogRef.closed;
    }

    public close(result?: R, options?: DialogCloseOptions): void {
        this.dialogRef.close(result, options);
    }

    public updatePosition(): this {
        this.dialogRef.updatePosition();
        return this;
    }

    public updateSize(width?: string | number, height?: string | number): this {
        this.dialogRef.updateSize(width, height);
        return this;
    }

    public addPanelClass(classes: string | string[]): this {
        this.dialogRef.addPanelClass(classes);
        return this;
    }

    public removePanelClass(classes: string | string[]): this {
        this.dialogRef.removePanelClass(classes);
        return this;
    }
}
