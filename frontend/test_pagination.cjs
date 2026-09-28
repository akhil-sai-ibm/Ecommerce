const axios = require('axios');

async function test() {
    try {
        console.log('\n--- 1. Login to get JWT ---');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'newadmin@admin.com',
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        
        console.log('\n--- 2. Test GET /api/products/search via Gateway ---');
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=10', {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('GET /api/products/search SUCCESS:', searchRes.status);
        console.log('Headers returned to Node (ignores CORS):');
        console.log('x-total-count:', searchRes.headers['x-total-count']);
        console.log('Number of products in data:', searchRes.data.length);
        
        console.log('\n--- 3. Test GET /api/inventory via Gateway ---');
        const invRes = await axios.get('http://localhost:5000/api/inventory', {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('GET /api/inventory SUCCESS:', invRes.status);
        console.log('Number of inventory items:', invRes.data.length);
    } catch(e) {
        console.error('Error:', e.response?.data || e.message);
    }
}
test();
