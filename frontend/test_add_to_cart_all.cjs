const axios = require('axios');

async function testAddToCartAll() {
    let token;
    try {
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
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=30', {
            headers: { Authorization: `Bearer ${token}` }
        });
        const products = searchRes.data;
        
        for (const product of products) {
            try {
                const cartRes = await axios.post('http://localhost:5000/api/cart/items', {
                    productId: product.id,
                    quantity: 1
                }, {
                    headers: { 
                        'Authorization': `Bearer ${token}`,
                        'Origin': 'http://localhost:5174',
                        'Content-Type': 'application/json'
                    }
                });
                console.log(`Product ${product.name}: Success (200)`);
            } catch(e) {
                console.error(`Product ${product.name}: Failed - ${e.response?.status} ${JSON.stringify(e.response?.data)}`);
            }
        }
    } catch(e) {
        console.error('Error Status:', e.response?.status);
        console.error('Error Data:', e.response?.data);
        console.error('Error Message:', e.message);
    }
}
testAddToCartAll();
