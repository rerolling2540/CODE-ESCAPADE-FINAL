from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DRAFTS = ROOT / "Assets" / "Data" / "Cards" / "Playable Class Cards"
CATALOG = ROOT / "Assets" / "Resources" / "InventoryCardCatalog.asset"
MERCHANT = ROOT / "Assets" / "Resources" / "NPC" / "SyntaxMerchantCatalog.asset"
SQL = ROOT / "Assets" / "Data" / "Supabase" / "class_card_library_seed.sql"

STATUS = {"ARMOR": 0, "BURN": 1, "POISON": 2, "BLEED": 3, "FROST": 4, "NATURE": 5}
cards: dict[str, dict] = {}


def hit(name, description, amount, target="selected", status=None, extra=None, combo=None):
    cards[name] = {"description": description, "damage": amount, "target": target,
                   "status": status, "extra": extra or [], "combo": combo}


def self_effect(name, description, effect, amount, second=None):
    cards[name] = {"description": description, "extra": [(effect, amount)] + ([second] if second else [])}


# Archer · C#
hit("Arrowfall", "Deal 5 damage to all enemies.", 5, "all")
hit("Briar Trap", "Deal 6 damage and apply 2 Poison to one enemy.", 6, status=("POISON", 2))
hit("Crescent Volley", "Deal 12 damage to one random enemy.", 12, "random")
hit("Crosswind", "Deal 9 damage to one enemy and gain 5 Armor.", 9, extra=[("armor", 5)])
hit("Disarming Shaft", "Deal 5 damage and apply 1 Frost to one enemy.", 5, status=("FROST", 1))
hit("Echo Shot", "Deal 8 damage to one enemy.", 8)
hit("Falcon Dive", "Deal 12 damage to one enemy.", 12)
self_effect("Field Dressing", "Restore 5 health.", "heal", 5)
hit("Frost Volley", "Deal 5 damage and apply 1 Frost to all enemies.", 5, "all", ("FROST", 1))
self_effect("Greenstring", "Gain 1 mana this turn.", "mana", 1)
hit("Hunter's Focus", "Deal 5 damage and apply 1 Bleed to one enemy.", 5, status=("BLEED", 1))
hit("Ice Tip", "Deal 8 damage and apply 2 Frost to one enemy.", 8, status=("FROST", 2))
hit("Leafstorm", "Deal 8 damage to all enemies.", 8, "all")
hit("Luminous Mark", "Deal 5 damage plus 3 for each Bleed stack on one enemy.", 5, combo=("BLEED", 3))
hit("Moonlit Shot", "Deal 7 damage to one enemy.", 7)
hit("Needle Shot", "Deal 9 damage to one enemy.", 9)
hit("Piercing Line", "Deal 10 damage to one enemy.", 10)
hit("Pinning Arrow", "Deal 6 damage and apply 1 Frost to one enemy.", 6, status=("FROST", 1))
hit("Quick Volley", "Deal 12 damage to one enemy.", 12)
hit("Scattershot", "Deal 9 damage to all enemies.", 9, "all")
self_effect("Shadow Step", "Gain 5 Armor.", "armor", 5)
hit("Split Fletching", "Deal 12 damage to one enemy.", 12)
self_effect("Stillwater Aim", "Gain 5 Armor and enter Nature for 3 turns.", "armor", 5,
            ("nature", 3))
cards["Targeting Lens"] = {"description": "Apply 2 Bleed to one enemy.", "manual_status": ("BLEED", 2)}
hit("Thunder Fletch", "Deal 8 damage and apply 1 Burn to one enemy.", 8, status=("BURN", 1))
hit("Tidal Arrow", "Deal 9 damage and apply 2 Bleed to one enemy.", 9, status=("BLEED", 2))
hit("Verdant Arrow", "Deal 7 damage and enter Nature for 3 turns.", 7, extra=[("nature", 3)])
hit("Vinebound Bolt", "Deal 7 damage and apply 1 Poison to one enemy.", 7, status=("POISON", 1))
self_effect("Wind Thread", "Draw 1 card and gain 1 mana this turn.", "draw", 1, ("mana", 1))
cards["Winter Snare"] = {"description": "Apply 2 Frost to one enemy.", "manual_status": ("FROST", 2)}

