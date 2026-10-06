const puppeteer = require('puppeteer-core');
const fs = require('fs');

(async () => {
    const browser = await puppeteer.launch({
        executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
        headless: "new"
    });
    const page = await browser.newPage();
    
    // Capture console
    page.on('console', msg => console.log('PAGE LOG:', msg.text()));

    try {
        await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle2' });
        await page.type('input[type="email"]', 'brandnew@example.com');
        await page.type('input[type="password"]', 'Password123!');
        await page.click('button[type="submit"]');
        
        await page.waitForNavigation({ waitUntil: 'networkidle0' });
        
        console.log("Waiting 3 seconds for products to load...");
        await new Promise(r => setTimeout(r, 3000));
        
        const content = await page.content();
        fs.writeFileSync('page_dump.html', content);
        console.log("Saved page_dump.html");
        
    } catch (e) {
        console.error(e);
    } finally {
        await browser.close();
    }
})();
