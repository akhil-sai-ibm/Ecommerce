const puppeteer = require('puppeteer-core');

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
        console.log("PAGE CONTENT DUMP:");
        console.log(content.substring(0, 5000)); // just output some of it
        
    } catch (e) {
        console.error(e);
    } finally {
        await browser.close();
    }
})();