# Mage · Python
hit("Arcane Matrix", "Deal 6 damage and gain 1 mana this turn.", 6, extra=[("mana", 1)])
self_effect("Astral Compass", "Draw 2 cards.", "draw", 2)
hit("Beam Cascade", "Deal 15 damage and apply 1 Burn to one enemy.", 15, status=("BURN", 1))
self_effect("Binary Ward", "Gain 8 Armor.", "armor", 8)
hit("Blizzard Script", "Deal 12 damage and apply 2 Frost to all enemies.", 12, "all", ("FROST", 2))
hit("Chain Spark", "Deal 10 damage to one enemy.", 10)
hit("Comet Array", "Deal 18 damage to all enemies.", 18, "all")
self_effect("Conjure Familiar", "Gain 8 Armor.", "armor", 8)
hit("Crystal Lance", "Deal 9 damage to one enemy.", 9)
hit("Ember Orb", "Deal 7 damage and apply 2 Burn to one enemy.", 7, status=("BURN", 2))
hit("Encrypted Grimoire", "Deal 6 damage to one random enemy.", 6, "random")
self_effect("Forest Memory", "Restore 5 health and draw 1 card.", "heal", 5, ("draw", 1))
hit("Fractal Flame", "Deal 12 damage and apply 1 Burn to one enemy.", 12, status=("BURN", 1))
hit("Frostfire", "Deal 10 damage, apply 1 Burn and 1 Frost to one enemy.", 10,
    status=("BURN", 1), extra=[("target_status", ("FROST", 1))])
hit("Glacier Pin", "Deal 8 damage and apply 2 Frost to one enemy.", 8, status=("FROST", 2))
self_effect("Growth Formula", "Gain 5 Armor and restore 3 health.", "armor", 5, ("heal", 3))
self_effect("Minor Restoration", "Restore 7 health.", "heal", 7)
hit("Nightshade Pact", "Deal 18 damage to one enemy.", 18)
cards["Plague Script"] = {"description": "Apply 2 Poison to one enemy.", "manual_status": ("POISON", 2)}
self_effect("Portal Step", "Gain 6 Armor.", "armor", 6)
self_effect("Radiant Ward", "Gain 10 Armor.", "armor", 10)
hit("Root Sigil", "Deal 4 damage and apply 1 Poison to one enemy.", 4, status=("POISON", 1))
hit("Rune Bolt", "Deal 8 damage to one enemy.", 8)
self_effect("Rune Compiler", "Gain 1 mana this turn.", "mana", 1)
self_effect("Starwell", "Gain 2 mana this turn.", "mana", 2)
self_effect("Temporal Loop", "Draw 1 card and gain 1 mana this turn.", "draw", 1, ("mana", 1))
hit("Thunder Glyph", "Deal 10 damage and apply 1 Frost to one enemy.", 10, status=("FROST", 1))
cards["Viridian Mist"] = {"description": "Apply 2 Poison to one enemy.", "manual_status": ("POISON", 2)}
hit("Void Prism", "Deal 4 damage plus 3 for each Poison stack on one enemy.", 4, combo=("POISON", 3))
hit("Winter Spirit", "Deal 6 damage and apply 1 Frost to all enemies.", 6, "all", ("FROST", 1))

