import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { Orders } from './orders';

describe('Order capture', () => {
  let http: HttpTestingController;
  let component: Orders;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    component = TestBed.createComponent(Orders).componentInstance;
    http.expectOne('/api/inventory').flush([{ itemId: 1, name: 'Milk', quantity: 3 }]);
    http.expectOne('/api/tables').flush([
      { tableId: 1, name: 'Table 1', seatCount: 4, status: 0 },
      { tableId: 2, name: 'Table 2', seatCount: 4, status: 1 },
    ]);
    component.customer = '  Nguyen family  ';
  });
  afterEach(() => http.verify());

  it('excludes occupied tables and rejects quantities beyond available stock', () => {
    expect(component.tables().map((table) => table.tableId)).toEqual([1]);
    component.quantities[1] = 4;
    component.submit();
    expect(component.error()).toContain('available stock');
    http.expectNone('/api/orders');
  });

  it('rejects fractional quantities and an empty basket', () => {
    component.quantities[1] = 1.5;
    component.submit();
    expect(component.error()).toContain('whole numbers');
    component.quantities[1] = 0;
    component.submit();
    expect(component.error()).toContain('at least one');
    http.expectNone('/api/orders');
  });

  it('posts the API contract once and navigates to confirmation', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    component.quantities[1] = 2;
    component.submit();
    component.submit();
    const request = http.expectOne('/api/orders');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      customer: 'Nguyen family',
      tableId: 1,
      items: [{ inventoryItemId: 1, quantity: 2 }],
    });
    request.flush({
      orderId: 42,
      customer: 'Nguyen family',
      timestamp: '2026-10-04T04:00:00Z',
      items: [],
    });
    expect(navigate).toHaveBeenCalledWith(['/orders/Confirmation', 42]);
  });

  it('keeps entered data and refreshes stock after a rejected order', () => {
    component.quantities[1] = 2;
    component.submit();
    http
      .expectOne('/api/orders')
      .flush(
        { message: 'Insufficient stock for item Milk.' },
        { status: 400, statusText: 'Bad Request' },
      );
    http.expectOne('/api/inventory').flush([{ itemId: 1, name: 'Milk', quantity: 1 }]);
    http.expectOne('/api/tables').flush([{ tableId: 1, name: 'Table 1', seatCount: 4, status: 0 }]);
    expect(component.error()).toContain('Insufficient stock');
    expect(component.customer).toBe('  Nguyen family  ');
    expect(component.quantities[1]).toBe(2);
    expect(component.submitting()).toBe(false);
    expect(component.items()[0].quantity).toBe(1);
  });
});
