import path from 'node:path';
import { pathToFileURL } from 'node:url';
const skillDir='C:/Users/ACER/.codex/plugins/cache/openai-primary-runtime/presentations/26.1025.11240/skills/presentations';
const { finalizePresentation } = await import(pathToFileURL(path.join(skillDir,'container_tools/artifact_tool_utils.mjs')).href);
const workspaceDir='C:/CODE-ESCAPADE/CODE-ESCAPADE-FINAL';
const stagingDir=path.join(workspaceDir,'.codex-pptx-review','build');
const finalPath=path.join(workspaceDir,'deliverables','Presentation1_Current_Game.pptx');
const candidatePath=path.join(stagingDir,'Presentation1_Current_Game.candidate.pptx');
const result=await finalizePresentation({
  workspaceDir,
  candidatePath,
  finalPath,
  explicitTotalSlideCount:15,
  pythonExecutable:'C:/Users/ACER/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',
  integrityValidatorPath:path.join(skillDir,'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath:path.join(skillDir,'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],
  requiredNativeTableOwnerSlides:[],
  verifyArtifactToolImport:true,
  receiptPath:path.join(stagingDir,'Presentation1_Current_Game.validation.json'),
});
console.log(JSON.stringify(result,null,2));
