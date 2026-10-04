import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Api, Order, errorMessage } from './api';
@Component({
  selector: 'app-confirmation',
  imports: [DatePipe, RouterLink],
  template: ` <main class="confirmation-shell">
    <section class="confirmation-card">
      @if (error()) {
        <p class="validation-summary" role="alert">{{ error() }}</p>
      } @else if (order(); as saved) {
        <p class="eyebrow">Order Confirmed</p>
        <h1>Order #{{ saved.orderId }} is recorded.</h1>
        <p class="hero-copy">
          Customer <strong>{{ saved.customer }}</strong> was captured on
          {{ saved.timestamp | date: 'dd MMM yyyy HH:mm' }}.
        </p>
        <div class="confirmation-list">
          @for (item of saved.items; track $index) {
            <div class="confirmation-row">
              <span>{{ names()[item.inventoryItemId] || 'Item #' + item.inventoryItemId }}</span
              ><strong>x {{ item.quantity }}</strong>
            </div>
          }
        </div>
      } @else {
        <p role="status">Loading order�</p>
      }
      <div class="confirmation-actions">
        <a class="submit-button" routerLink="/orders">Create another order</a>
      </div>
    </section>
  </main>`,
})
export class Confirmation {
  readonly order = signal<Order | null>(null);
  readonly names = signal<Record<number, string>>({});
  readonly error = signal('');
  constructor() {
    const api = inject(Api);
    inject(ActivatedRoute)
      .paramMap.pipe(
        switchMap((params) => {
          const id = Number(params.get('id'));
          if (!Number.isSafeInteger(id) || id <= 0) throw new Error('Invalid order ID');
          this.order.set(null);
          return forkJoin({ order: api.order(id), inventory: api.inventory() });
        }),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe({
        next: ({ order, inventory }) => {
          this.order.set(order);
          this.names.set(Object.fromEntries(inventory.map((item) => [item.itemId, item.name])));
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
}
