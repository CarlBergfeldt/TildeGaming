import { FIXED_STEP, clamp } from "./game-core.js";
import { ARENA_PROP_BY_TYPE, ARENA_PROPS, HORSE_DIRECTIONS, HORSE_EATING_DIRECTIONS, horseEatingSpritePath, horseJumpSpritePath, horseSpritePath, propSpritePath } from "./arena-assets.js";
import { APPLE_EATING_SECONDS, JUMP_TYPES, addScore, directionIndex, distance, finalScore, jumpClearance, paceSpeed, qualifies, validateArena } from "./arena-core.js";

const LOGICAL_WIDTH = 320;
const LOGICAL_HEIGHT = 180;
const canvas = document.querySelector("#arena-game");
const ctx = canvas.getContext("2d");
const $ = id => document.getElementById(id);
const keys = new Set();
const horseSprites = [];
const horseJumpSprites = [];
const horseEatingSprites = [];
const propSprites = new Map();
const PACE_NAMES = ["Halt", "Slow", "Fast"];
let data, scene, mode = "menu", last = 0, accumulator = 0, horse, lap, checkpoint, elapsed, clearances, faults, touched, paceLevel = 1, objectStates = new Map();

function storageGet(key, fallback) { try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; } }
function storageSet(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch {} }
function loadImage(source) { return new Promise((resolve, reject) => { const image = new Image(); image.onload = () => resolve(image); image.onerror = reject; image.src = source; }); }
export function getArenaData() { return structuredClone(data); }
export function setArenaData(next) { const errors = validateArena(next); if (errors.length) throw new Error(errors.join("\n")); data = structuredClone(next); scene = data.scenes[0]; }

async function preloadAssets() {
  const directions = await Promise.all(HORSE_DIRECTIONS.map(direction => loadImage(horseSpritePath(direction))));
  directions.forEach((image, index) => horseSprites[index] = image);
  const jumpDirections = await Promise.all(HORSE_DIRECTIONS.map(direction => loadImage(horseJumpSpritePath(direction))));
  jumpDirections.forEach((image, index) => horseJumpSprites[index] = image);
  const eatingDirections = await Promise.all(HORSE_EATING_DIRECTIONS.map(direction => loadImage(horseEatingSpritePath(direction))));
  eatingDirections.forEach((image, index) => horseEatingSprites[index] = image);
  const props = await Promise.all(ARENA_PROPS.map(prop => loadImage(propSpritePath(prop))));
  ARENA_PROPS.forEach((prop, index) => propSprites.set(prop.type, props[index]));
}

async function load() {
  const startButton = $("arena-start");
  startButton.disabled = true;
  startButton.textContent = "Preparing horses…";
  const defaults = await fetch("data/arena.json").then(response => response.json());
  setArenaData(storageGet("horseRunnerArenaDraftV2", defaults));
  startButton.disabled = false;
  startButton.textContent = "Enter arena";
  drawLeaderboard();
  draw();
  if (new URLSearchParams(location.search).has("autostart")) start();
  await preloadAssets();
  draw();
}

function start() {
  scene = data.scenes[0];
  const startPosition = scene.start;
  horse = { x: startPosition.x, y: startPosition.y, angle: startPosition.angle, z: 0, vz: 0, phase: 0, eating: 0, eatingAppleId: null, resumePace: 1 };
  lap = 0; checkpoint = 0; elapsed = 0; clearances = 0; faults = 0; touched = new Set(); paceLevel = 1; objectStates = new Map(); mode = "playing";
  $("arena-overlay").hidden = true; $("arena-name-card").hidden = true; updateHud(); syncHeaderControls(); canvas.focus();
}

function jump() { if (mode === "playing" && horse.z === 0 && horse.eating <= 0) horse.vz = data.settings.jumpPower; }

function setPace(nextLevel) {
  if (mode !== "playing" || horse.eating > 0) return;
  paceLevel = clamp(nextLevel, 0, 2);
  updateHud();
}

