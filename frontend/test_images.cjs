const axios = require('axios');

async function testImages() {
    try {
        const res = await axios.get('http://localhost:5000/uploads/products/899e8a60-80e3-473f-822b-9c5a9cfed23e.jpg', {
            validateStatus: () => true
        });
        console.log('Status:', res.status);
    } catch(e) {
        console.error('Error:', e.message);
    }
}
testImages();
