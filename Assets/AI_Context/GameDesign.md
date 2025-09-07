# 📖 Chapter 1 – Introduction & Vision

## 1.1 Project Context

_Seven Crowns_ is a **turn-based strategy RPG**, inspired by the core mechanics of _Heroes of Might & Magic III_, but modernized with:

* a **AAA-grade implementation in Unity 6.0.39f1 LTS**,
* a **rogue-lite free mode** for high replayability,
* and a **story-driven campaign** tied to meta-progression.

The game’s structure revolves around the symbolism of the number **7** (7 resources, 7 magic schools, 7 factions, 7 crowns), providing thematic coherence and design depth.

---

## 1.2 Vision

The goal is to deliver an experience that is:

* **Strategic** → resource management, city building, territory control.
* **RPG-driven** → hero progression via talents, artifacts, magic, and player choices.
* **Tactical** → dynamic initiative-based turn combat with unit abilities and magic.
* **Epic** → a narrative campaign where the player seeks to unite the **Seven Crowns**.
* **Replayable** → rogue-lite free mode with persistent meta-progression (unlocking artifacts, spells, and factions).

---

## 1.3 MVP Objectives

The MVP should allow players to:

1. **Explore** a map (surface + underground) with a hero.
2. **Collect** the 7 resources via mines, buildings, farms, and exploration.
3. **Build** a city (Village → Capital) and recruit an army.
4. **Fight** with a complete tactical system (initiative, unit special abilities, magic).
5. **Progress heroes** through stats, talents (3 branches × 5 nodes), secondary skills, and artifacts.
6. **Cast spells** (7 magic schools, 14 base spells).
7. **Win a scenario** by capturing the enemy Capital.
8. **Progress through meta** by unlocking persistent artifacts/spells in free mode.

---

## 1.4 Target Audience

* Players nostalgic for _Heroes of Might & Magic_, seeking a modernized experience.
* Fans of **tactical/4X/RPG hybrids** (_Disciples, Endless Legend, Age of Wonders_).
* Players of **rogue-lite strategy games** who value persistence and replay (_Slay the Spire, Rogue Legacy_).

---

## 1.5 Positioning

_Seven Crowns_ differentiates itself through:

* A **hybrid structure** → story campaign + rogue-lite free mode.
* An economy centered on **farms and population**, preventing snowball dominance.
* Hero progression inspired by **Final Fantasy / Diablo** (talent tree + spell mastery through usage).
* Strong **thematic coherence** → the number 7 drives design pillars.

---

## 1.6 Long-Term Vision

In its full scope, the game will include:

* **7 playable factions**, each with unique units, cities, and talent trees.
* A **complete narrative campaign** with multiple chapters and diverse objectives.
* **Procedural map generation** for the free mode.
* **Competitive balancing** and optional multiplayer.

# 📖 Chapter 2 – Exploration & Map

## 2.1 Overview

Exploration is a **core pillar** of _Seven Crowns_.\
The player controls heroes moving across a **tile-based strategic map** composed of terrain, resources, cities, mines, and special points of interest.\
Exploration is turn-based, with **movement points (MP)** limiting daily travel and **fog of war** restricting visibility.

---

## 2.2 Movement System

* **Movement Points (MP):**
  * Each hero has a daily pool of **240 MP**.
  * MP reset every dawn (no carryover).
* **Costs:**
  * Movement cost depends on terrain and direction (cardinal vs diagonal).
  * Example: Grass = 10 MP (cardinal), 14 MP (diagonal).
* **Stopping:**
  * If destination costs exceed remaining MP → hero stops on the last payable tile.
  * Path is preserved visually for next turn.

---

## 2.3 Terrain Types (MVP)

| Terrain | Passable? | Cost | Notes |
|---------|-----------|------|-------|
| Grass | Yes | 10 (cardinal), 14 (diagonal) | Standard terrain |
| Road | Yes | 8 / 11 | Fastest travel |
| Forest | Yes | 15 / 21 | Slower terrain |
| Mountain | No (base) | — | Passable only with artifact (cost 25/36) |
| Water | No | — | Impassable in MVP (reserved for post-MVP naval/magic) |
| Cliff / Ramp | Yes (directional) | 12 / 17 | Entry restricted by `EnterMask` |

👉 Post-MVP: Swamp, Desert, Snow, Shallow Water, Lava.

---

## 2.4 Visibility & Fog of War

* **Vision range:** fixed radius + modifiers (artifacts, talents).
* **Fog of War:**
  * Unexplored tiles hidden.
  * Explored tiles remain visible but do not reveal enemy movements.
* **Artifacts/skills** may extend vision (e.g. _Explorer’s Goggles_ = +1 vision).

---

## 2.5 Map Layers

* **Surface:** forests, plains, cities, resources, standard map content.
* **Underground:** secondary map layer with tunnels, caves, rare mines, hidden passages.
* Connected by **entrances/exits** (stairs, caves, portals).

