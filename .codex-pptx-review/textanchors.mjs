const { FileBlob, PresentationFile } = await import('file:///C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs');
const p=await PresentationFile.importPptx(await FileBlob.load('C:/Users/ACER/Downloads/Presentation1 (2).pptx'));
const info=await p.inspect({kind:'textbox',maxChars:50000});
for(const line of info.ndjson.split('\n')){if(!line)continue;const r=JSON.parse(line);if([10,11,12,13].includes(r.slideIndex))console.log(JSON.stringify(r));}
