import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { Confirmation } from './confirmation';

describe('Order confirmation', () => {
  it('loads a saved order from the URL without transient navigation state', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: '42' })) } },
      ],
    });
    const component = TestBed.createComponent(Confirmation).componentInstance;
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('/api/orders/42')
      .flush({
        orderId: 42,
        customer: 'Nguyen',
        timestamp: '2026-10-04T04:00:00Z',
        items: [{ inventoryItemId: 1, quantity: 2 }],
      });
    http.expectOne('/api/inventory').flush([{ itemId: 1, name: 'Milk', quantity: 1 }]);
    expect(component.order()?.orderId).toBe(42);
    expect(component.names()[1]).toBe('Milk');
    http.verify();
  });
});
