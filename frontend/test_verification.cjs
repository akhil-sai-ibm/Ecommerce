const axios = require('axios');
const FormData = require('form-data');
const fs = require('fs');

async function test() {
    try {
        console.log('\n--- 1. Test CORS Headers for X-Total-Count ---');
        const optionsRes = await axios.options('http://localhost:5000/api/products/search?pageNumber=1&pageSize=10', {
            headers: {
                'Origin': 'http://localhost:5174',
                'Access-Control-Request-Method': 'GET',
                'Access-Control-Request-Headers': 'Authorization'
            }
        });
        console.log('OPTIONS status:', optionsRes.status);
        console.log('Access-Control-Expose-Headers:', optionsRes.headers['access-control-expose-headers']);
        
        console.log('\n--- 2. Login to get JWT ---');
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'newadmin@admin.com',
            password: 'Password123!'
        });
        const token = loginRes.data.token;
        
        console.log('\n--- 3. Test Bulk Upload Still Works ---');
        const form = new FormData();
        form.append('file', fs.createReadStream('C:\\Users\\MAkhilSai\\Downloads\\ECommerce_Products_Bulk_Upload.xlsx'));
        const bulkRes = await axios.post('http://localhost:5000/api/products/bulk-upload', form, {
            headers: { 
                Authorization: `Bearer ${token}`,
                ...form.getHeaders()
            }
        });
        console.log('Bulk Upload SUCCESS:', bulkRes.status, bulkRes.data);
    } catch(e) {
        console.error('Error:', e.response?.data || e.message);
    }
}
test();
