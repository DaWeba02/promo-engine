import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 10,
  duration: '30s'
};

const payload = JSON.stringify({
  customerId: 'loadtest-customer',
  currency: 'EUR',
  couponCode: 'LOAD20',
  strategy: 'CustomerBestPrice',
  minimumMarginRate: 0.1,
  items: Array.from({ length: 20 }, (_, index) => ({
    sku: `SKU-${index + 1}`,
    quantity: 2,
    unitPrice: 10 + index,
    unitCost: 5 + index / 2,
    stockLevel: 100 + index
  }))
});

export default function () {
  const response = http.post('http://localhost:8080/quotes', payload, {
    headers: { 'Content-Type': 'application/json' }
  });

  check(response, {
    'status is 200': (r) => r.status === 200
  });

  sleep(1);
}
