import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Authoring repair for the active catalog. Runtime targeting uses these serialized
// effects, never the description text. Run without --write to audit the catalog.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
async function walk(dir) {
  const files = [];
  for (const entry of await fs.readdir(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) files.push(...await walk(p));
    else if (p.endsWith('.asset')) files.push(p);
  }
  return files;
}
const damage = { cls: 'DealDamageEffect', data: '' };
const isDamage = effect => effect?.cls === 'DealDamageEffect';
const key = effect => effect.cls + ':' + effect.data.trim();
let changed = 0;
let manualCount = 0;
for (const file of await walk(path.join(root, 'Assets/Data/Cards/CARDS'))) {
  const original = await fs.readFile(file, 'utf8');
  const t = original.replaceAll('\r\n', '\n');
  const description = t.match(/<Description>k__BackingField: ([\s\S]*?)\n  <Mana>/)[1].replace(/\n\s*/g, ' ');
  const refs = new Map();
  for (const match of t.matchAll(/^    - rid: (-?\d+)\n      type: \{class: ([^,]*),[^\n]*\n([\s\S]*?)(?=^    - rid:|$(?![\s\S]))/gm)) {
    const data = match[3].replace(/^      data: *\n?/, '').trimEnd();
    refs.set(match[1], match[2] ? { cls: match[2], data } : null);
  }
  const manual = refs.get(t.match(/<ManualTargetEffect>k__BackingField:\n    rid: (-?\d+)/)[1]);
  const otherBlock = t.match(/  <OtherEffects>k__BackingField:([\s\S]*?)\n  attackVfxType:/)[1];
  const oldOthers = [...otherBlock.matchAll(/rid: (-?\d+)\n    <Effect>k__BackingField:\n      rid: (-?\d+)/g)]
    .map(m => ({ target: refs.get(m[1])?.cls, effect: refs.get(m[2]) })).filter(e => e.effect && e.target);
  const dealsDamage = /\bdeal(?:ing)?\b.*\bdamage\b/i.test(description);
  const selected = dealsDamage && /\bone enemy(?:'s)?\b/i.test(description) && !/random/i.test(description);
  const target = selected ? 'SelectedEnemyTM' : /all enemies/i.test(description) ? 'AllEnemiesTM' : 'RandomEnemiesTM';
  const hits = dealsDamage ? /\btwice\b/i.test(description) ? 2 : Number(description.match(/\b(\d+) times\b/i)?.[1] || 1) : 0;
  const wantedManual = selected ? damage : null;
  if (selected) manualCount++;
  const others = [];
  for (let i = selected ? 1 : 0; i < hits; i++) others.push({ target, effect: damage });
  // Keep existing secondary effect amounts, but target enemy debuffs at enemies
  // and personal buffs at the hero. Drop accidental duplicate manual buffs.
  const secondary = oldOthers.filter(e => !isDamage(e.effect));
  const sameEffectKind = (a, b) => a.cls === b.cls && (a.cls !== 'AddStatusEffectEffect'
    || a.data.match(/statusEffectType: (\d+)/)?.[1] === b.data.match(/statusEffectType: (\d+)/)?.[1]);
  if (manual && !isDamage(manual) && !secondary.some(e => sameEffectKind(e.effect, manual))) {
    secondary.push({ target: 'HeroTM', effect: manual });
  }
  for (const entry of secondary) {
    const kind = entry.effect.cls === 'AddStatusEffectEffect'
      ? Number(entry.effect.data.match(/statusEffectType: (\d+)/)?.[1]) : null;
    const enemyStatus = kind !== null && [1, 2, 3, 4].includes(kind);
    const correctedTarget = enemyStatus ? target : ['DrawCardsEffect', 'GainManaEffect', 'HealEffect', 'AddStatusEffectEffect'].includes(entry.effect.cls)
      ? entry.effect.cls === 'DrawCardsEffect' ? 'NoTM' : 'HeroTM' : entry.target;
    others.push({ target: correctedTarget, effect: entry.effect });
  }
  const signature = (m, list) => JSON.stringify({ manual: m ? key(m) : null, others: list.map(e => [e.target, key(e.effect)]) });
  if (signature(manual, oldOthers) === signature(wantedManual, others)) continue;
  changed++;
  console.log(`${path.basename(file, '.asset')}: ${selected ? 'choose one enemy' : dealsDamage ? target : 'automatic self/status effect'}`);
  let nextId = 1001;
  // Unity requires the null sentinel in RefIds whenever a field uses rid -2.
  const definitions = wantedManual ? [] : ['    - rid: -2\n      type: {class: , ns: , asm: }\n'];
  function add(effect) {
    const id = nextId++;
    definitions.push(`    - rid: ${id}\n      type: {class: ${effect.cls}, ns: , asm: Assembly-CSharp}\n      data:${effect.data ? '\n' + effect.data : ''}\n`);
    return id;
  }
  const manualId = wantedManual ? add(wantedManual) : -2;
  const wrappers = others.map(e => `  - <TargetMode>k__BackingField:\n      rid: ${add({ cls: e.target, data: '' })}\n    <Effect>k__BackingField:\n      rid: ${add(e.effect)}\n`).join('');
  let updated = t.replace(/  <ManualTargetEffect>k__BackingField:[\s\S]*?(?=  attackVfxType:)/,
    `  <ManualTargetEffect>k__BackingField:\n    rid: ${manualId}\n  <OtherEffects>k__BackingField:${wrappers ? '\n' + wrappers : ' []\n'}`);
  updated = updated.replace(/  references:[\s\S]*$/, `  references:\n    version: 2\n    RefIds:\n${definitions.length ? definitions.join('') : '    - rid: -2\n      type: {class: , ns: , asm: }\n'}`);
  if (process.argv.includes('--write')) await fs.writeFile(file, updated.replaceAll('\n', original.includes('\r\n') ? '\r\n' : '\n'));
}
console.log(`${changed} cards ${process.argv.includes('--write') ? 'repaired' : 'need repair'}; ${manualCount} cards require an enemy selection.`);
