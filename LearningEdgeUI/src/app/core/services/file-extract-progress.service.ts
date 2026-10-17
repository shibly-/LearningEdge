import { Injectable, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

export interface FileExtractProgress {
  readonly fileId: string;
  readonly status: string;
  readonly progress: number;
}

@Injectable({ providedIn: 'root' })
export class FileExtractProgressService {
  private readonly updates = signal<Readonly<Record<string, FileExtractProgress>>>({});
  private readonly watched = new Set<string>();

  private connection: HubConnection | null = null;
  private starting: Promise<void> | null = null;

  /** Latest Redis snapshot pushed by the file-extract hub, keyed by file id. */
  readonly progress = this.updates.asReadonly();

  /** Drop a previous snapshot so a new extract does not keep the last percentage. */
  clear(fileId: string): void {
    const key = fileId.toLowerCase();
    this.updates.update((current) => {
      if (!(key in current)) {
        return current;
      }
      const next = { ...current };
      delete next[key];
      return next;
    });
  }

  /** Watch exactly these files. Others on this page are dropped. */
  sync(fileIds: readonly string[]): void {
    if (environment.useMockApi) {
      return;
    }
    const next = new Set(fileIds.map((id) => id.toLowerCase()));
    for (const fileId of [...this.watched]) {
      if (!next.has(fileId)) {
        this.watched.delete(fileId);
        void this.invoke('Unwatch', fileId);
      }
    }
    for (const fileId of next) {
      if (!this.watched.has(fileId)) {
        this.watched.add(fileId);
        void this.invoke('Watch', fileId);
      }
    }
  }

  private async invoke(method: 'Watch' | 'Unwatch', fileId: string): Promise<void> {
    try {
      await this.ensure();
      if (this.connection?.state !== HubConnectionState.Connected) {
        return;
      }
      if (method === 'Unwatch' && !this.watched.has(fileId)) {
        await this.connection.invoke(method, fileId);
        return;
      }
      if (method === 'Watch' && this.watched.has(fileId)) {
        await this.connection.invoke(method, fileId);
      }
    } catch (error) {
      console.warn(`File extract progress ${method} failed for ${fileId}.`, error);
    }
  }

  private ensure(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return Promise.resolve();
    }
    this.starting ??= this.connect().finally(() => {
      this.starting = null;
    });
    return this.starting;
  }

  private async connect(): Promise<void> {
    if (this.connection === null) {
      const connection = new HubConnectionBuilder()
        .withUrl(`${environment.apiBaseUrl}/hubs/file-extract`, { withCredentials: true })
        .withAutomaticReconnect()
        .build();

      connection.on('progress', (update: FileExtractProgress) => this.remember(update));
      connection.onreconnected(() => {
        for (const fileId of this.watched) {
          void connection.invoke('Watch', fileId);
        }
      });
      this.connection = connection;
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  private remember(update: FileExtractProgress): void {
    const fileId = update.fileId.toLowerCase();
    const next: FileExtractProgress = {
      fileId,
      status: update.status,
      progress: update.progress,
    };
    this.updates.update((current) => {
      const previous = current[fileId];
      if (previous?.status === next.status && previous.progress === next.progress) {
        return current;
      }
      return { ...current, [fileId]: next };
    });
  }
}
