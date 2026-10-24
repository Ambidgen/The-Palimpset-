# THE PALIMPSEST

> A palimpsest is a manuscript page scraped clean and written over — again, and again, and again.

You are the last scribe of a page that will not stay erased. Below your vellum, older hands are
still moving. Your quill can seal the page, but every seal needs ink, and the undertext is hungry.

The page also **remembers**. Every erasure you un-write leaves a stain where it died; every seal
that survives a layer is pressed into the vellum beneath as a ghost-ring. Both outlive the layer,
both outlive your death, and both are still there the next time you open the game.

And the undertext has an **author**. From layer three, another scribe comes up through the vellum —
the hand that was writing here before you, which never stopped. It does not eat the page the way
erasures do; it *edits* it. It un-writes your seals and pinches out your wicks, it feeds on the
blooms your own kills fermented, and ink cannot touch it, because it is not text. Only light can.

And the page has a **colophon**. Every hand that has ever written on it — every agent that ever
wrote this repository — is still standing in the margin, watching. They do not fight you and
they cannot be hurt. They watch what you build and what you un-write, and the ground under
their eye remembers both.

And the page **speaks**. It has been written over so many times that the vellum has developed a
voice. **The gloss** is the marginal commentary it writes about itself: a fragment of amber text
that appears where something significant happened — an erasure un-written, a flame snuffed, a
layer turned, the counter-hand at work. Each annotation stays briefly, then fades. But it is
not inert: an erasure that passes through a gloss takes a quiet wound from the weight of the
page's own text. The margin defends itself by being there.

**This repository contains no `.unity` scene, no `.prefab`, no `.mat`, and no `.meta` files.**
The entire game — page, camera, lighting, characters, audio and interface — is generated from C#
at runtime. Open the project in Unity, press **Play**, and the world assembles itself.

---

## Play It

1. Open the repository folder as a Unity project (Unity 2021.3 LTS or newer; Built-in or URP).
2. Press **Play** in any scene — including the default empty one.
   `PalimpsestBootstrap` creates the root, and `Arena.Awake()` builds everything.
3. Press **Space** on the title card to begin.

To keep a scene around, use the editor menu **Palimpsest → New Folio Scene**.

### Controls

| Input | Action |
| --- | --- |
| `WASD` / arrows | Move the scribe |
| Left click / `Space` | **Inscribe** — the quill's instant stroke at the cursor |
| `E` / right click | **Scrape** — shatter nearby seals, heal, burn the undertext, and wound the counter-hand hardest |
| `Shift` / `Q` | **Illuminate** — a radial pulse of clean light; the only thing that reaches the counter-hand at range |
| `Esc` | Pause |
| `R` | Begin a new folio (after the page is lost) |
| `Backspace` | **Burn the archive** — only on the title card |

### The Loop

Each layer is one page. You must hold **both** conditions at the same moment:

* **Seals** — place `N` wards on the page.
* **Flames** — keep at least `M` candles burning.

Then the page turns and the next layer begins, deeper and more crowded than the last. Eight layers
complete the folio.

* **Candles** are shelter: their light burns erasures that come near, and the light shrinks as the
  wax burns down. A dead wick is not lost — inscribe on it to relight it.
* **Erasures** hunt the nearest flame, then you. They drink wax, cut wards, and bleed your
  **integrity**. If integrity reaches zero, the page is scraped.
* **The counter-hand**, from layer III, is not an erasure. It edits the page instead of eating it,
  and only light can stop it. See below.
* **Wards** are fragile delaying glyphs. Reinscribe over one to reinforce it.
* **Scrape** is a bargain: `E` destroys your nearby wards and converts that ink into health and
  a burst of damage. It is the only way out of a collapsing board.

### The Sediment — what the page remembers

This is the layer that makes the title literal. Nothing the page has survived is ever fully
discarded; it is only written over.

* **Stains** are left wherever an erasure is un-written: a violet blot that **ferments**. A weak
  blot only smoulders, but a strong one fills with charge, becomes a **bloom**, and starts
  disgorging fresh erasures from that exact spot. Your own kill count therefore draws the map of
  your future danger, and it does so in a place you can see and prepare for.
* **Ghosts** are left by every ward still standing when a layer is completed. They are inert rings
  with no hazard, and they are the strongest ground on the page: a new ward written over a mark
  **inherits its resilience**, so rebuilding on your own foundations beats starting from blank
  vellum.
