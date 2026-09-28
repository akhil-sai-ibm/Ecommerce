const axios = require('axios');
const FormData = require('form-data');
const fs = require('fs');
const { execSync } = require('child_process');

async function test() {
    try {
        console.log('--- 1. Register new admin user ---');
        try {
            await axios.post('http://localhost:5000/api/auth/register', {
                email: 'newadmin@admin.com',
                password: 'Password123!',
                firstName: 'Admin',
                lastName: 'User',
                phoneNumber: '1234567890'
            });
            console.log('Registered successfully.');
        } catch (e) {
            console.log('Registration failed (might already exist):', e.response?.data);
        }

        console.log('--- 2. Update DB Role to Admin ---');
        execSync(`sqlcmd -S "(localdb)\\mssqllocaldb" -d "UserServiceDb" -Q "UPDATE Users SET RoleId = '22222222-2222-2222-2222-222222222222' WHERE Email = 'newadmin@admin.com';"`);
        
        console.log('\n--- 3. Login to get JWT ---');
        let token;
        const loginRes = await axios.post('http://localhost:5000/api/auth/login', {
            email: 'newadmin@admin.com',
            password: 'Password123!'
        });
        token = loginRes.data.token;
        console.log('JWT received.');
        
        console.log('\n--- 4. Test POST /api/products via Gateway ---');
        try {
            const productRes = await axios.post('http://localhost:5000/api/products', { name: "Test Prod " + Date.now(), price: 10, sku: "TEST-SKU-" + Date.now(), category: "Electronics", description: "Test", quantity: 10, reorderLevel: 5 }, {
                headers: { Authorization: `Bearer ${token}` }
            });
            console.log('POST /api/products SUCCESS:', productRes.status);
        } catch(e) {
            console.log('POST /api/products FAILED with status:', e.response?.status);
            console.log(e.response?.data);
        }
        
        console.log('\n--- 5. Test POST /api/products/bulk-upload via Gateway ---');
        try {
            const form = new FormData();
            form.append('file', fs.createReadStream('C:\\Users\\MAkhilSai\\Downloads\\ECommerce_Products_Bulk_Upload.xlsx'));
            
            const bulkRes = await axios.post('http://localhost:5000/api/products/bulk-upload', form, {
                headers: { 
                    Authorization: `Bearer ${token}`,
                    ...form.getHeaders()
                }
            });
            console.log('POST /api/products/bulk-upload SUCCESS:', bulkRes.status);
            console.log('Result:', bulkRes.data);
        } catch(e) {
            console.log('POST /api/products/bulk-upload FAILED with status:', e.response?.status);
            console.log(e.response?.data);
        }
    } catch(e) {
        console.error('Error in script:', e.message);
    }
}
test();
