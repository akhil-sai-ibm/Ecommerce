const axios = require('axios');

async function testOptions() {
    try {
        const res = await axios.options('http://localhost:5000/api/cart/items', {
            validateStatus: () => true
        });
        console.log('Status:', res.status);
        console.log('Headers:', res.headers);
    } catch(e) {
        console.error('Error:', e.message);
    }
}
testOptions();