# Warrior · Java
self_effect("Ashen Reversal", "Gain 6 Armor.", "armor", 6)
self_effect("Battle Rhythm", "Draw 1 card and gain 1 mana this turn.", "draw", 1, ("mana", 1))
hit("Blade Flourish", "Deal 10 damage to one enemy.", 10)
hit("Blazing Edge", "Deal 8 damage and apply 2 Burn to one enemy.", 8, status=("BURN", 2))
hit("Blood Price", "Deal 14 damage to one enemy.", 14)
self_effect("Breaker Stance", "Gain 6 Armor.", "armor", 6)
hit("Cinder Cut", "Deal 8 damage to one enemy.", 8)
hit("Cleaving Arc", "Deal 14 damage to one enemy.", 14)
self_effect("Counter Pattern", "Gain 5 Armor.", "armor", 5)
self_effect("Crimson Parry", "Gain 6 Armor.", "armor", 6)
hit("Ember Cleave", "Deal 5 damage and apply 1 Burn to all enemies.", 5, "all", ("BURN", 1))
self_effect("Forge Temper", "Gain 8 Armor.", "armor", 8)
hit("Furnace Heart", "Deal 5 damage and apply 2 Burn to one enemy.", 5, status=("BURN", 2))
self_effect("Granite Guard", "Gain 10 Armor.", "armor", 10)
hit("Iron Fist", "Deal 7 damage and apply 1 Bleed to one enemy.", 7, status=("BLEED", 1))
self_effect("Iron Stance", "Gain 6 Armor.", "armor", 6)
self_effect("Last Bastion", "Gain 12 Armor.", "armor", 12)
self_effect("Last Stand", "Gain 15 Armor.", "armor", 15)
self_effect("Lionheart", "Restore 6 health.", "heal", 6)
self_effect("Rallying Cry", "Gain 3 Armor and 1 mana this turn.", "armor", 3, ("mana", 1))
self_effect("Relentless March", "Gain 1 mana this turn.", "mana", 1)
hit("Seismic Step", "Deal 5 damage and apply 1 Frost to all enemies.", 5, "all", ("FROST", 1))
hit("Shield Splitter", "Deal 10 damage and apply 2 Bleed to one enemy.", 10, status=("BLEED", 2))
hit("Storm Hammer", "Deal 11 damage and apply 2 Frost to one enemy.", 11, status=("FROST", 2))
hit("Sundering Blow", "Deal 12 damage and apply 1 Bleed to one enemy.", 12, status=("BLEED", 1))
hit("Sundering Rush", "Deal 8 damage plus 3 for each Bleed stack on one enemy.", 8, combo=("BLEED", 3))
hit("Titan's Descent", "Deal 20 damage to one enemy.", 20)
self_effect("War Drum", "Draw 1 card and gain 2 Armor.", "draw", 1, ("armor", 2))
hit("Warden's Mark", "Deal 6 damage and apply 2 Bleed to one enemy.", 6, status=("BLEED", 2))
self_effect("Warrior Oath", "Gain 8 Armor.", "armor", 8)


def yaml_data(effect_type: str, data: dict) -> str:
    if effect_type == "AddStatusEffectEffect":
        parts = [f"        statusEffectType: {data['statusEffectType']}", f"        stackCount: {data.get('stackCount', 1)}"]
        parts += [f"        natureTurnsUntilHeal: {data.get('natureTurnsUntilHeal', 3)}",
                  f"        natureHealAmount: {data.get('natureHealAmount', 10)}"]
        return "\n".join(parts)
    if effect_type == "HealEffect": return f"        healAmount: {data['healAmount']}"
    if effect_type == "DrawCardsEffect": return f"        drawAmount: {data['drawAmount']}"
    if effect_type == "GainManaEffect": return f"        amount: {data['amount']}"
    if effect_type == "StatusComboDamageEffect":
        return "\n".join([f"        baseDamage: {data['baseDamage']}",
                           f"        bonusStatus: {data['bonusStatus']}",
                           f"        damagePerStack: {data['damagePerStack']}",
                           f"        consumeStatusStacks: {1 if data.get('consumeStatusStacks') else 0}"])
    return ""


def effect_row(rid: int, effect_type: str, data: dict | None = None) -> str:
    type_line = f"      type: {{class: {effect_type}, ns: , asm: Assembly-CSharp}}"
    if data:
        return f"    - rid: {rid}\n{type_line}\n      data:\n{yaml_data(effect_type, data)}"
    return f"    - rid: {rid}\n{type_line}\n      data:"


