const axios = require('axios');

async function checkProductImageUrls() {
    try {
        const searchRes = await axios.get('http://localhost:5000/api/products/search?pageNumber=1&pageSize=10', {
            validateStatus: () => true
        });
        const products = searchRes.data;
        for (const p of products) {
            console.log(`Product: ${p.name}, ImageUrl: ${p.imageUrl}`);
        }
    } catch(e) {
        console.error('Error:', e.message);
    }
}
checkProductImageUrls();
