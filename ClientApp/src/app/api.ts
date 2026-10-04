import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
export interface InventoryItem {
  itemId: number;
  name: string;
  quantity: number;
}
export interface DiningTable {
  tableId: number;
  name: string;
  seatCount: number;
  status: number;
}
export interface Order {
  orderId: number;
  customer: string;
  timestamp: string;
  items: { inventoryItemId: number; quantity: number }[];
}
export interface CreateOrder {
  customer: string;
  tableId: number;
  items: { inventoryItemId: number; quantity: number }[];
}
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  inventory() {
    return this.http.get<InventoryItem[]>('/api/inventory');
  }
  tables() {
    return this.http.get<DiningTable[]>('/api/tables');
  }
  order(id: number) {
    return this.http.get<Order>(`/api/orders/${id}`);
  }
  createOrder(request: CreateOrder) {
    return this.http.post<Order>('/api/orders', request);
  }
}
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Cannot connect to the server. Please try again.';
    if (error.status === 404) return 'Order not found.';
    return (
      error.error?.message ||
      Object.values(error.error?.errors ?? {})
        .flat()
        .join(' ') ||
      'Unable to complete the request. Please try again.'
    );
  }
  return 'Unable to complete the request. Please try again.';
}
