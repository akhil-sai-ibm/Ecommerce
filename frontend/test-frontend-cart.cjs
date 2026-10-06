const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: true });
    const page = await browser.newPage();

    // Enable request interception to log them
    page.on('request', request => {
        if (request.url().includes('/api/cart/items') && request.method() === 'POST') {
            console.log('--- ADD-TO-CART REQUEST ---');
            console.log('Method:', request.method());
            console.log('URL:', request.url());
            console.log('Request body:', request.postData());
            console.log('Authorization:', request.headers()['authorization']);
        }
    });

    page.on('response', async response => {
        if (response.url().includes('/api/cart/items') && response.request().method() === 'POST') {
            console.log('Status:', response.status());
            try {
                const text = await response.text();
                console.log('Response:', text);
            } catch(e) {}
            console.log('---------------------------');
        }
    });

    await page.goto('http://localhost:5173/login');
    
    // Fill login
    await page.waitForSelector('input[type="email"]');
    await page.type('input[type="email"]', 'brandnew@example.com');
    await page.type('input[type="password"]', 'Password123!');
    await page.click('button[type="submit"]');

    // Wait for the product page to load
    console.log("Waiting for products to load...");

    // Find the add to cart button
    console.log("Looking for Add to Cart button...");
    await page.waitForSelector('.product-actions button.btn-primary');
    
    // Click add to cart
    console.log("Clicking Add to Cart...");
    await page.click('.product-actions button.btn-primary');
    
    // Wait a moment for network
    await new Promise(r => setTimeout(r, 2000));
    
    console.log("Done.");
    await browser.close();
})();
