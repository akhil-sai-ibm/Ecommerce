const axios = require('axios');

async function testAddToCartCustomer() {
    let token;
    try {
        console.log('Registering customer...');
        await axios.post('http://localhost:5000/api/auth/register', {
            email: 'customer99@test.com',
            password: 'Password123!'
        }, { validateStatus: () => true });
        
        console.log('Logging in as customer...');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'customer99@test.com',
            password: 'Password123!'
        });
        token = loginRes.data.token;
    } catch(e) {
        console.error('Auth Error:', e.message);
        return;
    }

    try {
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
testAddToCartCustomer();
