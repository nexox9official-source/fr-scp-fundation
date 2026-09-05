# Site-76 reference for SiteRP

Source project: https://github.com/MujisongPlay/ScpSite76Plugin
Public fork checked: https://github.com/Draakoor/ScpSite76Plugin

## Status

Site-76 is useful as an architectural/design reference for SiteRP, but the public repositories do **not** include the actual `Site76` MapEditorReborn schematic. The plugin activates when MER emits a `SchematicSpawned` event whose name is exactly `Site76`.

Because the schematic itself is missing, this reference must not be treated as an installable map package.

The public repository also has no license file. Do not copy or redistribute its source code into SiteRP. Reimplement compatible ideas independently against current LabAPI/ProjectMER APIs.

## What Site-76 demonstrates

The old Site-76 plugin is a full-facility concept rather than a small vanilla overlay. Its runtime scans named objects inside the schematic and attaches systems to them.

Confirmed concepts:

- `AirlockSystem`: custom airlock behavior.
- `EleSystem`: custom multi-stop elevator system.
- `Map System`: multi-floor minimap infrastructure.
- `Console`: in-world information/monitoring console.
- `RespawnSystem`: dedicated reinforcement arrival animation/system.
- `PocketEscape`: custom Pocket Dimension escape points.
- `DoorInstallPoses`: marker transforms used to install functional SCP:SL doors into a custom facility.
- Custom player spawn points for Class-D, Scientists, Facility Guards, MTF, Chaos and SCP roles.
- Count trackers / personnel displays.
- Helicopter and Chaos vehicle concepts for reinforcement arrival.
- A dedicated custom respawn queue.

## Important implementation clues

The plugin does not build Site-76 itself. It waits for MER to spawn a schematic named `Site76`, then stores the schematic root GameObject and scans its child hierarchy.

This means the missing schematic contained the actual:

- room geometry,
- floors,
- corridor layout,
- elevator shafts/stops,
- airlock markers,
- minimap geometry,
- door installation markers,
- console props,
- respawn areas,
- spawn points.

The plugin only adds logic to those named objects.

## Compatibility with our server

Original Site-76 targets an old EXILED + MapEditorReborn stack from 2023. It is not safe to install directly on SCP:SL 14.2.7.

Our target stack:

- SCP:SL 14.2.7
- LabAPI 1.1.7
- ProjectMER 2026.7.6.1
- SiteRP.Core

Therefore Site-76 should be used as a **design reference**, not as a binary/plugin dependency.

## SiteRP adaptation plan

### Phase A — preserve vanilla gameplay foundations

Keep the current generated SCP:SL facility and its working:

- doors,
- elevators,
- SCP systems,
- SCP-079 support,
- warhead infrastructure,
- room identification,
- navigation/connectors.

Use SiteRP.Core + ProjectMER only to clean, rebuild and expand rooms where doing so is verified safe.

### Phase B — Site-76-inspired operational spaces

Reimplement the strongest Site-76 ideas as modern SiteRP systems:

1. Staff/security checkpoints with proper airlocks.
2. Departmental areas instead of empty decorative rooms.
3. Multi-floor information displays/minimap terminals where useful.
4. Functional personnel-count/status consoles.
5. Proper MTF/Chaos arrival zones controlled by RP events rather than vanilla automatic waves.
6. Purpose-built elevators only where geometry and navigation are fully verified.
7. Door-placement markers for newly constructed ProjectMER rooms.
8. Dedicated medical, research, security, engineering and administration spaces.

### Phase C — larger custom wings

Only after the vanilla-clean version is stable, create optional ProjectMER wings inspired by Site-76:

- Administration / Site Director wing
- Security HQ
- Medical wing / morgue
- Research laboratories
- Engineering / power / maintenance
- Staff cafeteria / break room
- Logistics / storage
- MTF staging and armory
- Controlled SCP test chambers

These should attach to verified valid connection points and must never create doors into void geometry.

## Safety rule for map work

No geometry modification should be made from guesses. Every room expansion or replacement must be based on one of:

- SiteRP map audit data,
- in-game admin markers,
- verified ProjectMER room-local coordinates,
- a tested schematic in a staging copy of the server.

Every SiteRP-created map element should remain reversible through SiteRP.Core whenever technically possible.

## Missing asset

Still missing: the original `Site76` / low-detail Site-76 MER schematic referenced by the old plugin README and runtime event.

If that schematic is later located publicly or supplied by its author, inspect it offline first and use it only as permitted by its licensing/author terms. Do not deploy it directly to production.
