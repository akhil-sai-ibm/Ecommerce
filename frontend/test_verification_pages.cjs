const axios = require('axios');

async function test() {
    try {
        console.log('\n--- 1. Login to get JWT ---');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'newadmin@admin.com',
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        
        console.log('\n--- 2. Fetch Page 1 ---');
        const p1 = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=10', {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('x-total-count:', p1.headers['x-total-count']);
        console.log('Page 1 items count:', p1.data.length);
        
        console.log('\n--- 3. Fetch Page 2 ---');
        const p2 = await axios.get('http://localhost:5000/api/products/search?pageNumber=2&pageSize=10', {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Page 2 items count:', p2.data.length);
        
        console.log('\n--- 4. Fetch Page 4 (should have 1 item) ---');
        const p4 = await axios.get('http://localhost:5000/api/products/search?pageNumber=4&pageSize=10', {
            headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Page 4 items count:', p4.data.length);
        
    } catch(e) {
        console.error('Error:', e.response?.data || e.message);
    }
}
test();