function togglePause() {
  if (mode !== "playing" && mode !== "paused") return;
  mode = mode === "playing" ? "paused" : "playing";
  syncHeaderControls();
}

function syncHeaderControls() {
  if (document.body.dataset.view !== "arena") return;
  $("pause").textContent = mode === "paused" ? "Resume" : "Pause";
  $("pause").setAttribute("aria-pressed", String(mode === "paused"));
}

function update(dt) {
  if (mode !== "playing") return;
  elapsed += dt;
  if (horse.eating > 0) {
    horse.eating = Math.max(0, horse.eating - dt);
    horse.phase += dt * 4;
    if (horse.eating === 0) {
      const appleState = objectStates.get(horse.eatingAppleId);
      if (appleState) objectStates.set(horse.eatingAppleId, { ...appleState, outcome: "eaten", picked: false, consumed: true });
      horse.eatingAppleId = null;
      paceLevel = horse.resumePace;
    }
    updateHud();
    return;
  }
  const steer = (keys.has("ArrowRight") || keys.has("KeyD") ? 1 : 0) - (keys.has("ArrowLeft") || keys.has("KeyA") ? 1 : 0);
  const speed = paceSpeed(data.settings.baseSpeed, paceLevel);
  if (speed > 0) horse.angle += steer * data.settings.turnSpeed * (paceLevel === 1 ? .82 : 1) * dt;
  horse.phase += dt * speed * .2;
  horse.x = clamp(horse.x + Math.cos(horse.angle) * speed * dt, 31, 289);
  horse.y = clamp(horse.y + Math.sin(horse.angle) * speed * dt, 48, 153);
  if (horse.z > 0 || horse.vz > 0) {
    horse.vz -= data.settings.gravity * dt;
    horse.z += horse.vz * dt;
    if (horse.z <= 0) { horse.z = 0; horse.vz = 0; }
  }
  for (const state of objectStates.values()) state.fall = Math.min(1, (state.fall || 0) + dt * 4.5);
  scene.objects.forEach(object => {
    const definition = ARENA_PROP_BY_TYPE.get(object.type);
    const visualState = objectStates.get(object.id);
    if (!definition || definition.behavior === "decor" || visualState?.fallen || visualState?.consumed) return;
    const d = distance(horse, object);
    if (definition.behavior === "distraction") {
      if (!visualState?.picked && d < definition.radius) {
        horse.eating = APPLE_EATING_SECONDS;
        horse.eatingAppleId = object.id;
        horse.resumePace = paceLevel;
        paceLevel = 0;
        objectStates.set(object.id, { outcome: "picked", picked: true, consumed: false });
        touched.add(object.id);
      }
      return;
    }
    if (d < definition.radius && !touched.has(object.id)) {
      touched.add(object.id);
      if (JUMP_TYPES.includes(object.type) && jumpClearance(horse.z, object.height)) {
        clearances++;
        objectStates.set(object.id, { outcome: "cleared", fall: 0, until: elapsed + 1.1 });
      } else {
        faults++; elapsed += data.settings.hitPenalty;
        if (JUMP_TYPES.includes(object.type)) objectStates.set(object.id, { outcome: "fallen", fallen: true, fall: 0 });
      }
    }
    if (d > definition.radius + 6) touched.delete(object.id);
  });
  for (const [id, visualState] of objectStates) if (visualState.outcome === "cleared" && elapsed > visualState.until) objectStates.delete(id);
  const target = scene.checkpoints[checkpoint];
  if (distance(horse, target) < 18) {
    checkpoint = (checkpoint + 1) % scene.checkpoints.length;
    if (checkpoint === 0) { lap++; if (lap >= data.settings.laps) finish(); }
  }
  updateHud();
}

function finish() {
  mode = "finished";
  const score = finalScore({ elapsed, clearances, faults, laps: lap });
  $("arena-result").textContent = `${score} points · ${elapsed.toFixed(1)}s · ${clearances} clear · ${faults} faults`;
  const scores = storageGet("horseRunnerArenaScores", []);
  if (qualifies(scores, score)) { $("arena-name-card").hidden = false; $("arena-player-name").dataset.score = score; $("arena-player-name").focus(); }
  else showOverlay("Five laps complete!", "Ride again");
}

