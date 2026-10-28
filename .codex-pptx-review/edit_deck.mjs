import fs from 'node:fs/promises';
import path from 'node:path';
const { FileBlob, PresentationFile } = await import('file:///C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs');
const source = 'C:/Users/ACER/Downloads/Presentation1 (2).pptx';
const workspaceDir = 'C:/CODE-ESCAPADE/CODE-ESCAPADE-FINAL';
const buildDir = path.join(workspaceDir, '.codex-pptx-review', 'build');
const previewDir = path.join(workspaceDir, '.codex-pptx-review', 'updated-slides');
const outputDir = path.join(workspaceDir, 'deliverables');
const candidatePath = path.join(buildDir, 'Presentation1_Current_Game.candidate.pptx');
const finalPath = path.join(outputDir, 'Presentation1_Current_Game.pptx');
await fs.mkdir(buildDir,{recursive:true});
await fs.mkdir(previewDir,{recursive:true});
await fs.mkdir(outputDir,{recursive:true});
try { await fs.access(finalPath); throw new Error(`Refusing to overwrite existing output: ${finalPath}`); } catch (e) { if (e.message.startsWith('Refusing')) throw e; }
const pres = await PresentationFile.importPptx(await FileBlob.load(source));
const ss = n => path.join(workspaceDir, 'Assets', 'Screenshots', n);
const edits = [
  { id:'im/etgnm5o7', file:ss('MainMenu-Final.png'), alt:'Current CodeEscapade main menu and hero selection' },
  { id:'im/x43et4rq', file:ss('Demon-Door-Java-Challenge.png'), alt:'Current CodeEscapade Java coding challenge' },
  { id:'im/zqd03a5o', file:ss('MainMenu-Final.png'), alt:'Current CodeEscapade main menu' },
  { id:'im/kn61ozmd', file:ss('Scene2-NPC-Player-Layering.png'), alt:'Current CodeEscapade overworld gameplay interface' },
  { id:'im/94nqhsbu', file:ss('Scene2-NPC-Player-Layering.png'), alt:'Current CodeEscapade overworld gameplay interface' },
  { id:'im/8je9ona9', file:ss('MainMenu-Final.png'), alt:'Current CodeEscapade hero selection in the main menu' },
  { id:'im/8budoj6p', file:ss('Executioner-Battle.png'), alt:'Current CodeEscapade turn-based card battle' },
  { id:'im/9c3eho7u', file:ss('Demon-Door-Java-Challenge.png'), alt:'Current CodeEscapade programming question interface' },
  { id:'im/7alcfep4', file:ss('MainMenu-Settings.png'), alt:'Current CodeEscapade settings interface' },
];
function pngSize(b) {
  if (b.toString('ascii',1,4) !== 'PNG') throw new Error('Replacement is not a PNG');
  return { width:b.readUInt32BE(16), height:b.readUInt32BE(20) };
}
for (const edit of edits) {
  const img=pres.resolve(edit.id);
  const oldFrame={...img.frame};
  const oldGeometry=img.geometry;
  const oldBorderRadius=img.borderRadius;
  const oldRotation=img.rotation;
  const oldFlipHorizontal=img.flipHorizontal;
  const oldFlipVertical=img.flipVertical;
  const oldLockAspectRatio=img.lockAspectRatio;
  const bytes=await fs.readFile(edit.file);
  const {width,height}=pngSize(bytes);
  const newHeight=oldFrame.width*height/width;
  const newFrame={left:oldFrame.left,top:oldFrame.top+(oldFrame.height-newHeight)/2,width:oldFrame.width,height:newHeight};
  img.replace({blob:new Uint8Array(bytes),contentType:'image/png',alt:edit.alt,fit:'cover'});
  img.frame=newFrame;
  img.crop={left:0,top:0,right:0,bottom:0};
  if(oldGeometry!==undefined) img.geometry=oldGeometry;
  if(oldBorderRadius!==undefined) img.borderRadius=oldBorderRadius;
  if(oldRotation!==undefined) img.rotation=oldRotation;
  if(oldFlipHorizontal!==undefined) img.flipHorizontal=oldFlipHorizontal;
  if(oldFlipVertical!==undefined) img.flipVertical=oldFlipVertical;
  if(oldLockAspectRatio!==undefined) img.lockAspectRatio=oldLockAspectRatio;
}
// Refresh the old password mockup with the current in-game settings screen.
{
  const img=pres.resolve('im/qd0f2hk3');
  const bytes=await fs.readFile(ss('MainMenu-Settings.png'));
  const {width,height}=pngSize(bytes);
  const h=450, w=h*width/height;
  img.replace({blob:new Uint8Array(bytes),contentType:'image/png',alt:'Current CodeEscapade settings screen',fit:'cover'});
  img.frame={left:(1280-w)/2,top:174,width:w,height:h};
  img.crop={left:0,top:0,right:0,bottom:0};
}
// Keep captions accurate for the replaced screens.
pres.resolve('sh/gb6tsvmx').text.replace('Login Interface:','Main Menu:');
pres.resolve('sh/faxsjq5c').text.replace('Create Account Interface:','World Exploration:');
pres.resolve('sh/ahc7qdor').text.replace('Change password','Settings Interface');
const candidate=await PresentationFile.exportPptx(pres);
await candidate.save(candidatePath);
for(let i=0;i<pres.slides.items.length;i++){
  if(![0,1,10,11,12,13].includes(i)) continue;
  const png=await pres.export({slide:pres.slides.items[i],format:'png',scale:1});
  await fs.writeFile(path.join(previewDir,`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await png.arrayBuffer()));
}
console.log(JSON.stringify({candidatePath,finalPath,slides:pres.slides.items.length,imagesUpdated:edits.length+1,previewDir},null,2));
