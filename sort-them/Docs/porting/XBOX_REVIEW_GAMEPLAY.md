# Sort Them Cars: Sorting Simulator — Gameplay Description for Platform Review

(Facts verified against game code and data on 2026-10-12. Completion time: fill in from playtests.)

## 1. Overview

Sort Them Cars is a first-person, single-player sorting sandbox. The player is alone in a toy warehouse: a huge pile of 3,005 toy cars lies on the floor, and 16 display racks with 305 shelves stand along the walls. The whole game is one task: pick cars out of the pile and put every car on the shelf where it belongs. There is no combat, no enemies, no timer, no fail state, no story cutscenes, no online features and no multiplayer.

## 2. Game modes

There is exactly one mode: a persistent free-play sandbox on a single map. There are no levels, chapters, missions, difficulty settings or separate campaign. Everything below happens inside that one mode.

- **Tutorial** — built into the first minutes of a new game, four short prompts (see 4.2). It cannot be selected separately; it runs automatically once and is marked as done in the save.
- **Upgrade Terminal** — an in-world machine where the player spends earned money on 9 upgrades (see 4.5).
- **Slot Machine** — an in-world mini-game: pay $100, spin, receive a random bonus upgrade, a parts crate, or a bomb (see 4.7).
- **Special car assembly** — a collection side activity: 40 parts crates hidden in the pile are spent to install 10 visual parts on each of 5 unique special cars (see 4.8).
- **Blueprint wall** — a decorative trophy wall; one framed blueprint appears for each fully completed category (see 4.9).
- **Small easter eggs** — a cash register that pays $10 once after 10 presses, and a radio that switches between 3 music tracks.

## 3. World structure

| Element | Count | Notes |
|---|---|---|
| Toy cars in the pile | 3,005 | 150 regular models × 20 copies, plus 5 unique special cars |
| Car models | 155 | 150 regular (10 per category) + 5 special |
| Categories | 16 | 15 regular + "Special" |
| Racks | 16 | one per category, each with a sign showing the category name |
| Shelves | 305 | 15 racks × 20 shelves (10 slots each) + 5 single-slot shelves on the Special rack |
| Parts crates | 40 | hidden inside the pile |
| Special cars | 5 | Electra, Hyperion, Icarus, Nemesis, Orion |

Regular categories: Classic Sports, Extreme, GT Racing A, GT Racing R, Hatchbacks, Muscle Cars, Pickups, Police, Rally & Tuning, Sedans, Emergency & Service, Sport Coupes, Supercars, SUVs, Vans & Food Trucks.

Each shelf holds one model only: the first car placed on an empty shelf defines which model that shelf accepts, and it then takes up to 10 copies of that model. Every regular model has 20 copies, so each model fills exactly two shelves.

## 4. Step-by-step walkthrough

### 4.1 Launch and title
The player sees the title screen: background art, the game logo, music and a blinking "Press Any Button" prompt. Any button starts the game. There is no main menu: the game loads the warehouse and automatically continues the existing save, or starts a fresh warehouse if there is none.

### 4.2 Tutorial (first launch only)
A small prompt card appears in the top-right corner with the controller glyph for the required action. Each step completes by doing the action:
1. "Walk around" — move about 2 metres with the left stick.
2. "Look around" — turn the camera 90 degrees with the right stick.
3. "Pick up any car" — aim at a car in the pile and press RT. The car goes into the hand/inventory.
4. "Place it on the highlighted rack" — the rack for the category of the car in hand is highlighted; walk to it, aim at a shelf and press LT.

A green check mark confirms completion and the game saves. During the tutorial, abilities, the terminal, the slot machine and the other interactables are disabled so the player cannot get lost.

What the player thinks: "I can walk, look, grab and place. The sign on the rack tells me where this car goes."

### 4.3 Core loop
What the player sees: a first-person view of the warehouse, a dot crosshair, the pile of cars in front, racks with category signs around the walls. HUD: top-left counters "cars sorted / 3005", "shelves closed / 305" and crates found; top-right balance in dollars; bottom-right the inventory ("3/5") with the active car drawn larger; bottom-left ability icons once unlocked.

What the player does:
1. Walks up to the pile and aims at a car. The car's name shows on hover.
2. Presses RT to take it. Inventory starts at 5 slots; the player can hold several cars and cycle the active one with LB/RB.
3. Reads the car (shape, name) and decides which category it belongs to, then walks to that rack. Each shelf has a price tag showing the model name and fill count ("4/10").
4. Aims at an empty shelf, or at a shelf that already has the same model, and presses LT to place it. If the aim is not on a valid shelf, LT throws the car instead.
5. Each car placed on a rack of the correct category pays $1 immediately. The price tag is white when correct.
6. A car can also be placed on a rack of the wrong category: the game allows it, but pays nothing and the price tag turns red, so the shelf never counts as complete. The player can pick the car back up (RT while aiming at the shelf), which refunds the reward in reverse.
7. When a shelf gets all 10 copies of the correct model, it "closes": the cars bounce, a sound and rumble play, the tag turns gold, and the shelves-closed counter increases. The game autosaves.

What the player thinks: "This is a Police car, so it goes on the Police rack. That shelf already has three of this model, I need seven more. Where are the rest in the pile?"

