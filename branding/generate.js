// Generates the Listrary icon pack (web + Android) from the mascot fragment.
// Usage (from this folder): npm install --no-save sharp && node generate.js
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');

const root = path.resolve(__dirname, '..');
const mascot = fs.readFileSync(path.join(__dirname, 'src', 'mascot.svgfrag'), 'utf8');
const webOut = path.join(root, 'wwwroot', 'images', 'icons');
const resOut = path.join(root, 'android', 'app', 'src', 'main', 'res');
const storeOut = path.join(__dirname, 'out');

const defs = `<defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
<stop offset="0" stop-color="#0FAF9F"/><stop offset="1" stop-color="#7FFFD4"/></linearGradient></defs>`;
const svg = (inner) =>
  `<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">${defs}${inner}</svg>`;
const scaled = (s, frag = mascot) =>
  `<g transform="translate(${256 - 256 * s} ${256 - 256 * s}) scale(${s})">${frag}</g>`;
const silhouette = mascot
  .replace(/<rect x="116"[^>]*opacity=[^>]*\/>\s*/, '')
  .replace(/#0B3C49/g, '@W')
  .replace(/fill="#[0-9A-Fa-f]+"/g, 'fill="#000"')
  .replace(/stroke="#[0-9A-Fa-f]+"/g, 'stroke="#FFF"')
  .replace(/@W/g, '#FFF');

const variants = {
  rounded: svg(`<rect width="512" height="512" rx="112" fill="url(#bg)"/>` + scaled(1)),
  square: svg(`<rect width="512" height="512" fill="url(#bg)"/>` + scaled(1)),
  maskable: svg(`<rect width="512" height="512" fill="url(#bg)"/>` + scaled(0.72)),
  circle: svg(`<circle cx="256" cy="256" r="256" fill="url(#bg)"/>` + scaled(0.8)),
  foreground: svg(scaled(0.56)),
  mono: svg(scaled(0.56, silhouette)),
  pinned: svg(silhouette),
};

const png = (s, size, file) => sharp(Buffer.from(s)).resize(size, size).png().toFile(file);
const mk = (d) => fs.mkdirSync(d, { recursive: true });

function ico(buffers, sizes) {
  const head = Buffer.alloc(6);
  head.writeUInt16LE(1, 2);
  head.writeUInt16LE(buffers.length, 4);
  let offset = 6 + 16 * buffers.length;
  const dirs = buffers.map((b, i) => {
    const d = Buffer.alloc(16);
    d[0] = sizes[i]; d[1] = sizes[i];
    d.writeUInt16LE(1, 4); d.writeUInt16LE(32, 6);
    d.writeUInt32LE(b.length, 8); d.writeUInt32LE(offset, 12);
    offset += b.length;
    return d;
  });
  return Buffer.concat([head, ...dirs, ...buffers]);
}

const card = (w, h) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 1200 630">${defs}
<rect width="1200" height="630" fill="url(#bg)"/>
<circle cx="1050" cy="80" r="140" fill="#fff" opacity=".12"/><circle cx="120" cy="560" r="110" fill="#fff" opacity=".12"/>
<g transform="translate(40 55) scale(1.0)">${mascot}</g>
<text x="620" y="320" font-family="Verdana, sans-serif" font-weight="bold" font-size="112" fill="#fff">Listrary</text>
<text x="624" y="400" font-family="Verdana, sans-serif" font-size="40" fill="#fff">Make lists. Share the fun!</text></svg>`;

(async () => {
  [webOut, storeOut].forEach(mk);
  fs.writeFileSync(path.join(webOut, 'favicon.svg'), variants.rounded);
  fs.writeFileSync(path.join(webOut, 'safari-pinned-tab.svg'), variants.pinned.replace(/#FFF/g, '#000'));
  fs.writeFileSync(path.join(storeOut, 'icon-source.svg'), variants.rounded);

  const web = { 'favicon-16x16.png': 16, 'favicon-32x32.png': 32, 'favicon-48x48.png': 48,
    'apple-touch-icon.png': 180, 'android-chrome-192x192.png': 192, 'android-chrome-512x512.png': 512, 'mstile-150x150.png': 150 };
  for (const [n, s] of Object.entries(web)) {
    await png(/^(apple|mstile)/.test(n) ? variants.square : variants.rounded, s, path.join(webOut, n));
  }
  await png(variants.maskable, 512, path.join(webOut, 'maskable-512x512.png'));

  const sizes = [16, 32, 48];
  const bufs = await Promise.all(sizes.map((s) => sharp(Buffer.from(variants.rounded)).resize(s, s).png().toBuffer()));
  fs.writeFileSync(path.join(root, 'wwwroot', 'favicon.ico'), ico(bufs, sizes));

  await sharp(Buffer.from(card(1200, 630))).png().toFile(path.join(webOut, 'og-image.png'));

  const dens = { mdpi: 1, hdpi: 1.5, xhdpi: 2, xxhdpi: 3, xxxhdpi: 4 };
  for (const [d, m] of Object.entries(dens)) {
    const dir = path.join(resOut, `mipmap-${d}`); mk(dir);
    await png(variants.rounded, 48 * m, path.join(dir, 'ic_launcher.png'));
    await png(variants.circle, 48 * m, path.join(dir, 'ic_launcher_round.png'));
    await png(variants.foreground, 108 * m, path.join(dir, 'ic_launcher_foreground.png'));
    await png(variants.mono, 108 * m, path.join(dir, 'ic_launcher_monochrome.png'));
  }
  const any = path.join(resOut, 'mipmap-anydpi-v26'); mk(any);
  const adaptive = `<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
    <background android:drawable="@color/ic_launcher_background" />
    <foreground android:drawable="@mipmap/ic_launcher_foreground" />
    <monochrome android:drawable="@mipmap/ic_launcher_monochrome" />
</adaptive-icon>
`;
  fs.writeFileSync(path.join(any, 'ic_launcher.xml'), adaptive);
  fs.writeFileSync(path.join(any, 'ic_launcher_round.xml'), adaptive);
  await png(variants.rounded, 512, path.join(storeOut, 'play-store-icon-512.png'));
  await sharp(Buffer.from(card(1024, 538))).resize(1024, 500, { fit: 'cover' }).png().toFile(path.join(storeOut, 'play-feature-graphic-1024x500.png'));
  console.log('done');
})();
