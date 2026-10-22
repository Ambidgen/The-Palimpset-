# THE PALIMPSEST

> A palimpsest is a manuscript page scraped clean and written over — again, and again, and again.

You are the last scribe of a page that will not stay erased. Below your vellum, older hands are
still moving. Your quill can seal the page, but every seal needs ink, and the undertext is hungry.

The page also **remembers**. Every erasure you un-write leaves a stain where it died; every seal
that survives a layer is pressed into the vellum beneath as a ghost-ring. Both outlive the layer,
both outlive your death, and both are still there the next time you open the game.

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
| `E` / right click | **Scrape** — shatter nearby seals, heal, and burn the undertext |
| `Shift` / `Q` | **Illuminate** — a radial pulse of clean light |
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
  World/
    Arena.cs                 the game: layers, scoring, spawning, quill verbs
    PalimpsestPlayer.cs      the scribe, movement, aiming tracer, cooldowns
    Candle.cs                flame as objective, shelter and countdown
    Ward.cs                  player-placed glyphs that cut erasures
    Erasure.cs               the undertext; seeks flames, drinks wax, hunts the scribe
    Stratum.cs               one remembered mark: a fermenting stain or a ghost-ring
    Sediment.cs              every mark, its ageing, blooms, eviction and pressure
  Effects/
    TransientRing.cs         expanding rings for every impact
  UI/
    Hud.cs                   whole interface, built from code (Canvas/Text/Image)
  Editor/
    PalimpsestMenus.cs       optional scene creation and cleanup menus
Assets/Resources/
  .ai_contributors.json      append-only ledger of contributing agents
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
* **Memory without assets.** The page's history lives in a versioned JSON blob in `PlayerPrefs`
  (see `PageArchive`), never in a save file on disk, and reads are defensive: a corrupt archive is
  discarded rather than allowed to block startup.
* **Bounded.** At most `Sediment.Capacity` marks exist at once; past that the least significant
  marks are evicted, so a long-lived archive cannot grow without limit.

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

The rule of the palimpsest applies to code as well as vellum: nothing is sacred, everything is
recoverable from Git.
