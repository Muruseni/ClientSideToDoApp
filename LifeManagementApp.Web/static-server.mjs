import { createServer } from 'node:http';
import { readFile } from 'node:fs';
import { extname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// This server is intentionally limited to the static browser client. It does
// not launch the ASP.NET executable that Smart App Control blocked on Windows.
const webRoot = resolve(fileURLToPath(new URL('./wwwroot/', import.meta.url)));
const port = Number(process.env.PORT || 5087);
const contentTypes = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8'
};

createServer((request, response) => {
  const requestedPath = decodeURIComponent((request.url || '/').split('?')[0]);
  const relativePath = requestedPath === '/' ? '/index.html' : requestedPath;
  const filePath = resolve(join(webRoot, `.${relativePath}`));

  // Prevent a browser request from escaping the wwwroot directory.
  if (!filePath.startsWith(`${webRoot}\\`)) {
    response.writeHead(403);
    response.end('Forbidden');
    return;
  }

  readFile(filePath, (error, content) => {
    if (error) {
      response.writeHead(404);
      response.end('Not found');
      return;
    }

    response.writeHead(200, { 'Content-Type': contentTypes[extname(filePath)] || 'application/octet-stream' });
    response.end(content);
  });
}).listen(port, '127.0.0.1', () => {
  console.log(`Static Life Management client available at http://127.0.0.1:${port}`);
});
