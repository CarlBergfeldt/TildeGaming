#!/usr/bin/env python3
"""
Asset generator for Horse Runner.

Produces:
  1. A richer, more "realistic" forest background (varied tree types & heights,
     depth layers, hills, sky gradient) that tiles seamlessly.
  2. Recolorable horse layers split out of the existing combined horse+rider
     sprites, giving THREE selectable horse coats (Chestnut / Black / Snow) plus
     a separate greyscale rider-jacket layer that can be tinted to any rider
     colour at draw time (Yellow / Blue / White / Black).
  3. A standing, arms-raised rider (body + tintable jacket) used for the podium
     victory finish.

Run from anywhere:  python3 tools/generate_assets.py
"""

import os
import math
import random
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "HorseRunner", "Content", "Sprites")


# ---------------------------------------------------------------------------
# Shared "polish" helpers used to lift the flat sprites: soft volume shading
# plus a crisp darker rim so the silhouette reads well at the larger in-game
# size.
# ---------------------------------------------------------------------------
def add_form_shading(img, top_boost=24, bottom_drop=34):
    """Lighten the top of the silhouette and darken the bottom for volume."""
    px = img.load()
    W, H = img.size
    ymin, ymax = H, 0
    for y in range(H):
        for x in range(W):
            if px[x, y][3] > 20:
                ymin = min(ymin, y)
                ymax = max(ymax, y)
    if ymax <= ymin:
        return img
    span = float(ymax - ymin)
    for y in range(H):
        frac = (y - ymin) / span
        delta = int(top_boost * (1 - frac) - bottom_drop * frac)
        for x in range(W):
            r, g, b, a = px[x, y]
            if a > 20:
                px[x, y] = (max(0, min(255, r + delta)),
                            max(0, min(255, g + delta)),
                            max(0, min(255, b + delta)), a)
    return img


def add_outline(img, factor=0.5):
    """Darken edge pixels (pixels touching transparency) for a crisp rim."""
    src = img.copy()
    sp = src.load()
    dp = img.load()
    W, H = img.size
    for y in range(H):
        for x in range(W):
            r, g, b, a = sp[x, y]
            if a <= 20:
                continue
            edge = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= W or ny >= H or sp[nx, ny][3] <= 20:
                    edge = True
                    break
            if edge:
                dp[x, y] = (int(r * factor), int(g * factor), int(b * factor), a)
    return img


