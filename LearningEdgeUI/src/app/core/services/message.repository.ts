import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, switchMap, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import type { Message, SendMessageCommand } from '../models/message';
import { fullName } from '../models/user';
import { MockDb } from './mock/mock-db';
import { UserRepository } from './user.repository';
import { MOCK_LATENCY_MS, notImplementedUpstream } from './repository-support';

// MOCK: no backend endpoint for messaging (spec 3.7).
export abstract class MessageRepository {
  abstract listForUser(organizationId: string, userId: string): Observable<readonly Message[]>;
  abstract listSentBy(organizationId: string, userId: string): Observable<readonly Message[]>;
  abstract send(
    command: SendMessageCommand,
    senderId: string,
    senderName: string,
  ): Observable<string>;
  abstract markRead(id: string): Observable<void>;
}

@Injectable()
export class HttpMessageRepository extends MessageRepository {
  override listForUser(_organizationId: string, _userId: string): Observable<readonly Message[]> {
    return throwError(() => notImplementedUpstream('Loading messages'));
  }

  override listSentBy(_organizationId: string, _userId: string): Observable<readonly Message[]> {
    return throwError(() => notImplementedUpstream('Loading sent messages'));
  }

  override send(
    _command: SendMessageCommand,
    _senderId: string,
    _senderName: string,
  ): Observable<string> {
    return throwError(() => notImplementedUpstream('Sending a message'));
  }

  override markRead(_id: string): Observable<void> {
    return throwError(() => notImplementedUpstream('Marking a message read'));
  }
}

@Injectable()
export class MockMessageRepository extends MessageRepository {
  private readonly db = inject(MockDb);
  private readonly users = inject(UserRepository);

  override listForUser(organizationId: string, userId: string): Observable<readonly Message[]> {
    const items = this.db.messages.filter(
      (m) => m.organizationId === organizationId && m.recipientId === userId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override listSentBy(organizationId: string, userId: string): Observable<readonly Message[]> {
    const items = this.db.messages.filter(
      (m) => m.organizationId === organizationId && m.senderId === userId,
    );
    return of(items).pipe(delay(MOCK_LATENCY_MS));
  }

  override send(
    command: SendMessageCommand,
    senderId: string,
    senderName: string,
  ): Observable<string> {
    const subject = command.subject.trim();
    const body = command.body.trim();
    if (subject.length === 0) {
      return throwError(() => new ApiFailure('validation', 'Subject is required.'));
    }
    if (body.length === 0) {
      return throwError(() => new ApiFailure('validation', 'Message body is required.'));
    }

    // Read through UserRepository so recipients from the API resolve too.
    return this.users.listByOrganization(command.organizationId).pipe(
      switchMap((users) => {
        const recipient = users.find((user) => user.id === command.recipientId);
        if (recipient === undefined) {
          return throwError(() => new ApiFailure('not-found', 'That recipient no longer exists.'));
        }

        const id = this.db.nextId('msg');
        this.db.messages = [
          ...this.db.messages,
          {
            id,
            organizationId: command.organizationId,
            senderId,
            senderName,
            recipientId: recipient.id,
            recipientName: fullName(recipient),
            subject,
            body,
            sentAt: new Date().toISOString(),
            read: false,
          },
        ];
        return of(id).pipe(delay(MOCK_LATENCY_MS));
      }),
    );
  }

  override markRead(id: string): Observable<void> {
    this.db.messages = this.db.messages.map((m) => (m.id === id ? { ...m, read: true } : m));
    return of(undefined);
  }
}
