import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Message, SendMessageCommand } from '../models/message';
import { AuthService } from '../auth/auth.service';
import { MessageRepository } from '../services/message.repository';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { AsyncCollectionStore, describeError } from './async-collection.store';

@Injectable({ providedIn: 'root' })
export class MessageStore extends AsyncCollectionStore<Message> {
  private readonly repository = inject(MessageRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  readonly unreadCount = computed(() => this.items().filter((message) => !message.read).length);
  readonly newestFirst = computed(() =>
    [...this.items()].sort((a, b) => b.sentAt.localeCompare(a.sentAt)),
  );

  loadInbox(): void {
    const organizationId = this.context.activeOrganizationId();
    const userId = this.auth.currentUser()?.id ?? null;
    if (organizationId === null || userId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listForUser(organizationId, userId));
  }

  loadSent(): void {
    const organizationId = this.context.activeOrganizationId();
    const userId = this.auth.currentUser()?.id ?? null;
    if (organizationId === null || userId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listSentBy(organizationId, userId));
  }

  send(command: SendMessageCommand, onSent: () => void): void {
    const user = this.auth.currentUser();
    if (user === null || this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .send(command, user.id, user.displayName)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Message sent.');
          onSent();
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  markRead(id: string): void {
    this.repository
      .markRead(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadInbox(),
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }
}
