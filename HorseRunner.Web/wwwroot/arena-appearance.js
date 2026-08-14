export const HORSE_APPEARANCES = Object.freeze([
  Object.freeze({ id: "chestnut", label: "Chestnut", trait: "Warm copper coat", swatch: "#a94d25", ramp: null }),
  Object.freeze({ id: "midnight", label: "Midnight", trait: "Glossy black coat", swatch: "#30343b", ramp: [[18, 20, 25], [108, 112, 121]] }),
  Object.freeze({ id: "dapple-grey", label: "Dapple Grey", trait: "Silver dapple coat", swatch: "#bbc1c7", ramp: [[83, 88, 98], [232, 235, 239]], dapples: true }),
  Object.freeze({ id: "palomino", label: "Palomino", trait: "Golden show coat", swatch: "#dba84f", ramp: [[104, 63, 24], [245, 202, 111]] })
]);

export const RIDER_APPEARANCES = Object.freeze([
  Object.freeze({ id: "navy", label: "Classic Navy", trait: "Traditional show jacket", swatch: "#233957", ramp: null }),
  Object.freeze({ id: "burgundy", label: "Burgundy", trait: "Wine-red jacket", swatch: "#7f293d", ramp: [[54, 14, 25], [178, 65, 85]] }),
  Object.freeze({ id: "forest", label: "Forest", trait: "Country-green jacket", swatch: "#375f46", ramp: [[20, 48, 31], [91, 145, 101]] }),
  Object.freeze({ id: "plum", label: "Plum", trait: "Deep violet jacket", swatch: "#604066", ramp: [[38, 21, 47], [135, 88, 145]] })
]);

export const DEFAULT_ARENA_APPEARANCE = Object.freeze({ horse: "chestnut", rider: "navy" });

const byId = (items, id) => items.find(item => item.id === id) || items[0];
export const horseAppearance = id => byId(HORSE_APPEARANCES, id);
export const riderAppearance = id => byId(RIDER_APPEARANCES, id);

export function normalizeArenaAppearance(value) {
  return {
    horse: horseAppearance(value?.horse).id,
    rider: riderAppearance(value?.rider).id
  };
}

function jacketPixel(r, g, b, a) {
  return a > 10 && b >= r + 10 && b >= g + 6 && b >= 38;
}

function horsePixel(r, g, b, a) {
  return a > 10 && r >= 24 && r <= 245 && r >= g + 11 && g >= b + 4 && b <= 165;
}

export function remapArenaPixel(r, g, b, a, x, y, horseId, riderId) {
  const rider = riderAppearance(riderId);
  if (rider.ramp && jacketPixel(r, g, b, a)) {
    const value = Math.max(r, g, b);
    const amount = Math.max(0, Math.min(1, (value - 25) / 170));
    return [
      Math.round(rider.ramp[0][0] + (rider.ramp[1][0] - rider.ramp[0][0]) * amount),
      Math.round(rider.ramp[0][1] + (rider.ramp[1][1] - rider.ramp[0][1]) * amount),
      Math.round(rider.ramp[0][2] + (rider.ramp[1][2] - rider.ramp[0][2]) * amount), a
    ];
  }
  const horse = horseAppearance(horseId);
  if (horse.ramp && horsePixel(r, g, b, a)) {
    const amount = Math.max(0, Math.min(1, (r - 22) / 215));
    let mapped = horse.ramp[0].map((dark, index) => Math.round(dark + (horse.ramp[1][index] - dark) * amount));
    if (horse.dapples) {
      const dapple = Math.sin(x * .71 + y * 1.13) + Math.sin(x * 1.91 - y * .43);
      if (dapple > 1.25) mapped = mapped.map(value => Math.min(255, value + 23));
      else if (dapple < -1.35) mapped = mapped.map(value => Math.max(0, value - 15));
    }
    return [...mapped, a];
  }
  return [r, g, b, a];
}

export function recolorArenaSprite(image, horseId, riderId) {
  const canvas = document.createElement("canvas");
  canvas.width = image.naturalWidth || image.width;
  canvas.height = image.naturalHeight || image.height;
  const context = canvas.getContext("2d", { willReadFrequently: true });
  context.drawImage(image, 0, 0);
  const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
  for (let offset = 0; offset < pixels.data.length; offset += 4) {
    const pixel = offset / 4;
    const mapped = remapArenaPixel(pixels.data[offset], pixels.data[offset + 1], pixels.data[offset + 2], pixels.data[offset + 3], pixel % canvas.width, Math.floor(pixel / canvas.width), horseId, riderId);
    pixels.data[offset] = mapped[0]; pixels.data[offset + 1] = mapped[1]; pixels.data[offset + 2] = mapped[2]; pixels.data[offset + 3] = mapped[3];
  }
  context.putImageData(pixels, 0, 0);
  return canvas;
}
