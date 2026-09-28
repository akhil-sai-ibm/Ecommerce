const axios = require('axios');

async function testRedirect() {
    try {
        const res = await axios.post('http://localhost:5000/api/cart/items', {}, {
            maxRedirects: 0,
            validateStatus: function (status) {
                return status >= 200 && status < 400; // Resolve for 3xx
            }
        });
        console.log('Status:', res.status);
        console.log('Headers:', res.headers);
    } catch(e) {
        console.log('Error Status:', e.response?.status);
        console.log('Error Headers:', e.response?.headers);
    }
}
testRedirect();
