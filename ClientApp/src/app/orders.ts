import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Api, DiningTable, InventoryItem, errorMessage } from './api';
@Component({ selector: 'app-orders', imports: [FormsModule], templateUrl: './orders.html' })
export class Orders {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly items = signal<InventoryItem[]>([]);
  readonly tables = signal<DiningTable[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  customer = '';
  tableId: number | null = null;
  quantities: Record<number, number | null> = {};
  constructor() {
    this.load();
  }
  get availableItems() {
    return this.items().filter((item) => item.quantity > 0).length;
  }
  get totalStock() {
    return this.items().reduce((sum, item) => sum + item.quantity, 0);
  }
  load(clearError = true) {
    this.loading.set(true);
    if (clearError) this.error.set('');
    forkJoin({ items: this.api.inventory(), tables: this.api.tables() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ items, tables }) => {
          this.items.set(items);
          this.tables.set(tables.filter((table) => table.status === 0));
          if (!this.tables().some((table) => table.tableId === this.tableId))
            this.tableId = this.tables()[0]?.tableId ?? null;
          this.loading.set(false);
        },
        error: (error) => {
          this.error.set(errorMessage(error));
          this.loading.set(false);
        },
      });
  }
  submit() {
    if (this.submitting() || this.loading()) return;
    this.error.set('');
    const customer = this.customer.trim();
    if (!customer || customer.length > 100) {
      this.error.set('Enter a customer name of up to 100 characters.');
      return;
    }
    if (!this.tables().some((table) => table.tableId === this.tableId)) {
      this.error.set('Please select an available table.');
      return;
    }
    const items = this.items().map((item) => ({
      inventoryItemId: item.itemId,
      quantity: this.quantities[item.itemId] ?? 0,
    }));
    if (
      items.some(
        (line) =>
          !Number.isInteger(line.quantity) ||
          line.quantity < 0 ||
          line.quantity >
            this.items().find((item) => item.itemId === line.inventoryItemId)!.quantity,
      )
    ) {
      this.error.set('Quantities must be whole numbers within the available stock.');
      return;
    }
    const selected = items.filter((item) => item.quantity > 0);
    if (!selected.length) {
      this.error.set('Select at least one inventory item to place an order.');
      return;
    }
    this.submitting.set(true);
    this.api
      .createOrder({ customer, tableId: this.tableId!, items: selected })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          void this.router.navigate(['/orders/Confirmation', order.orderId]);
        },
        error: (error) => {
          this.submitting.set(false);
          this.error.set(errorMessage(error));
          this.load(false);
        },
      });
  }
}
