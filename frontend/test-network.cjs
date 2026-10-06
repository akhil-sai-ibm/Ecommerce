const puppeteer = require('puppeteer');
(async () => {
    const browser = await puppeteer.launch({
        executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
        headless: "new"
    });
    const page = await browser.newPage();

    page.on('request', request => {
        if (request.url().includes('/api/cart')) {
            console.log('\n--- REQUEST ---');
            console.log('URL:', request.url());
            console.log('METHOD:', request.method());
            console.log('HEADERS:', request.headers());
            console.log('POST DATA:', request.postData());
        }
    });

    page.on('response', async response => {
        if (response.url().includes('/api/cart')) {
            console.log('\n--- RESPONSE ---');
            console.log('URL:', response.url());
            console.log('STATUS:', response.status());
            console.log('HEADERS:', response.headers());
            try {
                const text = await response.text();
                console.log('BODY:', text);
            } catch (e) {
                console.log('BODY: (could not read)', e.message);
            }
        }
    });
    
    page.on('console', msg => {
        console.log('PAGE CONSOLE:', msg.text());
    });
    page.on('pageerror', err => {
        console.log('PAGE ERROR:', err);
    });

    try {
        await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle2' });
        
        await page.type('#email', 'brandnew@example.com');
        await page.type('#password', 'Password123!');
        await page.click('button[type="submit"]');
        
        await page.waitForNavigation({ waitUntil: 'networkidle0' });
        
        await page.goto('http://localhost:5173/products', { waitUntil: 'networkidle2' });
        
        console.log("Looking for Add to Cart button...");
        await page.waitForSelector('.product-actions button.btn-primary');
        console.log("Clicking Add to Cart button...");
        await page.click('.product-actions button.btn-primary');
        
        console.log("Clicked! Waiting 3 seconds for requests to settle...");
        await new Promise(r => setTimeout(r, 3000));
        
    } catch (e) {
        console.error("Test failed:", e);
    } finally {
        await browser.close();
    }
})();
