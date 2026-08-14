import { clamp } from "./game-core.js";

export const ARENA_OBJECT_TYPES = Object.freeze([
  "jump", "log-jump", "vertical-rails", "oxer", "cone", "dressage-marker",
  "barrel", "hay-bale", "flower-planter", "saddle-rack", "water-trough", "arena-fence", "apple",
  "hotdog-stand", "drinks-cart", "judges-table", "photographer", "spectator-group", "grandstand", "waiting-horses", "announcer-booth"
]);

export const JUMP_TYPES = Object.freeze(["jump", "log-jump", "vertical-rails", "oxer"]);
export const APPLE_EATING_SECONDS = 5;

export function validateArena(data) {
  const errors = [];
  if (!data || data.version !== 1) errors.push("version must be 1");
  if (!data?.settings || !Number.isFinite(data.settings.laps) || data.settings.laps < 1) errors.push("settings.laps must be positive");
  if (!Array.isArray(data?.scenes) || !data.scenes.length) errors.push("at least one scene is required");
  for (const [index, scene] of (data?.scenes || []).entries()) {
    if (!scene.id || !scene.name) errors.push(`scene ${index + 1} needs id and name`);
    if (!Array.isArray(scene.objects)) errors.push(`scene ${index + 1} needs objects`);
    if (!Array.isArray(scene.checkpoints) || !scene.checkpoints.length) errors.push(`scene ${index + 1} needs at least one course goal`);
    if (scene.floor && ![scene.floor.width, scene.floor.height].every(Number.isFinite)) errors.push(`scene ${index + 1} has invalid floor size`);
    for (const object of (scene.objects || [])) {
      if (!object.id || !ARENA_OBJECT_TYPES.includes(object.type)) errors.push(`invalid object in scene ${index + 1}`);
      if (![object.x, object.y, object.rotation, object.height].every(Number.isFinite)) errors.push(`${object.id || "object"} has invalid numbers`);
    }
  }
  return errors;
}

export function finalScore({ elapsed, clearances, faults, laps }) {
  return Math.max(0, Math.round(laps * 1000 + clearances * 250 - faults * 350 - elapsed * 12));
}

export function qualifies(scores, score) {
  return scores.length < 5 || scores.some(entry => score > entry.score);
}

export function addScore(scores, entry) {
  return [...scores, entry].sort((a, b) => b.score - a.score || a.time - b.time).slice(0, 5);
}

export function jumpClearance(z, height) { return z >= 5 + clamp(height, 1, 3) * 4; }

export function distance(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }

export function paceSpeed(baseSpeed, level) {
  return baseSpeed * [0, .62, 1][clamp(Math.round(level), 0, 2)];
}

export function arenaFloor(scene) {
  const width = clamp(Number(scene?.floor?.width) || 280, 120, 288);
  const height = clamp(Number(scene?.floor?.height) || 152, 72, 156);
  return { x: (320 - width) / 2, y: (180 - height) / 2, width, height };
}

export function directionIndex(angle) {
  const fullTurn = Math.PI * 2;
  const normalized = ((angle % fullTurn) + fullTurn) % fullTurn;
  return Math.round(normalized / (Math.PI / 4)) % 8;
}

export function animationFrame(phase, frameCount) {
  if (!Number.isFinite(phase) || frameCount < 1) return 0;
  return ((Math.floor(phase) % frameCount) + frameCount) % frameCount;
}

export function jumpAnimationFrame(z, verticalSpeed, jumpPower) {
  if (z <= 0) return 0;
  if (verticalSpeed > jumpPower * .42) return 0;
  if (verticalSpeed > 0) return 1;
  if (verticalSpeed > -jumpPower * .42) return 2;
  return 3;
}

export function shouldRebuildObstacle(distanceFromHorse, radius, elapsed, rebuildAt) {
  return distanceFromHorse > radius + 18 && elapsed >= rebuildAt;
}
