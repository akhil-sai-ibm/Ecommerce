const axios = require('axios');

async function testCartOptions() {
    try {
        console.log('\n--- Test CORS Preflight for POST /api/cart/items ---');
        const optionsRes = await axios.options('http://localhost:5000/api/cart/items', {
            headers: {
                'Origin': 'http://localhost:5174',
                'Access-Control-Request-Method': 'POST',
                'Access-Control-Request-Headers': 'Authorization,Content-Type'
            }
        });
        console.log('OPTIONS status:', optionsRes.status);
    } catch(e) {
        console.error('Error Status:', e.response?.status);
        console.error('Error Data:', e.response?.data);
    }
}
testCartOptions();
