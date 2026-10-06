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

async function runAdminAudit() {
    console.log("Logging in as admin...");
    const loginRes = await fetchJson(`${GATEWAY_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: 'admin@example.com', password: 'Password123!' })
    });
    
    if (loginRes.status !== 200) {
        console.error("Failed to login as admin", loginRes.data);
        return;
    }
    
    console.log("Admin login succeeded:", loginRes.data);
    const adminAuthHeaders = { 
        'Authorization': `Bearer ${loginRes.data.token}`, 
        'Content-Type': 'application/json' 
    };

    console.log("Fetching admin profile...");
    const profileRes = await fetchJson(`${GATEWAY_URL}/users/profile`, {
        headers: adminAuthHeaders
    });
    console.log(`Profile status: ${profileRes.status}`, profileRes.data);
    
    console.log("Testing Admin-only endpoint (Get Low Stock)...");
    const lowStockRes = await fetchJson(`${GATEWAY_URL}/inventory/low`, {
        headers: adminAuthHeaders
    });
    console.log(`Low stock status: ${lowStockRes.status}`);

    console.log("Testing Admin-only endpoint (Admin Users list)...");
    const usersRes = await fetchJson(`${GATEWAY_URL}/users`, {
        headers: adminAuthHeaders
    });
    console.log(`Users list status: ${usersRes.status}`);
}

runAdminAudit();
