# Horse Runner - Sprite & Tiled Guide

## Sprites You Need to Create in Aseprite

### 1. Horse + Rider Running (sprite sheet)
- **File:** `Content/Sprites/horse_rider_run.png`
- **Frame size:** 96 x 64 pixels each
- **Frames:** 4-6 frames arranged horizontally in a single row
- **What to draw:**
  - A horse in side-view (facing right) with a rider on its back
  - Each frame shows a different leg position (galloping cycle)
  - The rider should be sitting upright, wearing a helmet or hat
  - Use a **transparent background** (magenta #FF00FF will also be keyed out)
- **Animation tip:** Study a horse gallop cycle - the 4 key poses are:
  1. Both front legs forward, back legs back (extended)
  2. Front legs down, back legs coming forward (gathered)
  3. Both front legs back, back legs forward (extended other way)
  4. Front legs coming forward, back legs down (gathered other way)

### 2. Horse + Rider Jumping
- **File:** `Content/Sprites/horse_rider_jump.png`
- **Size:** 96 x 64 pixels (single frame)
- **What to draw:**
  - Horse with legs tucked under the body (mid-jump pose)
  - Rider leaning forward slightly
  - Mane and tail flowing upward/backward
  - Transparent background

### 3. Obstacle - Log
- **File:** `Content/Sprites/obstacle_log.png`
- **Size:** 64 x 32 pixels
- **What to draw:**
  - A fallen tree log lying horizontally
  - Brown bark texture, visible tree rings on the cut end
  - Some moss or grass at the base

### 4. Obstacle - Rock
- **File:** `Content/Sprites/obstacle_rock.png`
- **Size:** 48 x 36 pixels
- **What to draw:**
  - A grey/brown boulder
  - Some highlights and shadows for a 3D look
  - Maybe a crack line across it

### 5. Obstacle - Bush
- **File:** `Content/Sprites/obstacle_bush.png`
- **Size:** 56 x 40 pixels
- **What to draw:**
  - A thorny/dense bush
  - Dark and light green layers for depth
  - Optional: small red berries
  - A short brown stem at the bottom

### 6. Apple (Reward)
- **File:** `Content/Sprites/apple.png`
- **Size:** 32 x 32 pixels
- **What to draw:**
  - A bright red/green apple
  - A small brown stem on top
  - A green leaf
  - A white highlight for shininess
  - Optional: a subtle golden glow around it

### 7. Forest Background
- **File:** `Content/Sprites/forest_bg.png`
- **Size:** 400 x 380 pixels (will tile horizontally)
- **What to draw:**
  - A forest scene that tiles seamlessly left-to-right
  - Distant mountains or hills at the top
  - Trees of varying sizes in the middle
  - The bottom should blend into the ground color
  - Use softer/muted colors (this scrolls slowly as parallax)
- **Tiling tip:** Copy the left ~50px, paste on the right, then blend to make it seamless

### 8. Ground Tile
- **File:** `Content/Sprites/ground.png`
- **Size:** 200 x 100 pixels (will tile horizontally)
- **What to draw:**
  - Grass on top (first ~12 pixels)
  - Dirt/earth below
  - Some small stones and roots for texture
  - Must tile seamlessly left-to-right

---

## Aseprite Tips

1. **Canvas setup:** Use the exact pixel sizes listed above
2. **Color mode:** RGBA (indexed is fine too, but RGBA is simpler)
3. **Export:** File > Export Sprite Sheet for the horse run animation
   - Layout: Horizontal strip
   - Check "Rows: 1"
4. **Transparent bg:** Make sure Layer 0 is transparent, don't draw on a colored background
5. **Color palette:** Stick to a consistent earth-tone palette:
   - Browns: `#8B5A2B`, `#654321`, `#3E2723`
   - Greens: `#228B22`, `#32CD32`, `#2D5A1E`
   - Greys: `#8C8C8C`, `#AAAAAA`, `#666666`
   - Skin: `#F0C8A0`, `#D4A574`
   - Sky: `#87CEEB`, `#ADD8E6`

---

## Using Tiled for Level Design

### Setup
1. Install Tiled from [mapeditor.org](https://www.mapeditor.org/)
2. Open `Content/Levels/forest_level.tmx`

### Map Structure
The map has these layers:
- **Background** (tile layer): Sky and distant scenery tiles
- **Obstacles** (object layer): Where you place obstacles and the apple
- **Decorations** (object layer): Trees, flowers, visual-only elements

### How to Edit Obstacles
1. Select the "Obstacles" layer in the Layers panel
2. Use the **Select Objects** tool (shortcut: S)
3. Drag obstacles left/right to change their position
4. To add a new obstacle:
   - Use Insert > Insert Object > Rectangle
   - Set its **Type** to one of: `obstacle_log`, `obstacle_rock`, `obstacle_bush`, `apple`
   - Position it at y=344-352 (on the ground line)

### Map Dimensions
- **200 tiles wide x 15 tiles tall** = 6400 x 480 pixels
- At scroll speed of 200 px/s, this gives ~32 seconds of unique ground
- The background tiles/parallax and looping extend this to fill the 60s game

### Creating a Better Tileset
1. In Aseprite, create a **256 x 256 pixel** image
2. Divide it into a grid of **32 x 32 tiles** (8 columns x 8 rows)
3. Paint tiles for: grass, dirt, stone, tree trunks, canopy, sky, flowers
4. Export as `Content/Tilesets/forest_tileset.png`
5. In Tiled: Map > Edit Tileset to update the reference

### Suggested Tileset Layout (32x32 tiles)
```
Row 0: [grass-TL] [grass-T] [grass-TR] [trunk] [dirt-L] [dirt-C] [dirt-R] [canopy]
Row 1: [stone-L]  [stone-C] [stone-R]  [roots] [flower] [mushrm] [bush]   [sky]
Row 2: [sky-cloud-L] [sky-cloud-C] [sky-cloud-R] [hill-L] [hill-C] [hill-R] [water] [bridge]
Row 3+: Additional decoration tiles as needed
```

### Workflow: Tiled -> Game
Currently the game generates obstacles procedurally in `GameLevel.cs`. To load from the Tiled map instead:
1. Add the `TiledCS` NuGet package (or parse the XML manually)
2. Read the object layer and create `Obstacle` instances from the object positions
3. This lets level designers tweak obstacle placement in Tiled without changing code

---

## Building and Running

```bash
# Prerequisites: .NET 8 SDK + MonoGame templates
dotnet new install MonoGame.Templates.CSharp

# Build and run
cd HorseRunner
dotnet restore
dotnet build
dotnet run
```

## Controls
- **Space** or **Up Arrow**: Jump
- **Escape**: Quit
- **Space/Enter**: Start game (on title screen)
- **R** or **Space/Enter**: Restart (on win/game over screen)