---

## 2.6 Interactive Objects

* **Resources:** piles of Or, Wood, Iron, Coal, Diamonds, Ether, Sulfur.
* **Mines:** captured to produce 1 resource/day.
* **Farms:** produce population per week (20 per farm).
* **Points of Interest:**
  * Grottos, ruins, towers, temples (combat or reward).
  * Special buildings (e.g. _Tree of Wisdom_, _XP Menhir_, _Fountain of Vitality_).

---

## 2.7 Hero-Environment Interaction

* On entering an object tile, the hero triggers:
  * **Collection:** resources.
  * **Ownership:** mines, farms.
  * **Event/Dialogue:** grottos, ruins.
  * **Combat:** against guardians.

---

## 2.8 Feedback to Player

* **Highlight:** movement range (tiles within MP).
* **Tooltip:** terrain cost, resource type, enemy difficulty (easy → impossible).
* **Mini-map:** always visible, showing explored territory, cities, mines, armies.

---

## 2.9 MVP Summary

* Tilemap with **surface + underground**.
* **5 terrains** + cliffs for tactical movement variety.
* **Fog of War** with persistent exploration.
* **Resources, mines, farms, POIs** populate the map.
* **Mini-map + tooltips** for navigation clarity.

# 📖 Chapter 3 – Economy & Resources

## 3.1 Overview

The economy in _Seven Crowns_ is based on **7 core resources**, aligning with the thematic “Seven.”\
Resources fuel **city development**, **unit recruitment**, **spell acquisition**, and **strategic progression**.\
Economy is complemented by the **Population system**, which determines weekly recruitment limits.

---

## 3.2 Core Resources (MVP)

| Resource | Icon | Usage |
|----------|------|-------|
| **Gold** 💰 | Treasury | Universal currency (units, buildings, heroes). |
| **Wood** 🌲 | Scieries | Basic construction. |
| **Iron** ⛓️ | Forges | Advanced buildings, weapons, fortifications. |
| **Coal** ⛏️ | Mines | Smithing, alchemy, machinery. |
| **Diamonds** 💎 | Rare mines | Elite units, artifacts. |
| **Ether** ✨ | Magical pools | Unlocking spells, enchanting artifacts. |
| **Sulfur** 🔥 | Alchemy pits | Explosives, magical units. |

---

## 3.3 Population (MVP)

* Population is a **weekly-renewed pool of recruits**.
* Determined by **farms owned** (urban or capturable on the map).
* **Weekly reset**: every Monday, total population available = 20 × (number of farms).

| Unit Tier | Example (Knights) | Population Cost |
|-----------|-------------------|-----------------|
| T1 | Peasant | 1 |
| T2 | Archer | 2 |
| T3 | Spearman | 4 |
| T4 | Monk | 6 |
| T5 | Knight | 10 |
| T6 | Paladin | 20 |
| T7 | Angel | 30 |

👉 Example: A castle with **2 farms** (+40 pop) → can recruit 40 peasants or 4 knights that week.

---

## 3.4 Resource Collection

* **On-map piles** → instant collection when hero steps on them.
* **Mines** → produce 1 unit/day of a resource.
* **Farms** → produce weekly population growth.
* **City buildings** → add daily income (Gold, Wood, Iron, etc.).

---

## 3.5 Resource Usage

* **City construction:** requires Gold + Wood/Iron/Coal.
* **Unit recruitment:** requires Gold + resource + Population.
* **Magic:**
  * Casting → Mana (derived from Knowledge stat).
  * Unlocking spells → Ether cost at city buildings.
* **Artifacts:** some require Diamonds, Ether, or Sulfur to craft/purchase.

---

## 3.6 Strategic Role

* Resource distribution encourages **map control**:
  * Mines = continuous economy.
  * Farms = military potential.
* Creates natural **conflict hotspots** (rare mines, Ether pools).
* Balances **short-term vs long-term investment**:
  * Collect piles for immediate boost.
  * Secure mines for sustainable growth.

---

## 3.7 MVP Summary

* **7 resources + Population** form the economic backbone.
* **Gold** = universal currency, **Ether** = magical currency.
* Farms directly control **recruitment limits per week**.
* Mines, city buildings, and exploration provide income.
* Ensures economy is **tight and strategic**, avoiding runaway snowballing.

# 📖 Chapter 4 – Heroes & Progression

## 4.1 Overview

Heroes are the **central agents** of the player:

* They **explore** the map, **lead armies** in combat, and **progress through experience**.
* Heroes combine **base stats, talents, secondary skills, magic mastery, and artifacts** to shape unique builds.

---

## 4.2 Base Stats (MVP)

Heroes start with **4 core stats**:

| Stat | Effect |
|------|--------|
| **Attack (ATK)** ⚔️ | Increases unit damage dealt. |
| **Defense (DEF)** 🛡️ | Reduces damage taken by units. |
| **Magic (MAG)** ✨ | Increases spell power (scaling for damage/healing). |
| **Knowledge (KNW)** 📚 | Determines maximum Mana pool (10 Mana/KNW). |

👉 Post-MVP expansions: **Magic Defense** (resist spells), **Engineering** (boost siege weapons/totems).

---

## 4.3 Leveling & Experience

* Heroes gain **XP** from:
  * Winning combats.
  * Completing quests/events.
  * Visiting special map locations (e.g. Menhirs).
* Level cap (MVP): **30**.
* Each level = **+1 talent point**.

---

## 4.4 Talent Trees

Heroes have **3 branches**, each with **5 nodes (MVP)**:

1. ⚔️ **Combat** → boosts units’ offensive/defensive power.
   * Ex: +1 ATK, +1 DEF, +2 Morale, +10% unit HP, Ultimate: +15% army damage.
2. ✨ **Magic** → enhances spellcasting.
   * Ex: +10 Mana, +5% spell power, unlock Fire School, reduce Ether costs, Ultimate: +15% spell power & -15% Mana cost.
3. 🏛️ **Strategist** → economic and logistic bonuses.
   * Ex: +5% Wood/Iron/Coal production, +10% recruit growth T1–T2, +5% bank yield, +5% Gold income, Ultimate: +10% all resources.

👉 Long-term vision: 15 nodes per branch (45 total).

---

## 4.5 Secondary Skills

Each hero can learn **up to 6 secondary skills** (MVP pool):

* **Pathfinding** → reduce terrain penalties.
* **Logistics** → +5–15% movement.
* **Command** → increase max army size efficiency.
* **Elemental Magic** → unlock Fire/Earth/Air/Water spells.
* **Diplomacy** → recruit neutral units at reduced cost.
* **Economy** → +5–15% global income.

Each has 3 levels: Basic → Advanced → Expert.

---

## 4.6 Magic Mastery

* Each magic school has **10 mastery levels** (0→10).
* Mastery increases with **spell usage** in combat.
* Example (Fire School):
  * Level 1 = unlock _Fireball_.
  * Level 3 = unlock _Wall of Fire_.
  * Level 10 = unlock _Meteor_.
* Each level adds **+5% spell power**.

---

## 4.7 Artifacts & Equipment

Heroes have **7 equipment slots**:

* Head, Body, Weapon, Boots, 2×Rings, Amulet.
* Mini-pool MVP = **11 artifacts** (stat boosts, movement bonuses, terrain reduction).
* Some require **branch expertise** (ex: Heavy weapon → Combat ≥3).

---

## 4.8 Hero Lifecycle

* **Recruitment**: in cities (Tavern), starts Level 1 with simple build.
* **Death**: artifacts drop on battlefield; hero can be **resurrected** in Chapel/Cathedral (cost Gold + Ether).
* **Dismissal**: heroes can be retired to free slots.

---

## 4.9 MVP Summary

* Heroes = RPG progression core.
* Stats (4) + Talent trees (3×5 nodes) + Secondary skills (6) + Magic mastery + Artifacts.
* Progression cap = Level 30, mastery 0–10.
* Multiple heroes can be recruited; the death/resurrection cycle adds stakes.

# 📖 Chapter 5 – Magic & Spells

## 5.1 Overview

Magic in _Seven Crowns_ is structured around **7 distinct schools**, aligned with the theme of Seven.

* **6 combat schools**: Black, White, Fire, Earth, Water, Air.
* **1 strategic school**: Universal (map/world spells).\
  Heroes unlock schools via **talent tree nodes** and improve them by **casting spells in combat** (usage-based mastery).

---

## 5.2 Spellcasting Resources

* **Mana** (pool):
  * Determined by the hero’s **Knowledge (KNW)** stat.
  * Consumed when casting spells in combat.
* **Ether** (currency):
  * Required to **unlock new spells** at magical buildings (Guilds, Libraries, etc.).

---

## 5.3 Magic Schools (MVP)

| School | Theme | Example Spells |
|--------|-------|----------------|
| **Black Magic** ☠️ | Curses, life drain, damage | Drain Life, Weakness |
| **White Magic** ✨ | Healing, blessings, purification | Heal, Divine Protection |
| **Fire Magic** 🔥 | Explosions, AoE, burning zones | Fireball, Wall of Fire |
| **Earth Magic** ⛰️ | Defense, control, entrapment | Stone Skin, Slow |
| **Water Magic** 🌊 | Frost, immobilization | Ice Shards, Freeze |
| **Air Magic** 🌪️ | Speed, lightning, initiative buffs | Lightning, Haste |
| **Universal Magic** 🌌 | World/map manipulation | Teleportation, Resource Harvest |

---

## 5.4 Spell Mastery (0→10)

