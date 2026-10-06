const puppeteer = require('puppeteer');
(async () => {
    const browser = await puppeteer.launch({
        executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
        headless: "new"
    });
    const page = await browser.newPage();
    
    page.on('dialog', async dialog => {
        console.log('\n--- DIALOG APPEARED ---');
        console.log('MESSAGE:', dialog.message());
        await dialog.accept();
    });

    try {
        await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle0' });
        await page.type('#email', 'brandnew@example.com');
        await page.type('#password', 'Password123!');
        await page.click('button[type="submit"]');
        await page.waitForNavigation({ waitUntil: 'networkidle0' });
        
        await page.goto('http://localhost:5173/products', { waitUntil: 'networkidle0' });
        
        console.log("\nClicking Add to Cart button...");
        await page.click('.product-actions button.btn-primary');
        
        console.log("Waiting 3 seconds...");
        await new Promise(r => setTimeout(r, 3000));
        
    } finally {
        await browser.close();
    }
})();