function showOverlay(title, button) { $("arena-title").textContent = title; $("arena-start").textContent = button; $("arena-overlay").hidden = false; }
function updateHud() { $("arena-lap").textContent = `${Math.min(lap + 1, data.settings.laps)}/${data.settings.laps}`; $("arena-time").textContent = elapsed.toFixed(1); $("arena-faults").textContent = faults; $("arena-pace").textContent = horse?.eating > 0 ? `Eating ${horse.eating.toFixed(1)}s` : PACE_NAMES[paceLevel]; }
function drawLeaderboard() { const scores = storageGet("horseRunnerArenaScores", []); $("arena-scores").innerHTML = scores.length ? scores.map((score, index) => `<li><span>${index + 1}. ${escapeHtml(score.name)}</span><strong>${score.score}</strong></li>`).join("") : "<li>No champions yet</li>"; }
function escapeHtml(value) { const span = document.createElement("span"); span.textContent = value; return span.innerHTML; }

function rect(x, y, width, height, color) { ctx.fillStyle = color; ctx.fillRect(x, y, width, height); }
function ellipse(x, y, radiusX, radiusY, color) { ctx.beginPath(); ctx.ellipse(x, y, radiusX, radiusY, 0, 0, Math.PI * 2); ctx.fillStyle = color; ctx.fill(); }

function beginFrame() {
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.setTransform(canvas.width / LOGICAL_WIDTH, 0, 0, canvas.height / LOGICAL_HEIGHT, 0, 0);
  ctx.imageSmoothingEnabled = true;
}

function drawBackground() {
  const palette = scene?.palette || {};
  rect(0, 0, 320, 180, palette.grass || "#426b45");
  for (let y = 0; y < 180; y += 9) rect(0, y, 320, 1, y % 18 ? "rgba(255,255,255,.025)" : "rgba(12,42,24,.08)");
  rect(16, 10, 288, 160, "#6d4934");
  rect(20, 14, 280, 152, palette.sand || "#c99b62");
  for (let x = 24; x < 300; x += 13) for (let y = 18; y < 164; y += 11) ellipse(x + (y % 3), y, 1.4, .45, "rgba(103,67,42,.18)");
  const rail = palette.rail || "#fff0bd";
  rect(16, 9, 288, 4, rail); rect(16, 167, 288, 4, rail); rect(16, 13, 4, 154, rail); rect(300, 13, 4, 154, rail);
  for (let x = 20; x <= 300; x += 20) { rect(x - 1, 7, 3, 9, "#e3cfaa"); rect(x - 2, 6, 5, 2, "#fff8dc"); }
  for (let x = 25; x < 300; x += 13) { rect(x, 1, 9, 6, palette.crowd || "#693f55"); ellipse(x + 4, 1.5, 2.2, 2.2, ["#f1c27d", "#9a684f", "#d89b72"][x % 3]); }
}

function drawCourseGuide() {
  if (!scene?.checkpoints?.length) return;
  ctx.save();
  ctx.beginPath();
  ctx.moveTo(scene.start.x, scene.start.y);
  scene.checkpoints.forEach(point => ctx.lineTo(point.x, point.y));
  ctx.closePath();
  ctx.strokeStyle = "rgba(255,247,207,.34)";
  ctx.lineWidth = 1.2;
  ctx.setLineDash([4, 5]);
  ctx.stroke();
  const target = scene.checkpoints[checkpoint || 0];
  if (target && mode !== "menu") {
    const pulse = 7.5 + Math.sin(performance.now() / 180) * 1.2;
    ctx.setLineDash([]);
    ctx.beginPath(); ctx.arc(target.x, target.y, pulse, 0, Math.PI * 2);
    ctx.strokeStyle = "rgba(255,238,137,.9)"; ctx.lineWidth = 1.6; ctx.stroke();
    ctx.beginPath(); ctx.moveTo(target.x, target.y - 12); ctx.lineTo(target.x - 3, target.y - 8); ctx.lineTo(target.x + 3, target.y - 8); ctx.closePath(); ctx.fillStyle = "#ffe682"; ctx.fill();
  }
  ctx.restore();
}

