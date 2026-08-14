#!/usr/bin/env python3
"""Remove chroma, split 5x2 atlases, and normalize arena animation frames."""

from pathlib import Path
from PIL import Image, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "HorseRunner.Web" / "art" / "source" / "arena"
RUN_OUTPUT = ROOT / "HorseRunner.Web" / "wwwroot" / "assets" / "arena" / "horse-rider-run"
JUMP_OUTPUT = ROOT / "HorseRunner.Web" / "wwwroot" / "assets" / "arena" / "horse-rider-jump-animated"
PREVIEW = ROOT / "HorseRunner.Web" / "art" / "previews" / "arena-animation"
IDS = ("01", "02", "03", "04", "05", "07", "08")


def remove_green(image: Image.Image) -> Image.Image:
    pixels = image.convert("RGBA")
    out = []
    for r, g, b, _ in pixels.getdata():
        dominance = g - max(r, b)
        if g > 105 and dominance > 18:
            alpha = max(0, min(255, int(255 * (1 - (dominance - 18) / 105))))
            spill = max(0, dominance)
            out.append((r, max(max(r, b), g - spill), b, alpha))
        else:
            out.append((r, g, b, 255))
    pixels.putdata(out)
    return pixels


def keep_main_subject(image: Image.Image) -> Image.Image:
    """Discard disconnected fragments from neighbouring generated atlas cells."""
    alpha = image.getchannel("A")
    width, height = image.size
    visible = alpha.load()
    visited = set()
    largest = []
    for y in range(height):
        for x in range(width):
            if visible[x, y] <= 42 or (x, y) in visited:
                continue
            component = []
            stack = [(x, y)]
            visited.add((x, y))
            while stack:
                px, py = stack.pop()
                component.append((px, py))
                for nx, ny in ((px-1,py-1),(px,py-1),(px+1,py-1),(px-1,py),(px+1,py),(px-1,py+1),(px,py+1),(px+1,py+1)):
                    if 0 <= nx < width and 0 <= ny < height and visible[nx, ny] > 42 and (nx, ny) not in visited:
                        visited.add((nx, ny)); stack.append((nx, ny))
            if len(component) > len(largest):
                largest = component
    mask = Image.new("L", image.size, 0)
    mask_pixels = mask.load()
    for x, y in largest:
        mask_pixels[x, y] = 255
    mask = mask.filter(ImageFilter.MaxFilter(9))
    image.putalpha(ImageChops.multiply(alpha, mask))
    return image


def cells(atlas: Image.Image):
    width, height = atlas.size
    for row in range(2):
        for column in range(5):
            yield atlas.crop((column * width // 5, row * height // 2,
                              (column + 1) * width // 5, (row + 1) * height // 2))


def normalized(subject: Image.Image) -> Image.Image:
    bounds = subject.getchannel("A").getbbox()
    if not bounds:
        raise RuntimeError("Empty animation cell")
    subject = subject.crop(bounds)
    frame = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    scale = min(238 / subject.width, 238 / subject.height)
    size = (max(1, round(subject.width * scale)), max(1, round(subject.height * scale)))
    resized = subject.resize(size, Image.Resampling.LANCZOS)
    frame.alpha_composite(resized, ((256 - resized.width) // 2, 248 - resized.height))
    return frame


def process(identifier: str):
    atlas = Image.open(SOURCE / f"animation-atlas-{identifier}-chroma.png")
    frames = [normalized(keep_main_subject(remove_green(cell))) for cell in cells(atlas)]
    run = frames[:6]
    jump = frames[6:]
    run_dir, jump_dir = RUN_OUTPUT / identifier, JUMP_OUTPUT / identifier
    run_dir.mkdir(parents=True, exist_ok=True)
    jump_dir.mkdir(parents=True, exist_ok=True)
    for index, frame in enumerate(run, 1):
        frame.save(run_dir / f"{index:02d}.png")
    for index, frame in enumerate(jump, 1):
        frame.save(jump_dir / f"{index:02d}.png")
    preview = Image.new("RGBA", (1280, 512), (229, 232, 226, 255))
    for index, frame in enumerate(frames):
        preview.alpha_composite(frame, ((index % 5) * 256, (index // 5) * 256))
    PREVIEW.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW / f"{identifier}.png")


if __name__ == "__main__":
    for item in IDS:
        process(item)
    print("Wrote 42 canter frames and 28 jump frames")
