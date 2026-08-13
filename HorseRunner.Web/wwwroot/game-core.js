export const LOGICAL_WIDTH = 320;
export const LOGICAL_HEIGHT = 180;
export const GROUND = 142;
export const FIXED_STEP = 1 / 60;

export const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

export const overlaps = (a, b) =>
  a.x < b.x + b.w &&
  a.x + a.w > b.x &&
  a.y < b.y + b.h &&
  a.y + a.h > b.y;

export const scoreText = value =>
  Math.max(0, Math.floor(value)).toString().padStart(5, "0");

const obstacleTypes = Object.freeze([
  Object.freeze({ kind: "log", w: 20, h: 13 }),
  Object.freeze({ kind: "stump", w: 14, h: 19 }),
  Object.freeze({ kind: "fence", w: 25, h: 22 })
]);

export function obstacleForScore(score) {
  const unlocked = clamp(Math.floor(score / 450) + 1, 1, obstacleTypes.length);
  return obstacleTypes[unlocked - 1];
}

export function obstacleDelay(score, randomValue = Math.random()) {
  return clamp(1.45 - score / 5000, 0.82, 1.45) + clamp(randomValue, 0, 1) * 0.34;
}

export function horseHitbox(horse) {
  return { x: horse.x + 8, y: horse.y + 5, w: horse.w - 15, h: horse.h - 7 };
}