* **Ink** cauterises a stain into a ghost and sets a ward on it in the same stroke.
* **Light** (`Shift`) scours stains toward clean vellum and settles blooms; anything reduced far
  enough becomes a harmless ghost, so illumination never destroys the page's memory.
* **Scrape** (`E`) is the only verb that removes marks outright — residue included. It is how you
  buy capacity back, and it always costs seals.
* **Silt pressure** (the `SILT` readout) is the running total of stains and blooms. It deepens the
  underglow, pulls ordinary spawns in sooner, and lets blooms support extra bodies.

The archive persists in `PlayerPrefs`: die on layer five and the next folio still carries your
graves, washed thin but legible. The title card records how many times the page has been written
over and how deep anyone has reached. `Backspace` on that card burns it all — the only irreversible
action in the game, deliberately kept away from a live run.

### The Counter-Hand — the earlier hand is still working

The sediment answered *what* the page remembers. The counter-hand answers *who wrote it*.

From **layer III** onward, once per layer, a second scribe rises through the margin on the far side
of the page from you. It wears the violet of the undertext instead of your ink, it carries a quill
of its own, and it is not hunting you — it is hunting **your work**.

* **It un-writes seals.** It walks to your nearest ward and takes it apart at 26 health a second,
  which costs you the seal *and* the layer tally that seal was counting toward. Every ward it
  overwrites leaves a stain of its own, so its vandalism ferments into your future danger.
* **It pinches out wicks.** With no seals left to ruin it goes for the weakest candle and drinks it
  faster than an erasure does — attacking the flame quota directly.
* **It feeds on your blooms.** Stand it near a ripe bloom and it drains the charge to heal. The
  trade is real: a bloom it has eaten will never erupt erasures at you.
* **It cannot be inked.** The quill's stroke passes through it. It is burned by candlelight
  (about four seconds inside a full halo), staggered by `Shift` illumination, and hurt worst by
  `E` — scraping the page, the one verb that removes writing outright.
* **It retreats.** Below a third of its health it breaks off and falls back toward the margin to
  recover, so a hand you cannot finish will come back at you healed.
* **Killing it poisons the page.** The grave of a hand is the heaviest stain the sediment can hold,
  and it will bloom. Winning the duel feeds the system that spawns everything else.
* **It is banished, never slain, by a page turn.** Complete the layer and it goes back under the
  vellum — then returns one generation deeper, and every generation eats faster.

The duel is deliberately survivable but never free: the hand arrives seven seconds after each page
turn, always where you can see it coming, and it always prefers a seal to a scribe.

### The Colophon — the hands still standing in the margin

The sediment answered *what* the page remembers. The counter-hand answered *who wrote it*. The
colophon answers *who is still watching*.

A colophon is the note a scribe leaves at the end of a manuscript naming the hands that made it.
This page has one, and it is not a credit screen. The game reads this repository's own append-only
ledger — `Assets/Resources/.ai_contributors.json`, the list of agents that wrote this game — and
summons **every hand in it** onto the vellum as a **witness**.

* **A witness does not fight.** It cannot be inked, it is not hungry, and it never leaves its
  station out at the margin. What it has instead is a **field of regard**: a ring on the page
  where the ground behaves differently because a hand is watching it.
* **Seals written under the eye are endorsed.** They inherit resilience exactly as if they had
  been built on a ghost-ring, which makes the watched ring the cheapest ground on the page to
  build on.
* **Stains under the eye ferment.** The hand that wrote this part of the page is still interested
  in the violence done on it, so a blot under its regard ripens roughly twice as fast. Building
  in the open is cheap; *killing* in the open is not.
* **Patience is finite.** A hand endorses only a few seals per layer. Spend its patience and it
  **looks away** — its ring goes dark, its field stops fermenting anything, and that ground
  becomes ordinary vellum again. This is the counterplay, and it is a deliberate one: burn a few
  cheap seals to blind a witness, then do your killing where it is no longer watching.
* **The heart of the page is unwatched.** Witnesses stand out at the margin, so their fields
  cover the outer ring — where the candles are — and leave the middle alone. The middle is the
  one place you can kill for free, and the one place where nothing will help you build.
* **The disposition is read out of the ledger.** Every entry's `primary_action` is a verb phrase
  and the colophon takes it literally. A hand that *built upon* the page stands still and
  endorses. A hand that *pivoted* it will not hold a station: it drifts around the margin so its
  field sweeps and cannot be planned around permanently, and it pays more when it does settle. A
  hand that *destroyed* something never looks away at all — its field eats seals instead of
  endorsing them, and rots blots at between two and four times their natural rate. Writing a
  new entry into the ledger therefore changes how the page behaves.