# ---------------------------------------------------------------------------
# 1. FOREST BACKGROUND  (400 x 380, tiles horizontally)
# ---------------------------------------------------------------------------
def gen_forest_bg():
    W, H = 400, 380
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rnd = random.Random(20240620)

    # --- Sky gradient: deeper blue up top, hazy pale near the horizon ---
    horizon = 250
    top = (120, 178, 224)
    bottom = (208, 226, 224)
    for y in range(0, horizon):
        t = y / horizon
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        d.line([(0, y), (W, y)], fill=c + (255,))

    # --- Distant hazy hills (two layers for depth) ---
    def hill_layer(base_y, amp, color, step):
        pts = [(0, H)]
        x = 0
        prev = base_y
        while x <= W:
            prev = base_y + int(math.sin(x * 0.012 + step) * amp
                                + math.sin(x * 0.05 + step * 2) * amp * 0.3)
            pts.append((x, prev))
            x += 8
        pts.append((W, H))
        d.polygon(pts, fill=color)

    hill_layer(170, 22, (150, 180, 175, 255), 0.0)   # far, hazy
    hill_layer(196, 18, (120, 160, 120, 255), 1.7)   # mid

    # --- Tree helpers -------------------------------------------------------
    def conifer(cx, base_y, height, width, fill, shade):
        """Layered triangular pine. Returns nothing, draws in place."""
        trunk_w = max(3, width // 8)
        d.rectangle([cx - trunk_w // 2, base_y - height * 0.18,
                     cx + trunk_w // 2, base_y], fill=(74, 52, 32, 255))
        tiers = 4
        for i in range(tiers):
            ty = base_y - height * 0.16 - (height * 0.82) * (i / tiers)
            tw = width * (1.0 - i * 0.18)
            th = height * 0.30
            # main tier
            d.polygon([(cx, ty - th), (cx - tw / 2, ty), (cx + tw / 2, ty)],
                      fill=fill)
            # lit left edge / shaded right edge for a bit of form
            d.polygon([(cx, ty - th), (cx - tw / 2, ty), (cx - tw / 6, ty)],
                      fill=shade)

    def deciduous(cx, base_y, height, width, fill, shade, hi):
        """Rounded broad-leaf tree built from overlapping blobs."""
        trunk_w = max(4, width // 7)
        d.rectangle([cx - trunk_w // 2, base_y - height * 0.32,
                     cx + trunk_w // 2, base_y], fill=(86, 60, 38, 255))
        cy = base_y - height * 0.62
        r = width * 0.5
        blobs = [(0, 0, r), (-r * 0.55, r * 0.18, r * 0.7),
                 (r * 0.55, r * 0.18, r * 0.7), (0, -r * 0.5, r * 0.78),
                 (-r * 0.35, -r * 0.2, r * 0.62), (r * 0.4, -r * 0.15, r * 0.6)]
        for bx, by, br in blobs:
            d.ellipse([cx + bx - br, cy + by - br, cx + bx + br, cy + by + br],
                      fill=fill)
        # shaded underside
        d.ellipse([cx - r * 0.4, cy + r * 0.1, cx + r * 0.9, cy + r * 0.9],
                  fill=shade)
        # sun highlight upper-left
        d.ellipse([cx - r * 0.8, cy - r * 0.7, cx - r * 0.1, cy],
                  fill=hi)

    # --- Tree band: draw from far (top/darker/smaller) to near (bigger) -----
    # Each entry: (relative depth) gives colour + size range.
    far_pines = [(70, 110, 230), (60, 95, 232), (52, 88, 234)]

    # Far row of small dark pines along the hill line
    fill_far = (44, 92, 58, 255)
    shade_far = (34, 74, 46, 255)
    x = -10
    while x < W + 20:
        h = rnd.randint(54, 78)
        w = rnd.randint(30, 44)
        conifer(x, 214 + rnd.randint(-4, 6), h, w, fill_far, shade_far)
        x += rnd.randint(26, 40)

    # Mid + near band: mix of taller pines, broad-leaf trees, varying heights.
    # Draw seamlessly: any tree near an edge is also drawn wrapped to the
    # opposite side so the 400px tile loops cleanly.
    near_specs = []
    x = 0
    while x < W:
        x += rnd.randint(30, 58)
        kind = rnd.choice(["pine", "pine", "broad", "broad", "tall_pine"])
        near_specs.append((x, kind, rnd.random()))

    def draw_near(cx, kind, seed):
        base_y = 250 + int(seed * 6)
        if kind == "tall_pine":
            h = rnd.randint(150, 196)
            w = rnd.randint(60, 78)
            conifer(cx, base_y, h, w, (40, 104, 60, 255), (30, 82, 48, 255))
        elif kind == "pine":
            h = rnd.randint(96, 140)
            w = rnd.randint(46, 64)
            conifer(cx, base_y, h, w, (48, 120, 66, 255), (36, 96, 52, 255))
        else:  # broad-leaf, varied greens
            h = rnd.randint(104, 150)
            w = rnd.randint(70, 104)
            greens = [((70, 150, 70), (44, 104, 50), (118, 188, 96)),
                      ((92, 158, 64), (62, 116, 44), (148, 198, 104)),
                      ((58, 138, 78), (40, 100, 56), (110, 184, 110))]
            f, s, hi = rnd.choice(greens)
            deciduous(cx, base_y, h, w, f + (255,), s + (255,), hi + (255,))

    for cx, kind, seed in near_specs:
        draw_near(cx, kind, seed)
        if cx < 60:           # wrap to right edge
            draw_near(cx + W, kind, seed)
        if cx > W - 60:       # wrap to left edge
            draw_near(cx - W, kind, seed)

    # --- Ground blend at the very bottom (trees stand on grass) -------------
    for y in range(250, H):
        t = (y - 250) / (H - 250)
        c = (int(60 - 16 * t), int(120 - 36 * t), int(58 - 18 * t))
        d.line([(0, y), (W, y)], fill=c + (255,))

    img.save(os.path.join(SPRITES, "forest_bg.png"))
    print("forest_bg.png written")


# ---------------------------------------------------------------------------
# 2. HORSE COAT LAYERS + TINTABLE RIDER JACKET
#    Split the existing combined horse+rider sprites by colour.
# ---------------------------------------------------------------------------
def is_jacket(r, g, b, a):
    # navy blue jacket / helmet
    return a > 10 and b >= r + 12 and b >= g + 8 and b >= 45


def is_brown(r, g, b, a):
    # any brown horse-coat / mane / hoof / boot tone (NOT cream pants, skin,
    # grey saddle or navy jacket)
    if a <= 10 or is_jacket(r, g, b, a):
        return False
    return (20 <= r <= 210 and r >= g + 12 and g >= b + 5 and b <= 150)


# coat ramps: (dark, light) sampled across the brown brightness range
COAT_RAMPS = {
    "chestnut": None,                                  # identity (original)
    "black":    ((22, 22, 26), (104, 102, 112)),
    "snow":     ((96, 98, 110), (240, 242, 246)),
}


def remap_coat(r, ramp):
    t = max(0.0, min(1.0, (r - 20) / 190.0))
    dark, light = ramp
    return tuple(int(dark[i] + (light[i] - dark[i]) * t) for i in range(3))


def split_horse(src_name):
    src = Image.open(os.path.join(SPRITES, src_name)).convert("RGBA")
    W, H = src.size
    sp = src.load()

    # coat layers (horse + rider body, jacket removed, brown recoloured)
    coat_imgs = {name: Image.new("RGBA", (W, H), (0, 0, 0, 0))
                 for name in COAT_RAMPS}
    coat_px = {name: coat_imgs[name].load() for name in COAT_RAMPS}

    # jacket layer: greyscale, tintable
    jacket = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    jp = jacket.load()

    for y in range(H):
        for x in range(W):
            r, g, b, a = sp[x, y]
            if a <= 10:
                continue
            if is_jacket(r, g, b, a):
                # greyscale value preserving navy shading -> tintable
                v = int(max(120, min(245, 150 + (b - 55) * 2.2)))
                jp[x, y] = (v, v, v, a)
                continue
            for name, ramp in COAT_RAMPS.items():
                if ramp is not None and is_brown(r, g, b, a):
                    nr, ng, nb = remap_coat(r, ramp)
                    coat_px[name][x, y] = (nr, ng, nb, a)
                else:
                    coat_px[name][x, y] = (r, g, b, a)

    base = src_name.replace("horse_rider_", "").replace(".png", "")  # run/jump/fall
    for name in COAT_RAMPS:
        img = coat_imgs[name]
        add_form_shading(img)        # volume
        add_outline(img, 0.5)        # crisp rim
        img.save(os.path.join(SPRITES, f"horse_{name}_{base}.png"))
    add_outline(jacket, 0.6)         # keep it tintable but crisp
    jacket.save(os.path.join(SPRITES, f"rider_jacket_{base}.png"))
    print(f"split {src_name} -> coats + jacket ({base})")


def gen_horse_layers():
    for n in ["horse_rider_run.png", "horse_rider_jump.png",
              "horse_rider_fall.png"]:
        split_horse(n)


# ---------------------------------------------------------------------------
# 3. STANDING RIDER for the podium finish (body + tintable jacket)
# ---------------------------------------------------------------------------
def gen_rider_stand():
    W, H = 64, 104
    body = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    jacket = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    jd = ImageDraw.Draw(jacket)
    cx = W // 2

    skin = (232, 196, 158, 255)
    skin_sh = (206, 168, 130, 255)
    pants = (236, 228, 206, 255)
    pants_sh = (208, 198, 176, 255)
    boot = (52, 38, 26, 255)
    helmet = (40, 40, 52, 255)

    # boots
    bd.rectangle([cx - 13, 92, cx - 3, 102], fill=boot)
    bd.rectangle([cx + 3, 92, cx + 13, 102], fill=boot)
    # breeches / legs
    bd.polygon([(cx - 11, 68), (cx - 1, 68), (cx - 2, 94), (cx - 12, 94)],
               fill=pants)
    bd.polygon([(cx + 1, 68), (cx + 11, 68), (cx + 12, 94), (cx + 2, 94)],
               fill=pants)
    bd.rectangle([cx - 1, 68, cx + 1, 94], fill=pants_sh)
    # hands (raised, above where sleeves end)
    bd.ellipse([cx - 30, 24, cx - 20, 34], fill=skin)
    bd.ellipse([cx + 20, 24, cx + 30, 34], fill=skin)
    # head
    bd.ellipse([cx - 9, 26, cx + 9, 44], fill=skin)
    bd.ellipse([cx + 1, 30, cx + 8, 42], fill=skin_sh)
    # helmet
    bd.pieslice([cx - 10, 16, cx + 10, 36], 180, 360, fill=helmet)
    bd.rectangle([cx - 11, 28, cx + 11, 31], fill=helmet)      # brim
    bd.rectangle([cx - 2, 13, cx + 2, 18], fill=(70, 70, 86, 255))  # button

    # --- jacket layer (greyscale, tintable): torso + raised V arms ---
    jv = 232
    col = (jv, jv, jv, 255)
    col_sh = (190, 190, 190, 255)
    # torso
    jd.polygon([(cx - 13, 44), (cx + 13, 44), (cx + 11, 70), (cx - 11, 70)],
               fill=col)
    # collar / shading down the middle
    jd.rectangle([cx - 2, 44, cx + 2, 70], fill=col_sh)
    # raised arms (V shape) up to the hands
    jd.polygon([(cx - 11, 46), (cx - 4, 48), (cx - 20, 28), (cx - 26, 30)],
               fill=col)
    jd.polygon([(cx + 11, 46), (cx + 4, 48), (cx + 20, 28), (cx + 26, 30)],
               fill=col)

    add_outline(body, 0.5)
    add_outline(jacket, 0.6)
    body.save(os.path.join(SPRITES, "rider_stand.png"))
    jacket.save(os.path.join(SPRITES, "rider_stand_jacket.png"))
    print("rider_stand.png + rider_stand_jacket.png written")


# ---------------------------------------------------------------------------
# 4. SOFT CONTACT SHADOW (drawn on the ground under the horse / obstacles)
# ---------------------------------------------------------------------------
def gen_shadow():
    W, H = 96, 30
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()
    cx, cy = W / 2.0, H / 2.0
    for y in range(H):
        for x in range(W):
            dx = (x - cx) / cx
            dy = (y - cy) / cy
            d = dx * dx + dy * dy
            if d < 1.0:
                a = int(120 * (1.0 - d) ** 1.5)
                px[x, y] = (0, 0, 0, a)
    img.save(os.path.join(SPRITES, "shadow.png"))
    print("shadow.png written")


if __name__ == "__main__":
    gen_forest_bg()
    gen_horse_layers()
    gen_rider_stand()
    gen_shadow()
    print("done")
