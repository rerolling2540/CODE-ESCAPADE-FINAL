# NPC authoring preferences

- Place new NPCs as normal, selectable prefab instances in the intended Unity scene hierarchy. Save their scene placement so the user can move them with the editor tools.
- Do not use runtime-only spawning for authored NPCs unless the user explicitly requests it. Preserve the user's chosen NPC positions.
- All NPCs must use the elf's shared DialogueCanvas through DialogueManager and the shared NPC dialogue settings. Use the reusable Shared Dialogue NPC prefab and Npc_Talk for future NPCs; do not create a separate dialogue canvas per NPC.