* Each school has **10 mastery levels**.
* Mastery increases through **spell usage in combat**.
* Effects:
  * +5% power per mastery level.
  * Unlock spells at levels 1, 3, 7, 10.
  * Reduced mana costs at levels 4, 6, 9.

👉 Example: Fire Magic

* L1 → unlock Fireball (+5% power).
* L3 → unlock Wall of Fire (+15%).
* L7 → unlock advanced spell (post-MVP expansion).
* L10 → unlock Meteor (+50%).

---

## 5.5 Spell List (MVP)

Each school has **2 spells** in MVP (14 total).

* **Black Magic:** Drain Life, Weakness.
* **White Magic:** Heal, Divine Protection.
* **Fire Magic:** Fireball, Wall of Fire.
* **Earth Magic:** Stone Skin, Slow.
* **Water Magic:** Ice Shards, Freeze.
* **Air Magic:** Lightning, Haste.
* **Universal Magic:** Teleportation (map), Remote Resource Harvest (map).

---

## 5.6 Combat Integration

* The hero appears in the **initiative bar** with a fixed initiative (≈T3).
* On turn → can cast a spell (costs Mana).
* Spell power = `(base effect + MAG scaling) × (1 + mastery bonus)`.
* Successful cast grants **XP in that school**.

---

## 5.7 MVP Summary

* 7 schools, 14 spells (2 per school).
* Mana for combat use, Ether for unlocking.
* Usage-based mastery (0→10) for progression.
* Universal school = unique map interaction spells.

# 📖 Chapter 6 – Units & Combat

## 6.1 Overview

Combat in _Seven Crowns_ is **turn-based, tactical, and initiative-driven**.\
Armies are composed of **unit stacks** led by a hero.\
The battlefield is a **10×12 grid** with terrain modifiers, obstacles, and elevation bonuses.

---

## 6.2 Unit Tiers (Faction: Knights – MVP)

Each faction has **7 unit tiers (T1–T7)**, progressing from basic militia to ultimate units.\
Units exist in **3 expertise levels** (Basic, Improved, Elite), determined by the level of their recruitment building.

| Tier | Unit | Role | Example Special Ability |
|------|------|------|-------------------------|
| T1 | Peasant | Swarm infantry | _Determination_ → +1 Morale (1×/battle) |
| T2 | Archer | Ranged | _Precise Shot_ → ignores range penalty (CD 3) |
| T3 | Spearman | Defensive infantry | _Pike Wall_ → bonus damage vs cavalry |
| T4 | Monk | Support magic | _Prayer_ → heal small ally stack |
| T5 | Knight | Cavalry | _Charge_ → +50% dmg after movement |
| T6 | Paladin | Elite holy cavalry | _Sacred Aura_ → +1 Morale to allies |
| T7 | Angel | Ultimate flying unit | _Resurrection_ → revive fallen allies (1×/battle) |

---

## 6.3 Unit Stats (MVP baseline)

* **HP (Health Points)**
* **Attack (ATK)**
* **Defense (DEF)**
* **Damage range (min–max)**
* **Speed (movement range per turn)**
* **Initiative (order in the turn bar)**
* **Population cost** (weekly recruitment limit)

👉 Example baseline (Peasant):

* HP 8, ATK 1, DEF 1, DMG 1–2, Speed 4, Init 80, Pop cost 1.

---

## 6.4 Expertise Levels

* **Basic (I):** baseline stats and ability.
* **Improved (II):** +20–30% stats, ability cooldown reduced or buffed.
* **Elite (III):** +50–60% stats, ability fully enhanced or permanent aura.

---

## 6.5 Battlefield

* **Size:** 10×12 tiles.
* **Zones:** attacker deployment (2 cols left), defender deployment (2 cols right).
* **Obstacles:** rocks, trees, ruins (block movement/line of sight).
* **Elevation:** some tiles grant ranged bonus (+20% range, +10% damage).

---

## 6.6 Initiative System

* Each unit has an **Initiative score** (80–130 typical).
* Initiative determines **frequency of turns**:
  * Fast units (cavalry, angels) act more often.
  * Slow units (peasants, trolls) act less often.
* Heroes have fixed initiative (\~100).

---

## 6.7 Actions per Turn

* **Move** (tiles ≤ Speed).
* **Attack** (melee or ranged).
* **Special Ability** (if available, cooldown/limited).
* **Defend** (+DEF until next turn).
* **Wait** (delay turn, reinsert later in bar).

---

## 6.8 Hero in Combat

* Appears on the initiative bar.
* Cannot move/attack directly.
* Can **cast spells** from known schools (cost Mana).
* Grants passive bonuses (stats ATK, DEF, etc. applied to units).

---

## 6.9 Victory Conditions

* **Victory:** all enemy units destroyed.
* **Defeat:** all allied units destroyed.
* MVP only → binary outcomes.
* Post-MVP → scenario-based win conditions (survive X turns, capture position).

