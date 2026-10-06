const axios = require('axios');

async function run() {
    try {
        // Get token
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'brandnew@example.com',
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        console.log("Token:", token.substring(0, 15) + "...");
        
        // POST to cart
        const cartRes = await axios.post('http://localhost:5000/api/cart/items', {
            productId: '83d29805-2ff2-4563-bfcb-5d1d5bbe2571',
            quantity: 1
        }, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        console.log("Cart Status:", cartRes.status);
        console.log("Cart Data:", cartRes.data);
    } catch (e) {
        console.log("Error status:", e.response?.status);
        console.log("Error data:", e.response?.data);
        console.log("Error message:", e.message);
    }
}
run();
