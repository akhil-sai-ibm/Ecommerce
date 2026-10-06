const fs = require('fs');

const GATEWAY_URL = 'http://localhost:5000/api';

async function fetchJson(url, options = {}) {
    const res = await fetch(url, options);
    const text = await res.text();
    let data;
    try {
        data = text ? JSON.parse(text) : null;
    } catch (e) {
        data = text;
    }
    return { status: res.status, data: data, headers: res.headers };
}

async function runCheckoutAudit() {
    console.log("Logging in as customer...");
    const loginRes = await fetchJson(`${GATEWAY_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: 'brandnew@example.com', password: 'Password123!' })
    });
    
    if (loginRes.status !== 200) {
        console.error("Failed to login", loginRes.data);
        return;
    }
    
    const customerAuthHeaders = { 
        'Authorization': `Bearer ${loginRes.data.token}`, 
        'Content-Type': 'application/json' 
    };

    // 1. Get products to add to cart
    console.log("Fetching products...");
    const prodRes = await fetchJson(`${GATEWAY_URL}/products`);
    const product = prodRes.data.find(p => p.stockQuantity > 0);
    
    if (!product) {
        console.error("No product with stock found.");
        return;
    }
    
    console.log(`Adding product ${product.id} to cart...`);
    await fetchJson(`${GATEWAY_URL}/cart/items`, { 
        method: 'POST',
        headers: customerAuthHeaders,
        body: JSON.stringify({ productId: product.id, quantity: 1 })
    });

    console.log("Testing Checkout Idempotency...");
    const idempotencyKey = Date.now().toString();
    const checkoutDto = {
        shippingAddressLine1: '123 Test St',
        shippingAddressLine2: '',
        shippingCity: 'Testville',
        shippingState: 'TS',
        shippingPostalCode: '12345',
        shippingCountry: 'Testland',
        shippingPhoneNumber: '555-1234'
    };
    
    // First checkout
    console.log("First checkout request...");
    const checkout1 = await fetchJson(`${GATEWAY_URL}/orders/checkout`, {
        method: 'POST',
        headers: { ...customerAuthHeaders, 'Idempotency-Key': idempotencyKey },
        body: JSON.stringify(checkoutDto)
    });
    
    console.log(`First checkout status: ${checkout1.status}`);
    console.log(`First checkout response: ${JSON.stringify(checkout1.data).substring(0, 100)}...`);

    // Second checkout with same idempotency key
    console.log("Second checkout request (idempotent)...");
    const checkout2 = await fetchJson(`${GATEWAY_URL}/orders/checkout`, {
        method: 'POST',
        headers: { ...customerAuthHeaders, 'Idempotency-Key': idempotencyKey },
        body: JSON.stringify(checkoutDto)
    });
    
    console.log(`Second checkout status: ${checkout2.status}`);
    console.log(`Second checkout response: ${JSON.stringify(checkout2.data)}`);
    
    // Checkout with empty cart
    console.log("Third checkout request (empty cart)...");
    const checkout3 = await fetchJson(`${GATEWAY_URL}/orders/checkout`, {
        method: 'POST',
        headers: { ...customerAuthHeaders, 'Idempotency-Key': (Date.now() + 1).toString() },
        body: JSON.stringify(checkoutDto)
    });
    console.log(`Empty cart checkout status: ${checkout3.status}`);
    console.log(`Empty cart checkout response: ${JSON.stringify(checkout3.data)}`);

}

runCheckoutAudit();
