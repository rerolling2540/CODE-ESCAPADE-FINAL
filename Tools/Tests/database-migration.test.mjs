import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import { pathToFileURL, fileURLToPath } from 'node:url';

// Runs only against a disposable, embedded PostgreSQL instance. No Supabase credentials.
const { PGlite } = await import(pathToFileURL(process.argv[2]).href);
const db = new PGlite();
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const sqlFile = name => fs.readFile(path.join(root, 'Assets/Data/Supabase', name), 'utf8');
let checks = 0;
const check = (actual, expected, label) => { assert.deepEqual(actual, expected, label); checks++; };
const uid = '11111111-1111-4111-8111-111111111111';
const otherUid = '22222222-2222-4222-8222-222222222222';
async function identity(id = uid, email = 'player@example.com') {
  await db.query("select set_config('request.jwt.claim.sub',$1,false),set_config('request.jwt.claims',$2,false)",
    [id, JSON.stringify({ sub: id, email })]);
}
async function rejects(action, code, label) {
  await assert.rejects(action, e => e.code === code, label); checks++;
}
const snapshot = (slot, time, value, inventory = '') => {
  const s = { activeSlot: slot, selectedCharacter: 0, modifiedUtc: time,
    slots: Array(10).fill(''), inventories: Array(10).fill(''), slotModifiedUtc: Array(10).fill('') };
  s.slots[slot] = value; s.inventories[slot] = inventory; s.slotModifiedUtc[slot] = time;
  return s;
};
let baseline = Array(10).fill('');
let conflicts = [];
const sync = async (s, observed = baseline) => {
  const result = (await db.query('select public.sync_player_save($1::jsonb,$2::jsonb) as result',
    [JSON.stringify(s),JSON.stringify(observed)])).rows[0].result;
  baseline = result.save_data.slotModifiedUtc;
  conflicts = result.conflicted_slots;
  return result.save_data;
};
try {
  await db.exec(`create role anon; create role authenticated; create schema auth;
    create table auth.users(id uuid primary key);
    insert into auth.users values('${uid}'),('${otherUid}');
    create function auth.uid() returns uuid language sql stable as
      $$select nullif(current_setting('request.jwt.claim.sub',true),'')::uuid$$;
    create function auth.jwt() returns jsonb language sql stable as
      $$select coalesce(nullif(current_setting('request.jwt.claims',true),''),'{}')::jsonb$$;
    grant usage on schema public,auth to authenticated,anon;
    grant execute on function auth.uid(),auth.jwt() to authenticated,anon;`);
  await db.exec((await sqlFile('cards_schema.sql')).replace('create extension if not exists pgcrypto;', ''));
  for (const file of ['player_saves_schema.sql','card_inventory_schema.sql','question_bank_schema.sql','question_performance_schema.sql'])
    await db.exec(await sqlFile(file));
  await db.exec('grant all on public.cards,public.player_saves,public.player_card_inventory_slots,public.player_card_inventory to anon,authenticated');
  // Reproduce an older project that rejected slot four before the migration.
  await db.exec(`alter table public.player_card_inventory_slots drop constraint player_card_inventory_slots_slot_index_check;
    alter table public.player_card_inventory_slots add constraint player_card_inventory_slots_slot_index_check check(slot_index between 0 and 2);`);
  const migration = await sqlFile('asap_database_fixes.sql');
  await db.exec(migration);
  await identity();
  await db.exec('set role authenticated');
  const original = (await db.query("select description from public.cards where name='Arrow Rain' limit 1")).rows[0].description;
  await db.exec("update public.cards set description='unauthorized change' where name='Arrow Rain'");
  check((await db.query("select description from public.cards where name='Arrow Rain' limit 1")).rows[0].description, original, 'Normal users cannot update cards');
  await rejects(() => db.exec("insert into public.cards(name) values('Unauthorized')"), '42501', 'Normal users cannot create cards');
  await rejects(() => db.exec('truncate public.cards cascade'), '42501', 'Normal users cannot bypass RLS through TRUNCATE');
  await rejects(() => db.exec('truncate public.player_card_inventory'), '42501', 'Normal users cannot truncate everyone’s inventory');
  await identity(uid, 'adeventurerank60@gmail.com');
  await db.exec("update public.cards set description='admin edit' where name='Arrow Rain'");
  check((await db.query("select description from public.cards where name='Arrow Rain' limit 1")).rows[0].description, 'admin edit', 'Admin can edit cards');
  await identity();
  const t1 = '2026-11-06T01:00:00Z', t2 = '2026-11-06T02:00:00Z', t3 = '2026-11-06T03:00:00Z';
  await sync(snapshot(0,t2,'new-save','new-items'));
  let merged = await sync(snapshot(0,t1,'old-save','old-items'));
  check(merged.slots[0], 'new-save', 'Stale devices cannot overwrite newer saves');
  check(merged.inventories[0], 'new-items', 'Inventory stays attached to its winning save');
  merged = await sync(snapshot(1,t1,'different-slot'));
  check([merged.slots[0],merged.slots[1]], ['new-save','different-slot'], 'Independent slot changes merge');
  const staleBaseline = [...baseline];
  merged = await sync(snapshot(0,'2026-11-06T02:30:00Z','other-device-new-save'));
  merged = await sync(snapshot(0,'2026-11-06T02:59:00Z','stale-device-later-clock'),staleBaseline);
  check(merged.slots[0], 'other-device-new-save', 'Out-of-date baseline cannot overwrite a newer branch even with a later clock');
  check(conflicts, [0], 'Server reports the conflicting slot for a device backup');
  merged = await sync(snapshot(0,t3,''));
  check([merged.slots[0],merged.inventories[0]], ['',''], 'Newer deletion clears the slot and its items');
  merged = await sync(snapshot(0,t2,'resurrected'));
  check(merged.slots[0], '', 'An older device cannot resurrect a deletion');
  merged = await sync(snapshot(0,t3,'same-timestamp'));
  check(merged.slots[0], '', 'Timestamp ties preserve the server deletion');
  await rejects(() => db.exec(`update public.player_saves set save_data='{}' where user_id='${uid}'`), '42501', 'Direct upserts cannot bypass the protected RPC');
  const cardKey = (await db.query('select game_card_key from public.cards where game_card_key is not null limit 1')).rows[0].game_card_key;
  await sync(snapshot(9,t3,'slot-ten'));
  const cards = JSON.stringify([{card_key:cardKey,quantity:2,level:1}]);
  await db.query('select public.replace_player_card_inventory(9,$1::jsonb,$2)',[cards,t3]);
  check((await db.query('select quantity from public.player_card_inventory where slot_index=9')).rows[0].quantity, 2, 'Slot ten accepts inventory');
  await rejects(() => db.query('select public.replace_player_card_inventory(9,$1::jsonb,$2)',['[]',t1]), '40001', 'Stale inventory replacement is rejected');
  check((await db.query('select quantity from public.player_card_inventory where slot_index=9')).rows[0].quantity, 2, 'Rejected replacement leaves inventory intact');
  await rejects(() => db.query('select public.replace_player_card_inventory(9,$1::jsonb,$2)',['[]','2026-11-07T01:00:00Z']), '40001', 'Inventories cannot bypass the saved slot version with a future timestamp');
  await db.query(`insert into public.player_question_attempts
    (attempt_id,user_id,slot_index,question_id,language,difficulty,source,player_level,outcome,presented_at)
    values('33333333-3333-4333-8333-333333333333',$1,9,'java-e01','java','easy','battle',1,'correct',now())`,[uid]);
  check((await db.query('select slot_index from public.player_question_attempts')).rows[0].slot_index, 9, 'Question history supports slot ten');
  await rejects(() => db.query('select public.replace_player_card_inventory(10,$1::jsonb,$2)',['[]',t3]), 'P0001', 'Invalid slot eleven is rejected');
  await identity(otherUid);
  check((await db.query('select * from public.player_saves')).rows.length, 0, 'Other users cannot read saves');
  check((await db.query('select * from public.player_card_inventory')).rows.length, 0, 'Other users cannot read owned cards');
  await identity('', '');
  await rejects(() => sync(snapshot(0,t1,'anonymous')), 'P0001', 'RPC rejects missing authentication');
  await db.exec('reset role');
  await db.exec(migration);
  check((await db.query('select quantity from public.player_card_inventory where slot_index=9')).rows[0].quantity, 2, 'Re-running migration preserves owned cards');
  check((await db.query('select count(*)::integer as count from public.cards')).rows[0].count, 26, 'Migration preserves catalog rows');
  console.log(`PASS: ${checks} PostgreSQL checks (permissions, stale writes, deletions, isolation, ten slots, repeatability).`);
} finally { await db.close(); }
