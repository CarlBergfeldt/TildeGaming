#!/usr/bin/env python3
"""Split transparent 2x2 arena prop atlases into normalized game sprites."""

from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "HorseRunner.Web" / "art" / "source" / "arena"
OUTPUT = ROOT / "HorseRunner.Web" / "wwwroot" / "assets" / "arena" / "props"
PREVIEW = ROOT / "HorseRunner.Web" / "art" / "previews" / "event-props.png"


def remove_boundary_bleed(cell: Image.Image, edges=("top", "left")) -> Image.Image:
    """Remove atlas neighbours that cross into a cell from its top/left edge."""
    alpha = cell.getchannel("A")
    width, height = cell.size
    visible = alpha.load()
    visited = set()
    remove = set()
    for y in range(height):
        for x in range(width):
            if visible[x, y] <= 28 or (x, y) in visited:
                continue
            stack = [(x, y)]
            component = []
            touches_internal_boundary = False
            visited.add((x, y))
            while stack:
                px, py = stack.pop()
                component.append((px, py))
                if (("left" in edges and px <= 1) or ("right" in edges and px >= width - 2)
                        or ("top" in edges and py <= 1) or ("bottom" in edges and py >= height - 2)):
                    touches_internal_boundary = True
                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if 0 <= nx < width and 0 <= ny < height and visible[nx, ny] > 28 and (nx, ny) not in visited:
                        visited.add((nx, ny))
                        stack.append((nx, ny))
            if touches_internal_boundary:
                remove.update(component)
    if remove:
        pixels = cell.load()
        for x, y in remove:
            pixels[x, y] = (0, 0, 0, 0)
    return cell


def split_atlas(filename: str, first_number: int) -> None:
    atlas = Image.open(SOURCE / filename).convert("RGBA")
    cell_width = atlas.width // 2
    cell_height = atlas.height // 2
    for index in range(4):
        column = index % 2
        row = index // 2
        cell = atlas.crop((column * cell_width, row * cell_height,
                           (column + 1) * cell_width, (row + 1) * cell_height))
        if filename == "event-services-transparent.png" and index >= 2:
            cell = cell.crop((0, 48, cell.width, cell.height))
        if index == 3:
            cell = remove_boundary_bleed(cell)
        if filename == "spectators-transparent.png" and index == 0:
            cell = remove_boundary_bleed(cell, ("right",))
            cell = cell.crop((0, 0, cell.width - 24, cell.height))
        alpha = cell.getchannel("A")
        bounds = alpha.getbbox()
        if bounds is None:
            raise RuntimeError(f"No visible pixels in {filename} cell {index + 1}")
        subject = cell.crop(bounds)
        frame = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        scale = min(460 / subject.width, 456 / subject.height)
        resized = subject.resize((max(1, round(subject.width * scale)),
                                  max(1, round(subject.height * scale))), Image.Resampling.LANCZOS)
        x = (512 - resized.width) // 2
        y = 488 - resized.height
        frame.alpha_composite(resized, (x, y))
        frame.save(OUTPUT / f"{first_number + index:02d}.png")


def render_preview() -> None:
    preview = Image.new("RGBA", (2048, 1024), (238, 241, 239, 255))
    for y in range(0, preview.height, 32):
        for x in range(0, preview.width, 32):
            if (x // 32 + y // 32) % 2:
                preview.paste((224, 229, 226, 255), (x, y, x + 32, y + 32))
    for index, number in enumerate(range(14, 22)):
        frame = Image.open(OUTPUT / f"{number:02d}.png").convert("RGBA")
        preview.alpha_composite(frame, ((index % 4) * 512, (index // 4) * 512))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW)


def process_gate() -> None:
    source = Image.open(SOURCE / "arena-fence-gate-transparent.png").convert("RGBA")
    bounds = source.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("Arena fence gate has no visible pixels")
    subject = source.crop(bounds)
    frame = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    scale = min(472 / subject.width, 448 / subject.height)
    resized = subject.resize((round(subject.width * scale), round(subject.height * scale)), Image.Resampling.LANCZOS)
    frame.alpha_composite(resized, ((512 - resized.width) // 2, 488 - resized.height))
    frame.save(OUTPUT / "22.png")


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    split_atlas("event-services-transparent.png", 14)
    split_atlas("spectators-transparent.png", 18)
    process_gate()
    render_preview()
    print("Wrote arena props 14.png through 22.png")