---

## 6.10 MVP Summary

* **1 faction (Knights)** with 7 units, each in 3 expertise levels.
* Combat on **10×12 grid** with initiative system.
* Each unit has **1 special ability**.
* Hero acts via **spells only**.
* Victory = eliminate opponent army.

# 📖 Chapter 7 – Cities & Defenses

## 7.1 Overview

Cities are the **economic and military backbone** of _Seven Crowns_.\
They evolve from **Village → City → Fortress → Capital**, unlocking new buildings, units, and defenses.\
Cities generate resources, recruit heroes, train units, and serve as strategic victory points.

---

## 7.2 City Progression (MVP)

| Stage | Income | Units Available | Defenses |
|-------|--------|-----------------|----------|
| **Village** 🌱 | +100 Gold/day | T1 | None |
| **City** 🏙️ | +250 Gold/day | T1–T3 | Basic walls |
| **Fortress** 🏰 | +500 Gold/day | T1–T5 | Reinforced walls, 2 towers, moat |
| **Capital** 👑 | +1000 Gold/day | T1–T7 | Elite walls, 4 towers, wide moat, magic gate |

---

## 7.3 Economic Buildings

* **Farm (I → II → III)** → +20/40/80 Population per week.
* **Market** → trade resources, +100 Gold/day.
* **Forge** → +1 Iron/day.
* **Coal Mine** (urban) → +1 Coal/day.
* **Bank** → placeholder for post-MVP (interest system).
* **Royal Treasury (Capital only)** → +500 Gold/day.

---

## 7.4 Military Buildings

* **Barracks (T1)** → Peasants.
* **Archery Range (T2)** → Archers.
* **Spearman Tower (T3)** → Spearmen.
* **Monastery (T4)** → Monks.
* **Stables (T5)** → Knights.
* **Paladin Temple (T6)** → Paladins.
* **Celestial Sanctuary (T7)** → Angels.

Each unit building has **3 expertise levels**:

* Level I → Basic recruits.
* Level II → Improved recruits.
* Level III → Elite recruits.

---

## 7.5 Magical Buildings

* **Chapel (City)** → unlocks White Magic basics (Heal).
* **Library (Fortress)** → increases max Mana for faction heroes.
* **Cathedral (Capital)** → advanced White Magic spells, hero resurrection.

---

## 7.6 Defensive Structures

* **Walls** (City) → block attackers.
* **Reinforced Walls** (Fortress) → 2× HP, +25% defense.
* **Elite Walls** (Capital) → 3× HP, partial resistance to weak spells.
* **Towers** → 2 (Fortress) / 4 (Capital), auto-attack enemies each round.
* **Moat** → slows and damages enemies crossing.
* **Magic Gate** (Capital) → reduces physical damage to main gate by 50%.

---

## 7.7 Hero Recruitment

* **Tavern** available at City stage.
* Allows recruitment of **1 hero per day** (cost in Gold).
* New heroes start at Level 1 with simple equipment/talents.

---

## 7.8 MVP Summary

* **4 stages** of cities, each adding income, units, and defenses.
* **Farms** control recruitment via population system.
* Buildings cover **economy, military, magic, defenses**.
* Cities are **victory-critical strongholds**, scaling from fragile villages to near-impenetrable capitals.

# 📖 Chapter 8 – Quests & Objectives

## 8.1 Overview

Quests and objectives provide **purpose and direction** for the player.\
They are divided into:

* **Primary Quests** → define victory conditions.
* **Secondary Quests** → optional, rewarding exploration and risk-taking.
* **Dynamic Quests (post-MVP)** → randomized tasks for replayability.

---

## 8.2 Primary Objectives (MVP)

* **Victory:**
  * Capture the enemy **Capital**,
  * or obtain a **Legendary Artifact** (scenario-based).
* **Defeat:**
  * Lose all cities,
  * or lose the main hero (campaign only).

---

## 8.3 Secondary Objectives (MVP)

* Encourage exploration and interaction with **special map locations**.
* Examples:
  1. **Mine Capture** → “Secure the Diamond Mine.”
     * Reward: +5 Diamonds + XP.
  2. **Artifact Hunt** → “Recover the Ring of Speed from the cave.”
     * Reward: Artifact equipped to hero.
  3. **Resource Delivery** → “Bring 500 Wood to the ruined fort.”
     * Reward: Gold or recruits.
  4. **Neutral Army Defeat** → “Eliminate the bandits near the road.”
     * Reward: XP + Ether.

---

## 8.4 Quest Sources

* **Special Buildings** (Tavern, Temple, Mage Tower).
* **POIs** on map (Ruins, Grottos, Camps).
* **Scripted Events** (campaign).

---

## 8.5 Rewards

* **Resources** (Gold, rare resources).
* **Artifacts** (equippable items).
* **Units** (free reinforcements).
* **Experience** (hero XP or Magic mastery).

