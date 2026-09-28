const axios = require('axios');

async function testAddToCartAdmin() {
    try {
        console.log('Logging in as admin...');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'admin@example.com',
            password: 'Admin123!'
        });
        const token = loginRes.data.token;
        
        console.log('Fetching products...');
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=1', {
            headers: { Authorization: `Bearer ${token}` }
        });
        const productId = searchRes.data[0].id;
        
        console.log('Adding to cart...');
        const cartRes = await axios.post('http://localhost:5000/api/cart/items', {
            productId: productId,
            quantity: 1
        }, {
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Origin': 'http://localhost:5174',
                'Content-Type': 'application/json'
            }
        });
        console.log('Status:', cartRes.status);
    } catch(e) {
        console.error('Error Status:', e.response?.status);
        console.error('Error Data:', e.response?.data);
        console.error('Error Message:', e.message);
    }
}
testAddToCartAdmin();
