# Horse Runner browser port and visual overhaul plan

## Product direction

Port Horse Runner from a MonoGame desktop executable to an install-free browser game while preserving its approachable one-button runner gameplay. The browser edition should feel like a polished mid-1990s pixel-art game: readable silhouettes, a restrained palette, layered scenery, expressive animation, and crisp pixels at every screen size.

The port will be delivered beside the desktop version at first. Shared rules can be extracted later once browser controls, timing, and collision behaviour have been validated against the original.

## Technical architecture

- Add a solution filter-style `.slnx` containing a new ASP.NET Core web project. Keep the existing solution available during migration.
- Use an ASP.NET Core host for straightforward local development, deployment, cache headers, and future score/profile APIs.
- Implement the game client with a native HTML canvas and an ES module. Canvas is a better fit than a DOM-heavy UI for the fixed-step game loop, sprite layering, particles, and pixel-perfect scaling.
- Keep simulation and rendering separate. Run physics on a fixed timestep, interpolate only visual effects, and scale a logical 320 × 180 scene to the available canvas with nearest-neighbour rendering.
- Store level definitions and asset metadata as data rather than hard-coding them into drawing code. This will make future level authoring and balancing safer.
- Support keyboard, pointer/touch, and reduced-motion preferences from the first browser milestone.

## Delivery phases

### Phase 1 — browser-playable vertical slice

- Scaffold the `.slnx` and an ASP.NET Core web app with no client package-manager dependency.
- Build a responsive game shell with a canvas, title screen, HUD, pause/restart controls, keyboard controls, and touch controls.
- Port the core run loop: acceleration, jump physics, collision, lives, score, procedural obstacle placement, game-over, and restart.
- Establish a 90s-inspired visual foundation using code-drawn pixel art: limited colour palette, dithered gradients, parallax hills and forest layers, chunky UI panels, contact shadows, dust, and screen transitions.
- Add accessibility basics: visible focus, semantic controls, an instructions panel, mute toggle, reduced motion support, and status text that is available outside the canvas.
- Add smoke tests for the HTTP surface and focused JavaScript tests for pure gameplay helpers.

### Phase 2 — gameplay parity

- Port horse/rider selection, all three environments (forest, arena, meadow), level transitions, apples, troll encounter, day/night progression, medals, and win flow.
- Move constants and level layouts into versioned JSON data and add validation at startup.
- Match the desktop game with fixed-timestep replay fixtures that verify jump arcs, obstacle timing, collision outcomes, scoring, and level duration.
- Persist settings and best scores in browser storage; never make local storage a requirement to play.

### Phase 3 — production pixel-art asset overhaul

- Create a visual bible before replacing assets: 320 × 180 logical resolution, 1-pixel base grid, hard-edged scaling, a shared 24–32 colour master palette plus small environment ramps, and consistent top-left lighting.
- Redesign the horse and rider around clear anatomy and silhouettes. Target an 8-frame gallop, 4-frame take-off/landing, 4-frame fall/recovery, idle breathing, mane/tail secondary motion, and separate palette-swappable coat and jacket layers.
- Repaint each biome as three to five parallax layers (sky, far silhouettes, midground landmarks, playfield, foreground occluders) and add bespoke props instead of stretched backgrounds.
- Give obstacles distinct anticipation silhouettes and collision-safe negative space. Add impact frames, dust clumps, leaf bursts, rail fragments, sparkles, and landing compression.
- Replace placeholder UI with a cohesive pixel font, nine-slice panels, icon states, selection portraits, level title cards, and compact mobile variants.
- Export lossless PNG sprite sheets plus machine-readable atlases. Keep source files in a dedicated art-source directory and document palette, frame size, pivot, hitbox, naming, and export rules.
- Review every asset at native resolution and at 2×–6× integer scales. Avoid filters, sub-pixel sprite positions, mixed pixel densities, and generated imagery that has not been manually cleaned to the grid.

### Phase 4 — audio and game feel

- Add short layered effects for hoof cadence, jump, landing, collision, pickups, UI actions, and ambience, with independent music/effects controls.
- Compose or license looping tracker/chiptune-inspired music without copying identifiable 1990s games. Record provenance and licences beside every external asset.
- Add conservative camera shake, hit-stop, palette flashes, and controller vibration where supported; disable or reduce them when reduced motion is requested.

### Phase 5 — quality, performance, and release

- Add Playwright journeys for start, jump, pause, touch input, game-over, and restart across desktop and mobile viewports.
- Set budgets for first-load transfer size, frame time, texture memory, and input latency. Use sprite atlases, preload only the first scene, and lazy-load later biomes/audio.
- Test current Chromium, Firefox, and Safari plus keyboard-only, touch-only, high-DPI, narrow landscape, audio-blocked, offline/error, and reduced-motion scenarios.
- Add an installable PWA only after caching/version invalidation is tested; add optional server-backed leaderboards only with abuse prevention and privacy requirements defined.
- Deploy first to a preview environment, conduct playtests focused on jump readability and obstacle fairness, then tune using observed failures rather than visual preference alone.

## Asset production workflow

1. Produce silhouette sheets and one representative forest scene; approve scale and palette before animating the full cast.
2. Draw key poses first, validate anatomy and collision readability in the running game, then add in-betweens.
3. Export atlas images and metadata deterministically. Preserve pivots so coats, rider layers, shadows, and effects cannot drift.
4. Review assets for originality and document whether each is original, commissioned, or licensed. Do not trace or imitate a specific commercial game's characters or scenery.
5. Replace vertical-slice procedural art one category at a time, retaining fallbacks until the new atlas has passed loading and visual regression tests.

## Definition of done

- The game starts from a current .NET SDK with one documented command and is playable without installing JavaScript tooling.
- A complete run can be played with keyboard and touch at a stable 60 fps on representative desktop and mobile hardware.
- Resize, pause/resume, lost focus, high-DPI screens, and reduced motion do not break simulation or controls.
- Production assets follow the visual bible, have recorded provenance, render with crisp integer scaling, and have no obvious seams or layer drift.
- Automated checks cover server startup, static asset delivery, core simulation helpers, and primary browser journeys.
- Deployment, controls, asset authoring, and rollback procedures are documented.

## Immediate implementation scope

The first implementation following this plan will create the `.slnx`, web host, responsive shell, and a playable canvas vertical slice. It will deliberately use original code-drawn pixel graphics so the browser architecture and game feel can be tested immediately without treating temporary art as final production assets.