function drawProp(object) {
  const definition = ARENA_PROP_BY_TYPE.get(object.type);
  const image = propSprites.get(object.type);
  if (!definition || !image) return;
  const visualState = objectStates.get(object.id);
  if (visualState?.picked || visualState?.consumed) return;
  ellipse(0, 1, definition.width * .36, Math.max(1.5, definition.height * .09), "rgba(30,25,20,.22)");
  if (definition.behavior === "distraction") {
    const pulse = 1 + Math.sin(performance.now() / 220) * .12;
    ctx.save(); ctx.scale(pulse, pulse);
    ctx.beginPath(); ctx.arc(0, -definition.height * .55, 7, 0, Math.PI * 2);
    ctx.strokeStyle = "rgba(255,238,166,.52)"; ctx.lineWidth = .7; ctx.stroke(); ctx.restore();
  }
  ctx.save();
  if (visualState?.fallen) {
    const fall = visualState.fall || 0;
    ctx.translate(0, fall * 3);
    ctx.rotate(fall * .12);
    ctx.scale(1, 1 - fall * .68);
    ctx.globalAlpha = .92;
  }
  ctx.drawImage(image, -definition.width / 2, -definition.height, definition.width, definition.height);
  ctx.restore();
  if (visualState?.outcome === "cleared") {
    ctx.beginPath(); ctx.arc(0, -definition.height * .55, 5, 0, Math.PI * 2); ctx.fillStyle = "rgba(42,112,66,.9)"; ctx.fill();
    ctx.strokeStyle = "#fff7cf"; ctx.lineWidth = 1.3; ctx.beginPath(); ctx.moveTo(-2.5, -definition.height * .55); ctx.lineTo(-.5, -definition.height * .55 + 2); ctx.lineTo(3, -definition.height * .55 - 2.5); ctx.stroke();
  }
}

function drawHorse() {
  const index = directionIndex(horse.angle);
  const eating = horse.eating > 0;
  const direction = eating ? HORSE_EATING_DIRECTIONS[index] : HORSE_DIRECTIONS[index];
  const image = eating ? horseEatingSprites[index] : horse.z > 2 ? horseJumpSprites[index] : horseSprites[index];
  const bob = eating ? Math.sin(horse.phase * 1.7) * .25 : horse.z === 0 ? Math.sin(horse.phase) * .7 : 0;
  const visualLift = horse.z * .35;
  const spriteSize = 38 + clamp((horse.y - 27) / 126, 0, 1) * 8;
  ellipse(horse.x, horse.y + 2, clamp(13 - horse.z * .12, 7, 13), clamp(4 - horse.z * .035, 2, 4), "rgba(23,20,18,.3)");
  ctx.save();
  ctx.translate(horse.x, horse.y - visualLift + bob);
  ctx.scale(direction.flip ? -1 : 1, 1);
  if (image) ctx.drawImage(image, -spriteSize / 2, -spriteSize, spriteSize, spriteSize);
  else { rect(-8, -10, 16, 8, "#9b5237"); rect(3, -14, 8, 8, "#bd6841"); }
  ctx.restore();
}

function draw() {
  if (!scene) return;
  beginFrame();
  drawBackground();
  drawCourseGuide();
  const renderables = scene.objects.map(object => ({ baseline: object.y, draw: () => { ctx.save(); ctx.translate(object.x, object.y); ctx.rotate(object.rotation || 0); drawProp(object); ctx.restore(); } }));
  if (horse) renderables.push({ baseline: horse.y, draw: drawHorse });
  renderables.sort((a, b) => a.baseline - b.baseline).forEach(item => item.draw());
  if (mode === "paused") { rect(0, 0, LOGICAL_WIDTH, LOGICAL_HEIGHT, "rgba(20,24,24,.42)"); ctx.fillStyle = "#fff4cf"; ctx.font = "bold 14px Georgia,serif"; ctx.textAlign = "center"; ctx.fillText("ARENA PAUSED", LOGICAL_WIDTH / 2, LOGICAL_HEIGHT / 2); ctx.textAlign = "start"; }
}

