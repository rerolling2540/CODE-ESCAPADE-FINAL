import fs from "node:fs/promises";
const { FileBlob, PresentationFile } = await import("file:///C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs");
const source = "C:/Users/ACER/Downloads/Presentation1 (2).pptx";
const pres = await PresentationFile.importPptx(await FileBlob.load(source));
const info = await pres.inspect({ kind: "slide,textbox,shape,image,table,chart,notes,thread,layout", maxChars: 40000 });
console.log(info.ndjson);
console.log("SLIDES=" + pres.slides.items.length);
const out = "C:/CODE-ESCAPADE/CODE-ESCAPADE-FINAL/.codex-pptx-review";
await fs.mkdir(out, { recursive: true });
for (let i = 0; i < pres.slides.items.length; i++) {
  const png = await pres.export({ slide: pres.slides.items[i], format: "png", scale: 0.5 });
  await fs.writeFile(`${out}/slide-${String(i + 1).padStart(2, "0")}.png`, new Uint8Array(await png.arrayBuffer()));
}
console.log("RENDERED=" + out);