---

## 8.6 Tooltips & Information

* Hovering an enemy army shows **difficulty rating** (Easy, Moderate, Hard, Impossible).
* Some quests show **estimated rewards** to aid decision-making.

---

## 8.7 MVP Summary

* **1 primary quest** per map (e.g. capture Capital).
* **2–3 secondary quests** to guide exploration.
* Rewards include resources, artifacts, units, XP.
* Clear tooltip feedback on **enemy difficulty**.

# 📖 Chapter 9 – Meta-progression

## 9.1 Overview

Meta-progression links the **Free Mode (rogue-lite runs)** with the **Campaign**.

* In Free Mode, each playthrough is a **self-contained run** where the hero explores, builds, and fights.
* Regardless of success or failure, the player gains **persistent rewards**.
* These persistent unlocks are **required to complete the Campaign**, ensuring long-term motivation.

---

## 9.2 Persistent Resources

* **Ether** ✨
  * The main currency of meta-progression.
  * Earned by exploring, defeating armies, completing quests.
  * Spent between runs to unlock **spells, artifacts, and factions**.
* **Artifacts** 💍
  * Artifacts discovered during a run are added to the **global pool** once unlocked.
  * They may then appear in future runs or be equipped in Campaign.
* **Spells** 📖
  * Spells unlocked with Ether remain permanently available for all heroes.
  * Higher-tier spells require more Ether and/or mastery milestones.
* **Factions** 🛡️
  * Unlocked by major milestones (e.g. spend 100 Ether, complete specific quests).
  * Each new faction adds its own units, heroes, and playstyle.

---

## 9.3 Progression Flow

1. **Start a Free Mode run** with unlocked factions/spells/artifacts.
2. **Explore & fight**, earning Ether + discovering artifacts.
3. **End of run** (victory or defeat): rewards converted into Ether + new items added to global pool.
4. **Between runs**: player spends Ether to unlock permanent content.
5. **Campaign access** expands as stronger content is unlocked.

---

## 9.4 Campaign Integration

* Campaign chapters are **gated** by meta-progression.
  * Example: Chapter 2 requires unlocking at least 1 rare artifact.
  * Chapter 3 requires unlocking a second faction.
  * Final chapter requires unlocking ultimate spells or units.
* This creates a **hybrid design**: Campaign provides narrative, Free Mode provides progression.

---

## 9.5 MVP Scope

* **Ether** fully functional as persistent currency.
* Unlock pool includes:
  * 5–10 Artifacts.
  * 6–10 Spells.
  * 1 additional Faction (unlocked via Ether).
* Campaign: **1 playable chapter** gated behind unlocking 1 rare artifact.

---

## 9.6 Post-MVP Extensions

* More unlockables: hero traits, buildings, permanent economy bonuses.
* Dynamic modifiers: “New Game+” difficulty scaling in Free Mode.
* Achievements linked to meta-progression.

---

## 9.7 MVP Summary

* Free Mode = farming loop for Ether.
* Ether unlocks **artifacts, spells, factions** permanently.
* Campaign = locked behind key unlocks, making it the **ultimate reward**.
* Ensures replayability and structured long-term progression.

# 📖 Chapter 10 – Interface & UX

## 10.1 Overview

The user interface must provide **clarity, accessibility, and feedback** for exploration, combat, and management.\
The MVP UI will prioritize **readability** and **strategic transparency**, ensuring that the player always understands:

* what actions are available,
* what resources they have,
* what the risks/rewards are.

---

## 10.2 World Map UI

* **Top Bar:**
  * Displays resources (Gold, Wood, Iron, Coal, Diamonds, Ether, Sulfur).
  * Displays population available (per week).
* **Hero Panel (side):**
  * Portrait + current MP.
  * Quick access to Hero Screen (stats, talents, artifacts, spells).
* **Mini-map:**
  * Always visible in bottom-right corner.
  * Shows explored/unexplored areas, enemy armies, owned towns/mines.
  * Click-to-navigate functionality.
* **Tooltips:**
  * Hovering over armies → difficulty estimate (Easy → Impossible).
  * Hovering over resources/buildings → production rates, owner.

---

## 10.3 Hero Screen

* **Stats Panel:** ATK, DEF, MAG, KNW.
* **Talent Tree:** 3 branches × 5 nodes (MVP).
* **Secondary Skills:** slots with level (Basic/Advanced/Expert).
* **Artifacts:** 7 equipment slots (head, body, weapon, boots, 2 rings, amulet).
* **Spellbook:** categorized by magic school, showing known/unlocked spells.

---

## 10.4 City Screen

* **Main view:** illustration of the city evolving (Village → Capital).
* **Buildings menu:** icons for economic, military, magic, defense structures.
* **Recruitment panel:** available units (with population cost).
* **Hero recruitment:** Tavern interface, list of available heroes.
* **Farms:** show weekly population generation.

