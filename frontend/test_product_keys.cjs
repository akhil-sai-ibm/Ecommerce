const axios = require('axios');

async function testFetchProducts() {
    try {
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=1');
        console.log('Product keys:', Object.keys(searchRes.data[0]));
        console.log('Product:', searchRes.data[0]);
    } catch(e) {
        console.error('Error Status:', e.response?.status);
    }
}
testFetchProducts();
