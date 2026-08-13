import { clamp } from "./game-core.js";

export function validateArena(data) {
  const errors = [];
  if (!data || data.version !== 1) errors.push("version must be 1");
  if (!data?.settings || !Number.isFinite(data.settings.laps) || data.settings.laps < 1) errors.push("settings.laps must be positive");
  if (!Array.isArray(data?.scenes) || !data.scenes.length) errors.push("at least one scene is required");
  for (const [index, scene] of (data?.scenes || []).entries()) {
    if (!scene.id || !scene.name) errors.push(`scene ${index + 1} needs id and name`);
    if (!Array.isArray(scene.objects)) errors.push(`scene ${index + 1} needs objects`);
    for (const object of (scene.objects || [])) {
      if (!object.id || !["jump", "cone"].includes(object.type)) errors.push(`invalid object in scene ${index + 1}`);
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
