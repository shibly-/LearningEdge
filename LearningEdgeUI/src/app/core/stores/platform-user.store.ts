import { Injectable, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { UpdateUserCommand, UserDto } from '../models/user';
import { ToastService } from '../services/toast.service';
import { UserRepository, type UserListFilter } from '../services/user.repository';
import { AsyncCollectionStore, describeError } from './async-collection.store';

/**
 * Cross-organization user list for SysAdmin screens. Not a root singleton:
 * each platform page provides its own instance, so their filters don't collide.
 */
@Injectable()
export class PlatformUserStore extends AsyncCollectionStore<UserDto> {
  private readonly repository = inject(UserRepository);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  private filter: UserListFilter = {};

  loadWith(filter: UserListFilter): void {
    this.filter = filter;
    this.load(this.repository.listAll(filter));
  }

  reload(): void {
    this.loadWith(this.filter);
  }

  update(id: string, command: UpdateUserCommand, onUpdated: (user: UserDto) => void): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .update(id, command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (user) => {
          this.toast.success(`${user.firstName} ${user.lastName} updated.`.trim());
          onUpdated(user);
          // The edit may move the user out of the current role or organization filter.
          this.reload();
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }
}
