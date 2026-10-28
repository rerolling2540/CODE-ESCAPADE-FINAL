import { FileBlob, PresentationFile } from 'file:///C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const path='C:/CODE-ESCAPADE/CODE-ESCAPADE-FINAL/deliverables/Presentation1_Current_Game.pptx';
const p=await PresentationFile.importPptx(await FileBlob.load(path));
console.log(JSON.stringify({slides:p.slides.items.length, title:p.slides.items[0].name ?? null}));