### 4.4 Money and economy
Only correct placements earn money ($1 per car, so $3,005 in total over a full game). Picking a car back off a shelf takes the dollar back, and the balance can go negative (shown in red). Money is spent at the Upgrade Terminal and the Slot Machine. No real-money purchases exist in the console version.

### 4.5 Upgrade Terminal
An interactable machine on the floor. Aiming at it and pressing RT opens a window listing 9 upgrades with name, description, level pips and price:

| Upgrade | Levels | Prices ($) | Effect |
|---|---|---|---|
| Bigger inventory | 10 | 30 → 120 | +1 inventory slot per level (5 → 15) |
| Longer reach | 4 | 20, 30, 40, 50 | take/place from farther away |
| Unlock sprint | 1 | 50 | hold L3 to run |
| Unlock crouch | 1 | 30 | B toggles crouch, to reach cars under shelves |
| Stronger throw | 3 | 20, 50, 70 | thrown cars fly farther |
| Auto-place on throw | 1 | 100 | a car thrown into its own rack snaps onto a shelf |
| Find matches | 1 | 100 | unlocks ability 1 |
| Auto-collect | 1 | 150 | unlocks ability 2 |
| Rack highlight | 1 | 100 | unlocks ability 3 |

What the player thinks: "Sorting one car at a time is slow. A bigger inventory and auto-collect will let me gather all 20 copies of a model in one trip."

### 4.6 Abilities (after unlocking)
Each needs a car in hand and has a 60-second cooldown shown as a radial meter on its HUD icon:
- **Find matches (D-pad up)** — every loose copy of the model in hand rises out of the pile, spins and glows for 30 s, so the player can see where the other copies are.
- **Auto-collect (D-pad left)** — for 10 s, matching cars within 8 m fly into the inventory one by one until it is full.
- **Rack highlight (D-pad right)** — highlights the rack for the car in hand for 30 s.

### 4.7 Slot Machine
An arcade cabinet in the warehouse. RT opens it; "Spin · $100" starts a spin. Outcomes: one of 24 bonus rewards (extra inventory slots, faster cooldown, longer ability duration, wider auto-collect radius, or a parts crate), or with 35% chance a bomb. When all 24 rewards are gone, every spin drops a bomb. A bomb appears in the pile; the player can pick it up (3-second fuse) and throw it to scatter cars in a 1.2 m radius. Bombs do no damage and carry no penalty; they are a toy for digging through the pile.

### 4.8 Parts crates and special cars
40 parts crates are buried in the pile and appear as the player digs. RT on a crate collects it ("Parts crate: +1"). Five unique special cars are also hidden in the pile; each goes on its own single-slot shelf on the Special rack, which locks once placed. Aiming at a placed special car and holding RT for 1.2 s spends one crate and installs the next of 10 visual parts (headlights, bumpers, exhaust, roof lights, engine, decals, spoiler, turbine, wheels or police light). Fully upgrading all five cars needs 50 crates: 40 from the pile and 10 from the slot machine. The upgrade is purely cosmetic.

### 4.9 Blueprint wall
A wall with 15 empty frames, one per regular category. When every shelf of a category is closed, that category's blueprint animates into its frame. It is a visual trophy and has no gameplay effect.

### 4.10 Completion
The game is complete when all 305 shelves are closed (every car on the correct shelf, including the 5 special cars). At that moment a message appears at the bottom of the screen: "All shelves closed. The warehouse is in perfect order!" with a rumble. There is no credits sequence or end screen; the player may keep walking around, rearranging cars or finishing special car upgrades.

## 5. Levels and completion time

There are no levels. Progress is a single continuous percentage: cars sorted out of 3,005 and shelves closed out of 305, both always visible in the HUD. Progress persists across sessions through the save.

Estimated time to complete the whole game (all 305 shelves): **[__ hours — fill in from playtest]**. Reaching the first upgrades takes a few minutes; the tutorial takes under a minute.

## 6. Saving

One automatic save slot. The game saves every 60 seconds, after each closed shelf, found crate, purchase, special car upgrade, blueprint and at the end of the tutorial, and immediately when the app loses focus or is suspended. "Saved" appears under the balance. The pause menu has a manual "Save" button and "Reset progress" (with a confirmation) that wipes the save and restarts the warehouse.

## 7. Controls (Xbox controller)

| Action | Input |
|---|---|
| Move | Left stick |
| Look | Right stick |
| Take (car, crate, bomb) / interact with terminal, slot machine, radio, register | RT |
| Place on shelf, or throw if not aiming at a valid shelf | LT |
| Jump | A |
| Crouch (toggle, after upgrade) | B |
| Sprint (hold, after upgrade) | L3 |
| Previous / next car in hand | LB / RB |
| Ability 1 / 2 / 3 | D-pad up / left / right |
| Pause | Menu |
| Menus | D-pad or left stick to navigate, A select, B back |

## 8. Pause menu and settings

Pause: Resume, Save, Settings, Controls, Shuffle the pile (re-stacks all loose cars into the piles, used if cars get scattered around the room), Reset progress.
Settings: music volume, effects volume, horizontal and vertical look sensitivity, language (19 languages), vibration on/off.
