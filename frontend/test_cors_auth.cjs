const axios = require('axios');

async function testAddToCartCorsAndAuth() {
    try {
        const randomEmail = `test${Date.now()}@gmail.com`;
        await axios.post('http://localhost:5000/api/auth/register', {
            email: randomEmail,
            password: 'Password123!'
        });
        
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: randomEmail,
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=1', {
            headers: { Authorization: `Bearer ${token}` }
        });
        const productId = searchRes.data[0].id;
        
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
        console.log('Headers:', cartRes.headers);
    } catch(e) {
        console.error('Error Status:', e.response?.status);
        console.error('Error Data:', e.response?.data);
        console.error('Error Message:', e.message);
    }
}
testAddToCartCorsAndAuth();
