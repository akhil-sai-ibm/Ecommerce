const axios = require('axios');

async function testCorsOn401() {
    try {
        const res = await axios.post('http://localhost:5000/api/cart/items', {}, {
            headers: {
                'Origin': 'http://localhost:5174'
            },
            validateStatus: () => true
        });
        console.log('Status:', res.status);
        console.log('Headers:', res.headers);
    } catch(e) {
        console.log('Error:', e.message);
    }
}
testCorsOn401();
