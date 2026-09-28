const axios = require('axios');

async function testAddToCartCustomer() {
    try {
        const randomEmail = `test${Date.now()}@gmail.com`;
        console.log('\n--- 1. Register customer ---', randomEmail);
        await axios.post('http://localhost:5000/api/auth/register', {
            email: randomEmail,
            password: 'Password123!'
        });
        
        console.log('\n--- 2. Login as customer ---');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: randomEmail,
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        
        console.log('\n--- 3. Fetch products to get a valid ProductId ---');
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=1', {
            headers: { Authorization: `Bearer ${token}` }
        });
        const productId = searchRes.data[0].id;
        console.log('Got productId:', productId);
        
        console.log('\n--- 4. Test POST /api/cart/items ---');
        const cartRes = await axios.post('http://localhost:5000/api/cart/items', {
            productId: productId,
            quantity: 1
        }, {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Add to cart SUCCESS:', cartRes.status);
    } catch(e) {
        console.error('Error Status:', e.response?.status);
        console.error('Error Data:', e.response?.data);
        console.error('Error Message:', e.message);
    }
}
testAddToCartCustomer();