---

## 10.5 Combat Screen

* **Battlefield Grid (10×12):** visible tiles with obstacles/elevations.
* **Initiative Bar:** dynamic, showing upcoming unit turns.
* **Unit Stack Panels:** for both sides, showing HP, count, morale.
* **Action Buttons:** Move, Attack, Defend, Wait, Special Ability.
* **Hero Spellbook Panel:** compact list of spells with Mana costs.
* **Combat Tooltips:** hover → show expected damage, chance to hit, spell effect preview.

---

## 10.6 Quest Log UI

* **Primary Objectives:** clearly displayed at top.
* **Secondary Quests:** listed with progress tracker.
* **Rewards:** visible upfront (resources, artifacts, XP).
* Accessible via a **tab in the main UI**.

---

## 10.7 Feedback & Accessibility

* **Colors & Icons:** consistent resource icons, color-coded difficulty.
* **Animations:** feedback for attacks, spell casts, resource collection.
* **Audio cues:** confirmation sounds for actions (build, recruit, cast).
* **Error prevention:** actions requiring unavailable resources are grayed out with tooltip explanation.

---

## 10.8 MVP Summary

* **World Map UI:** top bar, hero panel, mini-map, tooltips.
* **Hero Screen:** stats, talents, artifacts, spellbook.
* **City Screen:** construction, recruitment, hero hiring.
* **Combat Screen:** battlefield + initiative bar + action/spell panels.
* **Quest Log:** tracks objectives and rewards.
* **Tooltips & minimap** ensure clarity for new players.

# 📖 Chapter 11 – AI (Map & Combat)

## 11.1 Overview

The AI controls **enemy factions** and **neutral armies**.\
Its role in the MVP is to:

* Explore the map,
* Capture and defend resources,
* Recruit armies and heroes,
* Fight the player with basic tactical logic.

The AI does **not** need to be optimal at MVP stage, but must behave **coherently and predictably**.

---

## 11.2 AI on the Strategic Map

### Goals

* Expand territory (capture nearby mines/farms).
* Recruit units in its city.
* Defend its city and Capital.
* Attack the player when conditions are favorable.

### Behaviors (MVP)

1. **Exploration:**
   * Send hero to nearest unexplored tile, prioritizing resource-rich areas.
2. **Resource Capture:**
   * Prioritize **mines and farms** within a radius of its city.
3. **Recruitment:**
   * Spend Gold + Population to maintain a growing army.
4. **Attack/Defense:**
   * Attack the player’s hero if AI army strength ≥ 120% of the player’s.
   * Retreat/defend city otherwise.

---

## 11.3 AI in Combat

### Goals

* Survive, deal damage, and exploit obvious advantages.

### Behaviors (MVP)

* **Target selection:**
  * Attack the **weakest or nearest enemy stack**.
  * Prioritize **ranged units** if accessible.
* **Movement:**
  * Move toward closest reachable target.
  * Avoid wasting turns against walls if alternative targets exist.
* **Abilities:**
  * Use special ability when conditions are met (e.g., Archer → _Precise Shot_ if enemy \> range penalty).
* **Hero casting (enemy AI hero):**
  * Cast random available spell weighted by usefulness (damage \> buffs \> debuffs).

---

## 11.4 Neutral Armies

* **Stationary guardians**: protect mines, artifacts, or special sites.
* Behavior: defend location, fight to the death.
* Tooltip shows **relative difficulty** before engagement.

---

## 11.5 MVP Scope

* 1 enemy faction (Knights, same as player).
* 1 enemy hero with city + army.
* Neutral armies defending resources.
* AI behaviors:
  * Explore, capture, recruit, defend, attack (map).
  * Move, attack, cast, use abilities (combat).

---

## 11.6 Post-MVP Extensions

* Smarter target evaluation (synergies, counter-units).
* Multi-hero AI with coordination.
* Diplomacy (alliances, truces).
* Strategic feints and multi-front tactics.

---

## 11.7 MVP Summary

* **Map AI:** simple exploration + resource capture + basic offense/defense logic.
* **Combat AI:** attacks nearest/weakest, uses simple spell heuristics.
* **Neutral armies:** stationary defenders of key sites.
* Enough to make the demo feel alive and challenging without requiring deep AI sophistication.

# 📖 Chapter 12 – Demo Content (MVP Scenario)

## 12.1 Overview

The MVP demo must showcase **all core systems** in a compact, replayable scenario.\
It will feature:

* 1 playable faction (Knights),
* 1 enemy AI faction (also Knights, simplified),
* 1 medium-sized map (surface + underground),
* A mix of exploration, economy, combat, city development, and quests.

---

## 12.2 Faction Setup

* **Player Faction:** Knights
  * Starts with 1 Village, 1 hero (Level 1).
  * Access to T1 units (Peasants).
  * Must grow city to unlock higher-tier units.