def target_type(name: str) -> str:
    return {"selected": "SelectedEnemyTM", "all": "AllEnemiesTM", "random": "RandomEnemiesTM",
            "hero": "HeroTM", "none": "NoTM"}[name]


def status_data(status_name: str, stacks: int) -> dict:
    if status_name == "NATURE":
        return {"statusEffectType": STATUS[status_name], "natureTurnsUntilHeal": stacks, "natureHealAmount": 10}
    return {"statusEffectType": STATUS[status_name], "stackCount": stacks}


def write_card(path: Path, class_name: str, card_guid: str, merchant_rows: list, sql_rows: list):
    raw = path.read_text(encoding="utf-8-sig")
    spec = cards[path.stem]
    match = re.search(r"<Mana>k__BackingField: (\d+)", raw)
    mana = int(match.group(1)) if match else 0
    prefix = raw[:raw.index("  <Description>k__BackingField:")]
    output = prefix
    output += f"  <Description>k__BackingField: {json.dumps(spec['description'], ensure_ascii=False)}\n"
    output += f"  <Mana>k__BackingField: {mana}\n"
    output += re.search(r"  <Image>k__BackingField: .*\n", raw).group(0)

    refs = ["    - rid: -2", "      type: {class: , ns: , asm: }"]
    rid = 101
    manual = None
    auto = []
    damage = spec.get("damage")
    target = spec.get("target", "selected")
    if damage is not None:
        if target == "selected": manual = ("DealDamageEffect", {})
        else: auto.append((target, "DealDamageEffect", {}))
    if spec.get("combo"):
        status_name, per_stack = spec["combo"]
        manual = ("StatusComboDamageEffect", {"baseDamage": damage, "bonusStatus": STATUS[status_name],
                                               "damagePerStack": per_stack, "consumeStatusStacks": False})
        damage = None
    if "manual_status" in spec:
        status_name, stacks = spec["manual_status"]
        manual = ("AddStatusEffectEffect", status_data(status_name, stacks))
    if "status" in spec and spec["status"]:
        status_name, stacks = spec["status"]
        if target == "selected": auto.append(("selected", "AddStatusEffectEffect", status_data(status_name, stacks)))
        else: auto.append((target, "AddStatusEffectEffect", status_data(status_name, stacks)))

    def append_extra(extra):
        kind, amount = extra
        if kind == "target_status":
            status_name, stacks = amount
            auto.append((target, "AddStatusEffectEffect", status_data(status_name, stacks)))
        elif kind == "armor": auto.append(("hero", "AddStatusEffectEffect", status_data("ARMOR", amount)))
        elif kind == "nature": auto.append(("hero", "AddStatusEffectEffect", status_data("NATURE", amount)))
        elif kind == "heal": auto.append(("hero", "HealEffect", {"healAmount": amount}))
        elif kind == "draw": auto.append(("none", "DrawCardsEffect", {"drawAmount": amount}))
        elif kind == "mana": auto.append(("none", "GainManaEffect", {"amount": amount}))

    for extra in spec.get("extra", []): append_extra(extra)
    if auto:
        output += "  <OtherEffects>k__BackingField:\n"
        for op_target, effect_type, data in auto:
            mode_rid, effect_rid = rid, rid + 1
            rid += 2
            output += f"  - <TargetMode>k__BackingField:\n      rid: {mode_rid}\n    <Effect>k__BackingField:\n      rid: {effect_rid}\n"
            refs.append(effect_row(mode_rid, target_type(op_target)))
            refs.append(effect_row(effect_rid, effect_type, data))
    else:
        output += "  <OtherEffects>k__BackingField: []\n"
    if manual:
        manual_rid = rid
        refs.append(effect_row(manual_rid, manual[0], manual[1]))
        output += f"  <ManualTargetEffect>k__BackingField:\n    rid: {manual_rid}\n"
    else:
        output += "  <ManualTargetEffect>k__BackingField:\n    rid: -2\n"

    if damage is None:
        damage_type, base_damage, percent = 0, 0, 0
    else:
        damage_type, base_damage, percent = 0, int(damage), 0
    output += f"  damageType: {damage_type}\n  baseDamage: {base_damage}\n  percentDamage: {percent}\n  damageMultiplier: 1\n"
    output += "  references:\n    version: 2\n    RefIds:\n" + "\n".join(refs) + "\n"
    path.write_text(output, encoding="utf-8")

    offer_id = "merchant.classcard." + card_guid
    card_id = "classcard." + card_guid
    class_index = {"Warrior - Java": 0, "Archer - CSharp": 1, "Mage - Python": 2}[class_name]
    price = 25 + 10 * mana
    merchant_rows.append(
        f"  - id: {offer_id}\n    title: {path.stem}\n    description: {spec['description']}\n"
        f"    kind: 0\n    currency: 0\n    price: {price}\n    hero: {class_index}\n"
        f"    card: {{fileID: 11400000, guid: {card_guid}, type: 2}}\n    cardId: {card_id}\n"
        "    cosmeticTitle: \n    titleColor: {r: 1, g: 1, b: 1, a: 1}"
    )
    sql_quote = lambda value: "'" + str(value).replace("'", "''") + "'"
    sql_rows.append("(" + ", ".join([sql_quote(card_id), sql_quote(path.stem), sql_quote(spec["description"]),
                                      str(mana), str(damage_type), str(base_damage), str(percent), "1"]) + ")")
    return card_id


