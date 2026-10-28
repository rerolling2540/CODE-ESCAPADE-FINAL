import fs from 'node:fs/promises';
const { FileBlob, PresentationFile } = await import('file:///C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs');
const pres = await PresentationFile.importPptx(await FileBlob.load('C:/Users/ACER/Downloads/Presentation1 (2).pptx'));
const info = await pres.inspect({ kind: 'slide,textbox,shape,image', maxChars: 120000 });
const rows = info.ndjson.split('\n').filter(Boolean).map(x=>JSON.parse(x));
console.log('count', rows.length, 'slides', pres.slides.items.length);
for (const r of rows) { if (r.kind === 'image' || r.kind === 'slide') console.log(JSON.stringify(r)); }