* **Enemy Faction (AI):** Knights
  * Starts with 1 Fortress (stronger than player’s Village).
  * 1 enemy hero (Level 3) patrolling near city.
  * More initial units, but slower growth to balance difficulty.

---

## 12.3 Map Design

* **Surface Layer:**
  * Player city in the west, enemy city in the east.
  * Mines: Gold, Wood, Iron, Coal.
  * 2 Farms capturable.
  * Several neutral armies guarding resources.
  * 2–3 Points of Interest (Tree of Wisdom, Menhir, Obelisk).
* **Underground Layer:**
  * Diamond Mine, Ether Pool, Sulfur Pit.
  * 1 Ruin (combat reward: Artifact).
  * Stronger neutral armies.
  * Hidden passage linking near player city → behind enemy lines.

---

## 12.4 Objectives

* **Primary Victory:** Capture the enemy **Fortress** (AI city).
* **Secondary Quests:**
  1. Secure the **Diamond Mine** (underground).
  2. Recover the **Amulet of Mana** from a guarded ruin.
* **Defeat Conditions:**
  * Lose all cities, or
  * Lose main hero.

---

## 12.5 Progression & Duration

* Designed for \~30–60 min playthrough.
* Player progression during demo:
  * Level 1 → Level 6–8 hero (access to 5–7 talent nodes).
  * Unlock up to Tier 5 units (Knights).
  * Cast 4–6 different spells depending on school usage.

---

## 12.6 Showcase Features

* **Exploration:** Surface + underground with fog of war, tooltips, mini-map.
* **Economy:** Resource gathering, farm-based population system.
* **Hero Progression:** Stats, talents, secondary skills, artifacts.
* **Combat:** 10×12 battlefield, initiative bar, unit special abilities, hero spells.
* **Cities:** Evolving from Village → City, buildings, recruitment, defense.
* **Quests:** 1 primary + 2 secondary.
* **AI:** Basic exploration, resource capture, defense, combat.
* **Meta-progression:** Reward at end of demo (Ether to unlock 1 artifact globally).

---

## 12.7 MVP Deliverable

* **1 self-contained map** with all MVP systems integrated.
* **Playable to victory or defeat.**
* **Repeatable** for testing progression, balance, and meta-progression loop.

# 📖 Annex – Post-MVP Evolutions

## A. Exploration & Map

* Additional terrain types: Swamp, Desert, Snow, Shallow Water, Lava.
* Naval gameplay: boats, ports, naval combat.
* Destructible terrain (blocked tunnels, bridges, magical barriers).
* Dynamic events: storms, plagues, monster migrations.
* Procedural map generation for Free Mode.

---

## B. Economy & Resources

* **Banking system:** invest Gold for weekly interest.
* **Population depth:** famine, festivals, migration events.
* Advanced trade mechanics between factions.
* Rare super-resources: e.g. Crystal, Obsidian.

---

## C. Heroes & Progression

* Expanded stats: Magic Defense, Engineering.
* Talent tree expansion: 15 nodes per branch (45 total).
* Dual specializations: branching hero builds.
* Unique hero classes per faction.
* Permanent traits gained through quests (hero “evolves” over campaign).

---

## D. Magic & Spells

* Full spell lists: 5–7 per school.
* Combo spells: mixing two schools (e.g. Air + Water = Storm).
* Rare Universal spells: global enchantments, time control.
* Spell research in magical buildings.
* Legendary “Crown Spells” tied to the Seven Crowns.

---

## E. Units & Combat

* Additional factions’ full rosters.
* Multi-ability units and branching evolutions (e.g. Monk → Inquisitor or High Priest).
* Morale & Luck systems influencing combat flow.
* Advanced siege warfare: catapults, rams, siege towers.
* Battlefield variety: bridges, choke points, destructible obstacles.

---

## F. Cities & Defenses

* Specialized building paths (economic vs military focus).
* Multi-city economy bonuses (empire management).
* Wonders per faction (unique global effect).
* Expanded defenses: fire towers, magical barriers, traps.
* City enchantments (blessings, curses).

---

## G. Quests & Objectives

* Dynamic quests generated per playthrough.
* Multi-step quest chains (fetch, escort, destroy).
* Faction-specific storylines.
* Alternative victory conditions: economic dominance, magical supremacy.
* Heroic endings tied to individual heroes.

---

## H. Meta-progression

* Unlock trees for:
  * Permanent economy upgrades.
  * Global hero traits.
  * Special artifacts and spells.
* “New Game+” with difficulty modifiers.
* Player hub between runs (Hall of Crowns).
* Achievements and milestone rewards.

---

## I. Multiplayer (future scope)

* Hotseat local multiplayer.
* Online asynchronous multiplayer (turn by turn).
* Competitive ladder with balanced maps.
* Co-op campaign mode.