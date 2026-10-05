import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { CreateUserCommand, UserDto } from '../models/user';
import { UserRole } from '../models/user-role';
import { OrganizationContextService } from '../services/organization-context.service';
import { ToastService } from '../services/toast.service';
import { UserRepository } from '../services/user.repository';
import { AsyncCollectionStore, describeError } from './async-collection.store';

@Injectable({ providedIn: 'root' })
export class UserStore extends AsyncCollectionStore<UserDto> {
  private readonly repository = inject(UserRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);

  private readonly savingState = signal(false);
  readonly isSaving = this.savingState.asReadonly();

  readonly staff = computed(() => this.items().filter((user) => user.role === UserRole.Instructor));
  readonly trainees = computed(() => this.items().filter((user) => user.role === UserRole.Learner));
  readonly admins = computed(() => this.items().filter((user) => user.role === UserRole.OrgAdmin));

  loadForActiveOrganization(): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.reset();
      return;
    }
    this.load(this.repository.listByOrganization(organizationId));
  }

  create(command: CreateUserCommand, onCreated: (id: string) => void): void {
    if (this.savingState()) {
      return;
    }
    this.savingState.set(true);

    this.repository
      .create(command)
      .pipe(
        finalize(() => this.savingState.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (id) => {
          this.toast.success(`${command.firstName} ${command.lastName} added.`.trim());
          this.loadForActiveOrganization();
          onCreated(id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  findById(id: string): UserDto | undefined {
    return this.items().find((user) => user.id === id);
  }
}
