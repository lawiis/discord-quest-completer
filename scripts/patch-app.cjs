const fs = require('fs');
const zlib = require('zlib');
const path = require('path');

function toVA(fileOffset) {
  return BigInt(fileOffset) + 0x140000000n + 0x1600n;
}

const exePath = path.join(__dirname, 'discord-quest-completer.exe');
const distDir = path.join(__dirname, 'source', 'dist');

console.log('Reading files...');
const buf = fs.readFileSync(exePath);

const html = fs.readFileSync(path.join(distDir, 'index.html'));
const css = fs.readFileSync(path.join(distDir, 'assets', 'index-DuvfGG4J.css'));
const js = fs.readFileSync(path.join(distDir, 'assets', 'index-DpPauhri.js'));

console.log('Compressing with Brotli (quality 11)...');
const bHtml = zlib.brotliCompressSync(html, { params: { [zlib.constants.BROTLI_PARAM_QUALITY]: 11 } });
const bCss = zlib.brotliCompressSync(css, { params: { [zlib.constants.BROTLI_PARAM_QUALITY]: 11 } });
const bJs = zlib.brotliCompressSync(js, { params: { [zlib.constants.BROTLI_PARAM_QUALITY]: 11 } });

console.log(`Sizes: HTML=${bHtml.length}, JS=${bJs.length}, CSS=${bCss.length}`);

const htmlPathStr = Buffer.from('/index.html');
const jsPathStr = Buffer.from('/assets/index-DpPauhri.js');
const cssPathStr = Buffer.from('/assets/index-DuvfGG4J.css');

// Calculate offsets
const htmlPathOffset = 11965420;
const htmlDataOffset = htmlPathOffset + htmlPathStr.length;

const jsPathOffset = htmlDataOffset + bHtml.length;
const jsDataOffset = jsPathOffset + jsPathStr.length;

const cssPathOffset = jsDataOffset + bJs.length;
const cssDataOffset = cssPathOffset + cssPathStr.length;

const endOffset = cssDataOffset + bCss.length;
// Can expand through unused /tauri.svg and /vite.svg up to where /assets/gamelist starts
const maxAvailable = 12024980;

console.log(`Layout:
  HTML Path: [${htmlPathOffset}..${htmlDataOffset}]
  HTML Data: [${htmlDataOffset}..${jsPathOffset}] (${bHtml.length} bytes)
  JS Path:   [${jsPathOffset}..${jsDataOffset}]
  JS Data:   [${jsDataOffset}..${cssPathOffset}] (${bJs.length} bytes)
  CSS Path:  [${cssPathOffset}..${cssDataOffset}]
  CSS Data:  [${cssDataOffset}..${endOffset}] (${bCss.length} bytes)
  End Offset: ${endOffset} / Max Available: ${maxAvailable} (Headroom: ${maxAvailable - endOffset} bytes)
`);

if (endOffset > maxAvailable) {
  throw new Error(`Data exceeds maximum available space by ${endOffset - maxAvailable} bytes!`);
}

// Write the asset paths and data into buffer
htmlPathStr.copy(buf, htmlPathOffset);
bHtml.copy(buf, htmlDataOffset);

jsPathStr.copy(buf, jsPathOffset);
bJs.copy(buf, jsDataOffset);

cssPathStr.copy(buf, cssPathOffset);
bCss.copy(buf, cssDataOffset);

// Fill padding with zeroes
buf.fill(0, endOffset, maxAvailable);

// Update Asset Table in .rdata
// index.html table entry at 12025048
buf.writeBigUInt64LE(toVA(htmlPathOffset), 12025048);
buf.writeBigUInt64LE(BigInt(htmlPathStr.length), 12025056);
buf.writeBigUInt64LE(toVA(htmlDataOffset), 12025064);
buf.writeBigUInt64LE(BigInt(bHtml.length), 12025072);

// index.js table entry at 12025080
buf.writeBigUInt64LE(toVA(jsPathOffset), 12025080);
buf.writeBigUInt64LE(BigInt(jsPathStr.length), 12025088);
buf.writeBigUInt64LE(toVA(jsDataOffset), 12025096);
buf.writeBigUInt64LE(BigInt(bJs.length), 12025104);

// index.css table entry at 12025112
buf.writeBigUInt64LE(toVA(cssPathOffset), 12025112);
buf.writeBigUInt64LE(BigInt(cssPathStr.length), 12025120);
buf.writeBigUInt64LE(toVA(cssDataOffset), 12025128);
buf.writeBigUInt64LE(BigInt(bCss.length), 12025136);

// Clear unused /tauri.svg table entry at 12025144
buf.writeBigUInt64LE(0n, 12025144);
buf.writeBigUInt64LE(0n, 12025152);
buf.writeBigUInt64LE(0n, 12025160);
buf.writeBigUInt64LE(0n, 12025168);

// Clear unused /vite.svg table entry at 12025176
buf.writeBigUInt64LE(0n, 12025176);
buf.writeBigUInt64LE(0n, 12025184);
buf.writeBigUInt64LE(0n, 12025192);
buf.writeBigUInt64LE(0n, 12025200);

fs.writeFileSync(exePath, buf);
console.log('Successfully patched discord-quest-completer.exe with newly built UI/UX assets!');