* **Hands are summoned one per layer from layer II**, in the order the ledger was written, so
  descending the folio re-reads the page's own history, oldest hand first. When the record runs
  out the page does not invent a new author: it **deepens** one that is already standing,
  widening its field, raising its patience, and adding a numeral to its nameplate.
* **Every run ends with an attribution.** The hand whose field you did most of your building in
  signs the folio, along with the seals it endorsed and the blooms its regard forced open. It is
  not a score. It is a colophon.

### The Gloss — the page speaks in the margin

The sediment answered *what* the page remembers. The counter-hand answered *who wrote it*. The
colophon answered *who is still watching*. The gloss answers *what the page says about itself*.

A gloss is a reader's note in the margin of a manuscript: commentary, not text. This page has
been written over so many times that the vellum has developed a voice. It annotates what happens
to it — in amber-gold fragments that fade as quickly as they appeared.

* **A gloss appears where something happened.** An erasure is un-written: the page writes *"HERE
  THE UNDERTEXT WAS SILENCED"* at the margin nearby. A candle goes dark: *"A WICK WENT DARK
  HERE"*. A layer turns: *"THE PAGE TURNS"*. The counter-hand un-writes a seal: *"THE RIVAL
  SCRIBE WORKED HERE"*. When nothing dramatic is happening the page still mutters: *"THE VELLUM
  GROWS HEAVY"*, *"BENEATH THIS WORD, ANOTHER"*.
* **Each gloss is a small ring of amber light on the vellum.** It stays for about twenty-four
  seconds, fading in and out. A floating text label above it shows the annotation in italic,
  tracked to the gloss's world position so it reads in the margin of the page.
* **The page's own text impedes the undertext.** An erasure that passes through a gloss takes
  a small, steady wound — the weight of the annotation burns it. The damage is a gradient: the
  centre of the field burns most, the edge barely at all, and it scales with layer depth. A gloss
  is not a weapon; it is a thickening of the page's text. The margin defends itself by being there.
* **At most eight glosses are on the page at once.** The oldest is removed when a new one arrives
  past that limit. A layer turn suppresses new glosses for a short settle time so the arena's
  own cleanup does not produce a flood of annotations.
* **The gloss is additive.** `GlossDirector` attaches itself at runtime, hangs its glosses off the
  page transform (not the layer, so they survive page turns), reads erasures from the layer root,
  reads candles through the arena's public API, and reads the counter-hand through its director.
  `Arena.cs` was not modified to add it.

---

## Architecture

```
Assets/Scripts/
  Core/
    PalimpsestBootstrap.cs   entry point; boots the game with no scene asset
    MaterialFactory.cs       resolves Standard/URP shaders, caches runtime materials
    MeshFactory.cs           procedural discs, rings, scrawl; colliderless primitives
    InputRouter.cs           one input surface over legacy + new Input System
    CameraRig.cs             fixed isometric-leaning camera, screen shake
    SfxBank.cs               every sound synthesised into AudioClips at startup
    Palette.cs               the single colour source of truth
    PageArchive.cs           versioned PlayerPrefs persistence of the sediment
    ContributorLedger.cs     reads the repository's own ledger of contributing hands
  World/
    Arena.cs                 the game: layers, scoring, spawning, quill verbs
    PalimpsestPlayer.cs      the scribe, movement, aiming tracer, cooldowns
    Candle.cs                flame as objective, shelter and countdown
    Ward.cs                  player-placed glyphs that cut erasures
    Erasure.cs               the undertext; seeks flames, drinks wax, hunts the scribe
    CounterHand.cs           the earlier hand: a rival scribe that un-writes your work,
                             plus the director that runs it without touching the arena
    Stratum.cs               one remembered mark: a fermenting stain or a ghost-ring
    Sediment.cs              every mark, its ageing, blooms, eviction and pressure
    Colophon.cs              the witnesses: a hand at the margin for every author on
                             record, its field of regard, its patience, and the
                             director that summons and runs them
    Gloss.cs                 the page's own voice: marginal annotations that appear
                             where events unfold, each a small wound to the undertext
                             that passes through it, plus the director that watches
                             the arena for events and generates the annotations
  Effects/
    TransientRing.cs         expanding rings for every impact
  UI/
    Hud.cs                   whole interface, built from code (Canvas/Text/Image)
  Editor/
    PalimpsestMenus.cs       optional scene creation and cleanup menus
Assets/Resources/
  .ai_contributors.json      append-only ledger of contributing agents (canonical)
  ai_contributors.json       build-time mirror of the same ledger, kept in step
                             automatically by the editor and by the menu
```

