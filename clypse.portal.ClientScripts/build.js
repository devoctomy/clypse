/**
 * Simple build script for copying JavaScript files to dist folder
 * This can be extended later to include bundling, minification, etc.
 */

const fs = require('fs');
const path = require('path');

const srcDir = path.join(__dirname, 'src');
const distDir = path.join(__dirname, 'dist');

// Ensure dist directory exists
if (!fs.existsSync(distDir)) {
    fs.mkdirSync(distDir, { recursive: true });
}

// Clean dist directory
fs.readdirSync(distDir).forEach(file => {
    fs.unlinkSync(path.join(distDir, file));
});

// Copy all .js files from src to dist
fs.readdirSync(srcDir).forEach(file => {
    if (file.endsWith('.js')) {
        const srcPath = path.join(srcDir, file);
        const distPath = path.join(distDir, file);
        fs.copyFileSync(srcPath, distPath);
        console.log(`Copied: ${file}`);
    }
});

console.log('Build completed successfully!');
