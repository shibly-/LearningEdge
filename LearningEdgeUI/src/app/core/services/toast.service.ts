import { Injectable, signal } from '@angular/core';
import { ApiFailure } from '../http/api-result';

export type ToastKind = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  readonly id: number;
  readonly kind: ToastKind;
  readonly message: string;
}

const AUTO_DISMISS_MS: Readonly<Record<ToastKind, number>> = {
  success: 4000,
  info: 5000,
  warning: 7000,
  error: 0, // Errors stay until dismissed.
};

/**
 * The only sanctioned way to surface an error. Rendered by ToastContainer with
 * aria-live="polite".
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly items = signal<readonly Toast[]>([]);
  private nextId = 1;

  readonly toasts = this.items.asReadonly();

  success(message: string): void {
    this.push('success', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  warning(message: string): void {
    this.push('warning', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  /** Normalizes anything thrown by ApiService into a single user-facing message. */
  fromFailure(error: unknown, fallback = 'Something went wrong. Please try again.'): void {
    if (error instanceof ApiFailure) {
      this.push(error.kind === 'rate-limited' ? 'warning' : 'error', error.message);
      return;
    }
    if (error instanceof Error && error.message.length > 0) {
      this.push('error', error.message);
      return;
    }
    this.push('error', fallback);
  }

  dismiss(id: number): void {
    this.items.update((current) => current.filter((toast) => toast.id !== id));
  }

  clear(): void {
    this.items.set([]);
  }

  private push(kind: ToastKind, message: string): void {
    const id = this.nextId++;
    this.items.update((current) => [...current, { id, kind, message }]);

    const ttl = AUTO_DISMISS_MS[kind];
    if (ttl > 0) {
      setTimeout(() => this.dismiss(id), ttl);
    }
  }
}