files = list(DRAFTS.rglob("*.asset"))
if len(files) != 90:
    raise SystemExit(f"Expected 90 class cards, found {len(files)}")
all_guids, class_rows, merchant_rows, sql_rows = [], [], [], []
status_count = 0
for path in sorted(files):
    class_name = path.parent.name
    meta = Path(str(path) + ".meta").read_text(encoding="utf-8-sig")
    guid = re.search(r"^guid: ([0-9a-f]+)$", meta, re.MULTILINE).group(1)
    if path.stem not in cards: raise SystemExit(f"Missing implementation for {path.stem}")
    card_id = write_card(path, class_name, guid, merchant_rows, sql_rows)
    class_rows.append(f"  - id: {card_id}\n    card: {{fileID: 11400000, guid: {guid}, type: 2}}")
    if cards[path.stem].get("status") or cards[path.stem].get("manual_status") or cards[path.stem].get("combo") or any(x[0] in ("nature", "target_status") for x in cards[path.stem].get("extra", [])):
        status_count += 1

catalog_text = CATALOG.read_text(encoding="utf-8-sig")
catalog_ids = set(re.findall(r"^  - id: (.+)$", catalog_text, re.MULTILINE))
new_class_rows = [row for row in class_rows if row.splitlines()[0].removeprefix("  - id: ") not in catalog_ids]
if new_class_rows:
    CATALOG.write_text(catalog_text.rstrip() + "\n" + "\n".join(new_class_rows) + "\n", encoding="utf-8")

merchant_text = MERCHANT.read_text(encoding="utf-8-sig")
merchant_ids = set(re.findall(r"^  - id: (.+)$", merchant_text, re.MULTILINE))
new_merchant_rows = [row for row in merchant_rows if row.splitlines()[0].removeprefix("  - id: ") not in merchant_ids]
if new_merchant_rows:
    MERCHANT.write_text(merchant_text.rstrip() + "\n" + "\n".join(new_merchant_rows) + "\n", encoding="utf-8")
SQL.write_text(
    "-- Add the playable class card library to the editable master card table.\n"
    "-- Run after card_inventory_schema.sql. These stable keys are also used by Unity's inventory and merchant catalogs.\n"
    "insert into public.cards\n"
    "    (game_card_key, name, description, mana, damage_type, base_damage, percent_damage, damage_multiplier)\nvalues\n    "
    + ",\n    ".join(sql_rows)
    + "\non conflict (game_card_key) do nothing;\n",
    encoding="utf-8",
)
print(f"Promoted {len(files)} playable class cards; {status_count} include status mechanics; added {len(new_class_rows)} inventory and {len(new_merchant_rows)} merchant entries.")