function loop(now) {
  const dt = Math.min((now - last) / 1000, .1) || 0;
  last = now; accumulator += dt;
  while (accumulator >= FIXED_STEP) { update(FIXED_STEP); accumulator -= FIXED_STEP; }
  draw();
  requestAnimationFrame(loop);
}

function renderGameToText() {
  const direction = horse ? HORSE_DIRECTIONS[directionIndex(horse.angle)].id : null;
  return JSON.stringify({
    coordinateSystem: "origin top-left; x increases right; y increases down; z increases upward",
    mode,
    horse: horse ? { x: +horse.x.toFixed(1), y: +horse.y.toFixed(1), z: +horse.z.toFixed(1), angle: +horse.angle.toFixed(2), direction, pace: horse.eating > 0 ? "Eating" : PACE_NAMES[paceLevel], speed: +paceSpeed(data.settings.baseSpeed, paceLevel).toFixed(1), eatingRemaining: +horse.eating.toFixed(1), eatingAppleId: horse.eatingAppleId } : null,
    lap: lap ?? 0, checkpoint: checkpoint ?? 0, elapsed: +(elapsed ?? 0).toFixed(1), clearances: clearances ?? 0, faults: faults ?? 0,
    objects: (scene?.objects || []).map(object => ({ id: object.id, type: object.type, x: object.x, y: object.y, height: object.height, outcome: objectStates.get(object.id)?.outcome || null }))
  });
}

window.arena_render_game_to_text = renderGameToText;
window.arena_advance_time = milliseconds => { const steps = Math.max(1, Math.round(milliseconds / (1000 / 60))); for (let i = 0; i < steps; i++) update(FIXED_STEP); draw(); };

$("arena-start").addEventListener("click", start);
$("arena-jump").addEventListener("pointerdown", jump);
$("pause").addEventListener("click", () => { if (document.body.dataset.view === "arena") togglePause(); });
$("restart").addEventListener("click", () => { if (document.body.dataset.view === "arena") start(); });
addEventListener("gameviewchange", syncHeaderControls);
$("arena-score-form").addEventListener("submit", event => { event.preventDefault(); const input = $("arena-player-name"), entry = { name: input.value.trim().slice(0, 16) || "Rider", score: Number(input.dataset.score), time: elapsed, clearances, faults }; storageSet("horseRunnerArenaScores", addScore(storageGet("horseRunnerArenaScores", []), entry)); input.value = ""; $("arena-name-card").hidden = true; drawLeaderboard(); showOverlay("Hall of fame!", "Ride again"); });
addEventListener("keydown", event => {
  if (document.body.dataset.view !== "arena") return;
  if (event.code === "Space") { event.preventDefault(); jump(); }
  if ((event.code === "ArrowUp" || event.code === "KeyW") && !event.repeat) { event.preventDefault(); setPace(paceLevel + 1); }
  if ((event.code === "ArrowDown" || event.code === "KeyS") && !event.repeat) { event.preventDefault(); setPace(paceLevel - 1); }
  if (event.code === "KeyP" || event.code === "Escape") { event.preventDefault(); togglePause(); }
  if (event.code === "KeyF") { event.preventDefault(); if (!document.fullscreenElement) canvas.parentElement.requestFullscreen?.(); else document.exitFullscreen?.(); }
  keys.add(event.code);
});
addEventListener("keyup", event => keys.delete(event.code));
document.addEventListener("visibilitychange", () => { if (document.hidden && mode === "playing") { mode = "paused"; syncHeaderControls(); } });

load().catch(error => { console.error(error); $("arena-start").textContent = "Assets failed to load"; });
requestAnimationFrame(loop);
