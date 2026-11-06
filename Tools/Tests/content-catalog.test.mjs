import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {pathToFileURL, fileURLToPath} from 'node:url';
const {PGlite} = await import(pathToFileURL(process.argv[2]).href);
const db = new PGlite();
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const sql = name => fs.readFile(path.join(root,'Assets/Data/Supabase',name),'utf8');
let checks=0;
const check=(a,b,label)=>{assert.deepEqual(a,b,label);checks++;};
async function rejects(query,code,label){await assert.rejects(()=>db.exec(query),e=>e.code===code,label);checks++;}
async function email(value){await db.query("select set_config('request.jwt.claims',$1,false)",[JSON.stringify({email:value})]);}
try {
  await db.exec(`create role anon; create role authenticated; create schema auth;
    create function auth.jwt() returns jsonb language sql stable as $$select current_setting('request.jwt.claims',true)::jsonb$$;
    grant usage on schema public,auth to anon,authenticated;
    grant execute on function auth.jwt() to anon,authenticated;`);
  await db.exec((await sql('cards_schema.sql')).replace('create extension if not exists pgcrypto;',''));
  await db.exec(await sql('question_bank_schema.sql'));
  await db.exec(await sql('current_cards_seed.sql'));
  await db.exec(`create table public.test_ownership(card_key text references public.cards(game_card_key),quantity int);
    insert into public.test_ownership select game_card_key,1 from public.cards where source_pack='CARDS' limit 1;`);
  const migration=await sql('content_catalog_flow.sql');
  await db.exec(migration);
  const key=(await db.query('select card_key from public.test_ownership')).rows[0].card_key;
  await email('adeventurerank60@gmail.com');
  await db.exec('set role authenticated');
  await db.query('update public.cards set name=$1,percent_damage=40 where game_card_key=$2',['Edited card',key]);
  check((await db.query('select percent_damage::int as damage from public.cards where game_card_key=$1',[key])).rows[0].damage,40,'Admin edits persist');
  await db.query(`insert into public.cards(game_card_key,name,template_key,character_class,shop_available)
    values('remote.new','New card',$1,'warrior',true)`,[key]);
  await db.query('update public.cards set is_active=false,shop_available=false where game_card_key=$1',[key]);
  await db.exec("update public.question_bank set published=false where id='java-e01'");
  await rejects("delete from public.cards where game_card_key='remote.new'",'42501','Admin uses retirement, not destructive deletion');
  await rejects("delete from public.question_bank where id='java-e01'",'42501','Unpublishing preserves question identities');
  await rejects("update public.cards set game_card_key='changed' where game_card_key='remote.new'",'P0001','Ownership IDs are immutable');
  await rejects("update public.question_bank set id='changed' where id='java-e01'",'P0001','Question history IDs are immutable');
  await rejects("update public.cards set minimum_question_tier=3,maximum_question_tier=1 where game_card_key='remote.new'",'23514','Invalid difficulty bounds rejected');
  await db.exec('reset role');
  await db.exec(migration);
  check((await db.query('select is_active,shop_available from public.cards where game_card_key=$1',[key])).rows[0],{is_active:false,shop_available:false},'Rerunning migration preserves retirement and shop choices');
  check((await db.query('select quantity from public.test_ownership where card_key=$1',[key])).rows[0].quantity,1,'Retirement preserves owned copies');
  check((await db.query("select count(*)::int n from public.test_ownership where card_key='remote.new'")).rows[0].n,0,'Adding content never grants ownership');
  await email('player@example.com'); await db.exec('set role authenticated');
  await db.exec("update public.cards set name='unauthorized' where game_card_key='remote.new'");
  check((await db.query("select name from public.cards where game_card_key='remote.new'")).rows[0].name,'New card','Normal players cannot edit catalog');
  await rejects("insert into public.cards(game_card_key,name) values('bad','bad')",'42501','Normal players cannot add content');
  await db.exec('reset role; set role anon');
  check((await db.query('select name from public.cards where game_card_key=$1',[key])).rows[0].name,'Edited card','Guests can download updated definitions, including owned retired cards');
  check((await db.query("select count(*)::int n from public.question_bank where id='java-e01'")).rows[0].n,0,'Guests cannot download unpublished questions');
  check((await db.query("select count(*)::int n from public.question_bank where id='java-e02'")).rows[0].n,1,'Published questions remain available offline after caching');
  await rejects("update public.cards set name='bad'",'42501','Guests have no catalog write privilege');
  console.log(`${checks} content catalog integration checks passed.`);
} finally {await db.close();}