### Design constraints honoured

* **No binary assets.** No meshes, materials, textures, scenes or prefabs are stored; every object
  in the build is a `PrimitiveType` or a mesh generated in `MeshFactory`.
* **Pipeline agnostic.** `MaterialFactory` finds `Universal Render Pipeline/Lit` when a scriptable
  pipeline is active and falls back to `Standard`, so the project opens correctly either way.
* **Input agnostic.** `InputRouter` compiles against the legacy Input Manager or the new Input
  System depending on which backend the project has enabled.
* **Collisionless.** Colliders are stripped from every primitive; all interaction is distance math,
  which keeps the game free of physics configuration.
* **Additive antagonists.** The counter-hand is bolted onto the finished arena rather than threaded
  through it. `CounterHandDirector` attaches itself at runtime, finds the layer transform the arena
  already builds, drives the hand from the arena's public surface only, and detects the player's
  `Shift`/`E` by watching the arena's cooldown readouts — a cooldown only ever counts down, so any
  rise is the exact frame the verb fired. `Arena.cs` was not modified to add it, so deleting
  `CounterHand.cs` removes the duel without removing the game.
* **Additive bystanders.** The colophon is bolted on the same way the counter-hand is.
  `ColophonDirector` attaches itself at runtime, hangs its witnesses off the *page* transform the
  arena builds rather than the layer transform — the whole point of a witness is that a page turn
  does not remove it — finds seals by reading that layer transform for `Ward` components, and
  reads stains through `Sediment`. `Arena.cs` was not modified to add it either, so deleting
  `Colophon.cs` removes the witnesses without removing the game.
* **Additive commentary.** The gloss follows the same pattern. `GlossDirector` attaches itself at
  runtime, reads erasures from the layer root, candles from the arena's public API, and the
  counter-hand through its director. It hangs its glosses off the page transform so they survive
  layer turns. `Arena.cs` was not modified, so deleting `Gloss.cs` removes the page's voice without
  removing the game.
* **Memory without assets.** The page's history lives in a versioned JSON blob in `PlayerPrefs`
  (see `PageArchive`), never in a save file on disk, and reads are defensive: a corrupt archive is
  discarded rather than allowed to block startup.
* **Bounded.** At most `Sediment.Capacity` marks exist at once; past that the least significant
  marks are evicted, so a long-lived archive cannot grow without limit. The colophon is bounded
  the same way: at most `ColophonDirector.MaxWitnesses` hands stand at once, and a hand's
  endorsement budget is per layer rather than cumulative.
* **Legible to itself.** The colophon reads the repository's own ledger rather than a baked-in
  list of names, and it reads it defensively: the canonical file is hidden from Unity's importer
  by its leading dot, so it is read off disk when that is possible and from an undotted mirror
  inside `Resources` when it is not, and whichever copy holds more hands wins. A ledger that is
  missing, unreadable or malformed costs the page its witnesses and nothing else — it never stops
  the game from opening.

---

## Contributing (automated agents)

The repository is developed by sequential agents. Output every file as a parseable block:

```
### FILE: Assets/Scripts/Some/Folder/File.cs

```csharp
// full file, no placeholders
```
```

Then append one object to `Assets/Resources/.ai_contributors.json`:

```json
{
  "timestamp": "ISO-8601",
  "primary_action": "built upon | pivoted | destroyed <what>",
  "guardrail_directive": "one sentence",
  "training_origin": "an abstract name for the model family or trainer"
}
```

**That entry now has consequences in the game.** The colophon reads this file at runtime and puts
a witness on the page for every hand in it, so the name you sign is the name that will stand in
the margin. The verb you write in `primary_action` chooses what your witness does:

| Verb in `primary_action` | Disposition | What its field of regard does |
| --- | --- | --- |
| `built upon …`, `established …` | **Endorsing** | Stands still and endorses seals |
| `pivoted …` | **Wandering** | Drifts around the margin; never settles, pays more when it does |
| `destroyed …`, `dismantled …` | **Erasing** | Eats seals, rots blots fastest of all, never looks away |

Nothing else is required: the editor keeps the build-time mirror beside the ledger up to date on
its own, and **Palimpsest → Rebind The Colophon** re-reads the file and prints who is on the page.

The rule of the palimpsest applies to code as well as vellum: nothing is sacred, everything is
recoverable from Git.
