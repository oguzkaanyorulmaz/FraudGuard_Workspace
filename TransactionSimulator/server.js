const http = require('http');
const fs = require('fs');
const path = require('path');
const PORT = process.env.PORT || 4000;
const API_HOST = process.env.API_HOST || 'localhost';
const API_PORT = process.env.API_PORT || 5217;

const MIME_TYPES = {
    '.html': 'text/html; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.js': 'application/javascript; charset=utf-8',
    '.json': 'application/json',
    '.png': 'image/png',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon'
};

const server = http.createServer((req, res) => {
    // Proxy API requests to backend
    if (req.url.startsWith('/api/')) {
        const options = {
            hostname: API_HOST,
            port: API_PORT,
            path: req.url,
            method: req.method,
            headers: {
                ...req.headers,
                host: `${API_HOST}:${API_PORT}`
            }
        };

        const proxyReq = http.request(options, (proxyRes) => {
            // Forward CORS headers
            res.writeHead(proxyRes.statusCode, {
                ...proxyRes.headers,
                'Access-Control-Allow-Origin': '*'
            });
            proxyRes.pipe(res);
        });

        proxyReq.on('error', (err) => {
            res.writeHead(502, { 'Content-Type': 'application/json; charset=utf-8' });
            res.end(JSON.stringify({
                error: 'Backend bağlantı hatası',
                details: err.message
            }));
        });

        req.pipe(proxyReq);
        return;
    }

    // Serve static files
    let filePath = req.url === '/' ? '/index.html' : req.url.split('?')[0];
    filePath = path.join(__dirname, filePath);

    const ext = path.extname(filePath);
    const contentType = MIME_TYPES[ext] || 'application/octet-stream';

    fs.readFile(filePath, (err, data) => {
        if (err) {
            if (err.code === 'ENOENT') {
                res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
                res.end('Dosya bulunamadı');
            } else {
                res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
                res.end('Sunucu hatası');
            }
            return;
        }
        res.writeHead(200, { 'Content-Type': contentType });
        res.end(data);
    });
});

server.listen(PORT, () => {
    console.log('');
    console.log('  ╔══════════════════════════════════════════════╗');
    console.log('  ║   🛡️  FraudGuard İşlem Simülatörü           ║');
    console.log('  ╠══════════════════════════════════════════════╣');
    console.log(`  ║   🌐  http://localhost:${PORT}                  ║`);
    console.log(`  ║   📡  API Proxy → http://${API_HOST}:${API_PORT}      ║`);
    console.log('  ╚══════════════════════════════════════════════╝');
    console.log('');
});
