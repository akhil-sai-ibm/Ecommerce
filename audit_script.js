const fs = require('fs');

const GATEWAY_URL = 'http://localhost:5000/api';
const results = [];

function recordResult(area, endpoint, expected, actual, rootCause, status) {
    results.push({ area, endpoint, expected, actual, rootCause, status });
    console.log(`[${status}] ${area} - ${endpoint}`);
    if (status !== 'Confirmed') {
        console.log(`   Expected: ${expected}`);
        console.log(`   Actual: ${actual}`);
        if (rootCause) console.log(`   Root Cause: ${rootCause}`);
    }
}

async function fetchJson(url, options = {}) {
    const res = await fetch(url, options);
    const text = await res.text();
    let data;
    try {
        data = text ? JSON.parse(text) : null;
    } catch (e) {
        data = text;
    }
    if (!res.ok) {
        throw { response: { status: res.status, data: data } };
    }
    return { status: res.status, data: data };
}

async function runAudit() {
    let customerToken = '';
    let adminToken = '';
    let customerId = '';
    
    // PART 2: AUTHENTICATION
    console.log('--- PART 2: AUTHENTICATION ---');
    try {
        const uniqueEmail = `test_${Date.now()}@example.com`;
        const regRes = await fetchJson(`${GATEWAY_URL}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: uniqueEmail,
                password: 'Password123!',
                firstName: 'Audit',
                lastName: 'User'
            })
        });
        recordResult('Authentication', 'Registration (Valid)', '201 Created', `${regRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Authentication', 'Registration (Valid)', '201 Created', `${err.response?.status} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    try {
        const loginRes = await fetchJson(`${GATEWAY_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: 'brandnew@example.com',
                password: 'Password123!'
            })
        });
        customerToken = loginRes.data.token;
        customerId = loginRes.data.user.id;
        recordResult('Authentication', 'Login (Valid)', '200 OK + JWT', `${loginRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Authentication', 'Login (Valid)', '200 OK + JWT', `${err.response?.status} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    try {
        const loginResAdmin = await fetchJson(`${GATEWAY_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: 'admin@example.com',
                password: 'AdminPassword123!'
            })
        });
        adminToken = loginResAdmin.data.token;
        recordResult('Authentication', 'Login (Admin)', '200 OK + JWT', `${loginResAdmin.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Authentication', 'Login (Admin)', '200 OK + JWT', `${err.response?.status} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    const customerAuthHeaders = { 'Authorization': `Bearer ${customerToken}`, 'Content-Type': 'application/json' };
    const adminAuthHeaders = { 'Authorization': `Bearer ${adminToken}`, 'Content-Type': 'application/json' };

    // PART 3: PROFILE
    console.log('--- PART 3: PROFILE ---');
    try {
        const profRes = await fetchJson(`${GATEWAY_URL}/users/${customerId}/profile`, { headers: customerAuthHeaders });
        recordResult('Profile', 'Get Profile (By ID)', '200 OK', `${profRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Profile', 'Get Profile (By ID)', '200 OK', `${err.response?.status}`, '', 'Broken');
    }
    
    try {
        const profRes = await fetchJson(`${GATEWAY_URL}/users/profile`, { headers: customerAuthHeaders });
        recordResult('Profile', 'Get Profile (Me)', '200 OK', `${profRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Profile', 'Get Profile (Me)', '200 OK', `${err.response?.status}`, '', 'Broken');
    }
    
    // PART 4: PRODUCT CATALOG
    console.log('--- PART 4: PRODUCT CATALOG ---');
    let productId = '';
    try {
        const prodRes = await fetchJson(`${GATEWAY_URL}/products`);
        if (prodRes.data && prodRes.data.length > 0) {
            productId = prodRes.data[0].id;
        }
        recordResult('Product Catalog', 'Get Products', '200 OK with list', `${prodRes.status}, count: ${prodRes.data?.length}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Product Catalog', 'Get Products', '200 OK', `${err.response?.status}`, '', 'Broken');
    }

    // PART 6: CART
    console.log('--- PART 6: CART ---');
    try {
        if (!productId) throw new Error("No product ID to add to cart");
        const cartRes = await fetchJson(`${GATEWAY_URL}/cart/items`, { 
            method: 'POST',
            headers: customerAuthHeaders,
            body: JSON.stringify({ productId, quantity: 1 })
        });
        recordResult('Cart', 'Add Item', '200 OK', `${cartRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Cart', 'Add Item', '200 OK', `${err.response?.status || err.message} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    // PART 7: CHECKOUT
    console.log('--- PART 7: CHECKOUT ---');
    let idempotencyKey = Date.now().toString();
    try {
        const checkoutRes = await fetchJson(`${GATEWAY_URL}/orders/checkout`, {
            method: 'POST',
            headers: { ...customerAuthHeaders, 'Idempotency-Key': idempotencyKey },
            body: JSON.stringify({
                shippingAddress: '123 Test St',
                shippingCity: 'Testville',
                shippingPostalCode: '12345',
                shippingCountry: 'Testland'
            })
        });
        recordResult('Checkout', 'Place Order', '201 Created', `${checkoutRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Checkout', 'Place Order', '201 Created', `${err.response?.status} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    // PART 8: ORDERS
    console.log('--- PART 8: ORDERS ---');
    try {
        const ordersRes = await fetchJson(`${GATEWAY_URL}/orders/me`, { headers: customerAuthHeaders });
        recordResult('Orders', 'Get Orders', '200 OK', `${ordersRes.status}, count: ${ordersRes.data?.length}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Orders', 'Get Orders', '200 OK', `${err.response?.status}`, '', 'Broken');
    }

    // PART 11 & 12: ADMIN
    console.log('--- PART 11 & 12: ADMIN ---');
    try {
        const adminProdRes = await fetchJson(`${GATEWAY_URL}/products`, {
            method: 'POST',
            headers: adminAuthHeaders,
            body: JSON.stringify({
                name: 'Audit Test Product',
                description: 'Audit Test',
                price: 99.99,
                stockQuantity: 10,
                categoryId: 'c0a80121-7f9a-4b98-9e53-2d1844b6c310' 
            })
        });
        recordResult('Admin', 'Create Product', '201 Created', `${adminProdRes.status}`, '', 'Confirmed');
    } catch (err) {
        recordResult('Admin', 'Create Product', '201 Created', `${err.response?.status} - ${JSON.stringify(err.response?.data)}`, '', 'Broken');
    }

    fs.writeFileSync('C:\\Users\\MAkhilSai\\source\\repos\\ECommerce\\audit_results.json', JSON.stringify(results, null, 2));
    console.log("Audit script completed.");
}

runAudit();
