import { FIXED_STEP, clamp } from "./game-core.js";
import { ARENA_PROP_BY_TYPE, ARENA_PROPS, HORSE_DIRECTIONS, HORSE_EATING_DIRECTIONS, HORSE_JUMP_FRAME_COUNT, HORSE_RUN_FRAME_COUNT, horseAnimatedJumpFramePath, horseEatingSpritePath, horseRunFramePath, propSpritePath } from "./arena-assets.js";
import { DEFAULT_ARENA_APPEARANCE, HORSE_APPEARANCES, RIDER_APPEARANCES, horseAppearance, normalizeArenaAppearance, recolorArenaSprite, riderAppearance } from "./arena-appearance.js";
import { APPLE_EATING_SECONDS, JUMP_TYPES, addScore, animationFrame, arenaFloor, directionIndex, distance, finalScore, jumpAnimationFrame, jumpClearance, paceSpeed, qualifies, shouldRebuildObstacle, validateArena } from "./arena-core.js";

const LOGICAL_WIDTH = 320;
const LOGICAL_HEIGHT = 180;
const canvas = document.querySelector("#arena-game");
const ctx = canvas.getContext("2d");
const $ = id => document.getElementById(id);
const keys = new Set();
const horseRunSprites = [];
const horseJumpSprites = [];
const horseEatingSprites = [];
let activeHorseRunSprites = [];
let activeHorseJumpSprites = [];
let activeHorseEatingSprites = [];
const propSprites = new Map();
const PACE_NAMES = ["Halt", "Slow", "Fast"];
let data, scene, mode = "menu", last = 0, accumulator = 0, horse, lap, checkpoint, elapsed, clearances, faults, touched, paceLevel = 1, objectStates = new Map();
let appearance = { ...DEFAULT_ARENA_APPEARANCE };
let selectedSceneId = null;
let selectionStep = "partnership";

function storageGet(key, fallback) { try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; } }
function storageSet(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch {} }
function loadImage(source) { return new Promise((resolve, reject) => { const image = new Image(); image.onload = () => resolve(image); image.onerror = reject; image.src = source; }); }
export function getArenaData() { return structuredClone(data); }
export function setArenaData(next) { const errors = validateArena(next); if (errors.length) throw new Error(errors.join("\n")); data = structuredClone(next); scene = data.scenes.find(item => item.id === selectedSceneId) || data.scenes[0]; selectedSceneId = scene.id; if ($("arena-scene-select")) syncArenaSelector(); }

async function preloadAssets() {
  const directions = await Promise.all(HORSE_DIRECTIONS.map(direction => Promise.all(Array.from({ length: HORSE_RUN_FRAME_COUNT }, (_, frame) => loadImage(horseRunFramePath(direction, frame))))));
  directions.forEach((images, index) => horseRunSprites[index] = images);
  const jumpDirections = await Promise.all(HORSE_DIRECTIONS.map(direction => Promise.all(Array.from({ length: HORSE_JUMP_FRAME_COUNT }, (_, frame) => loadImage(horseAnimatedJumpFramePath(direction, frame))))));
  jumpDirections.forEach((images, index) => horseJumpSprites[index] = images);
  const eatingDirections = await Promise.all(HORSE_EATING_DIRECTIONS.map(direction => loadImage(horseEatingSpritePath(direction))));
  eatingDirections.forEach((image, index) => horseEatingSprites[index] = image);
  const props = await Promise.all(ARENA_PROPS.map(prop => loadImage(propSpritePath(prop))));
  ARENA_PROPS.forEach((prop, index) => propSprites.set(prop.type, props[index]));
}

function rebuildAppearanceSprites() {
  activeHorseRunSprites = horseRunSprites.map(images => images.map(image => recolorArenaSprite(image, appearance.horse, appearance.rider)));
  activeHorseJumpSprites = horseJumpSprites.map(images => images.map(image => recolorArenaSprite(image, appearance.horse, appearance.rider)));
  activeHorseEatingSprites = horseEatingSprites.map(image => recolorArenaSprite(image, appearance.horse, appearance.rider));
}

function appearanceButton(item, kind) {
  return `<button type="button" data-${kind}="${item.id}" aria-pressed="${appearance[kind] === item.id}"><span class="appearance-swatch" style="background:${item.swatch}"></span><strong>${item.label}</strong><small>${item.trait}</small></button>`;
}

function drawAppearancePreview() {
  const preview = $("arena-rider-preview");
  const previewContext = preview.getContext("2d");
  previewContext.clearRect(0, 0, preview.width, preview.height);
  const image = activeHorseRunSprites[4]?.[0];
  if (!image) return;
  const size = 170;
  previewContext.drawImage(image, preview.width / 2 - size / 2, 188 - size, size, size);
}

function syncAppearanceSelector() {
  $("arena-horse-options").innerHTML = HORSE_APPEARANCES.map(item => appearanceButton(item, "horse")).join("");
  $("arena-rider-options").innerHTML = RIDER_APPEARANCES.map(item => appearanceButton(item, "rider")).join("");
  const selectedHorse = horseAppearance(appearance.horse);
  const selectedRider = riderAppearance(appearance.rider);
  const summary = `${selectedHorse.label} horse · ${selectedRider.label} rider`;
  $("arena-appearance-summary").textContent = summary;
  $("arena-final-summary").textContent = summary;
  $("arena-next").textContent = `Continue with ${selectedHorse.label}`;
  drawAppearancePreview();
}

function showSelectionStep(step, title = null) {
  selectionStep = step;
  $("arena-partnership-step").hidden = step !== "partnership";
  $("arena-course-step").hidden = step !== "course";
  $("arena-title").textContent = title || (step === "course" ? "Choose your arena" : "Choose your partnership");
  $("arena-ribbon").textContent = step === "course" ? scene.name : "Partnership";
}

function syncArenaSelector() {
  const selector = $("arena-scene-select");
  selector.innerHTML = data.scenes.map(item => `<option value="${escapeHtml(item.id)}" ${item.id === selectedSceneId ? "selected" : ""}>${escapeHtml(item.name)}</option>`).join("");
  $("arena-start").textContent = `Enter ${scene.name}`;
  if (selectionStep === "course") $("arena-ribbon").textContent = scene.name;
}

function selectArena(id) {
  const next = data.scenes.find(item => item.id === id);
  if (!next) return;
  scene = next;
  selectedSceneId = next.id;
  storageSet("horseRunnerSelectedArena", selectedSceneId);
  syncArenaSelector();
  draw();
}

function selectAppearance(kind, id) {
  const next = normalizeArenaAppearance({ ...appearance, [kind]: id });
  if (next[kind] === appearance[kind]) return;
  appearance = next;
  storageSet("horseRunnerArenaAppearance", appearance);
  rebuildAppearanceSprites();
  syncAppearanceSelector();
  draw();
}

async function load() {
  const startButton = $("arena-start");
  const nextButton = $("arena-next");
  startButton.disabled = true;
  nextButton.disabled = true;
  startButton.textContent = "Preparing horses…";
  const defaults = await fetch("data/arena.json").then(response => response.json());
  setArenaData(storageGet("horseRunnerArenaDraftV2", defaults));
  selectArena(storageGet("horseRunnerSelectedArena", data.scenes[0].id));
  appearance = normalizeArenaAppearance(storageGet("horseRunnerArenaAppearance", DEFAULT_ARENA_APPEARANCE));
  await preloadAssets();
  rebuildAppearanceSprites();
  syncAppearanceSelector();
  startButton.disabled = false;
  nextButton.disabled = false;
  drawLeaderboard();
  draw();
  if (new URLSearchParams(location.search).has("autostart")) start();
}

function start() {
  scene = data.scenes.find(item => item.id === selectedSceneId) || data.scenes[0];
  const startPosition = scene.start;
  const floor = arenaFloor(scene);
  horse = { x: clamp(startPosition.x, floor.x + 11, floor.x + floor.width - 11), y: clamp(startPosition.y, floor.y + 14, floor.y + floor.height - 10), angle: startPosition.angle, z: 0, vz: 0, phase: 0, eating: 0, eatingAppleId: null, resumePace: 1 };
  lap = 0; checkpoint = 0; elapsed = 0; clearances = 0; faults = 0; touched = new Set(); paceLevel = 1; objectStates = new Map(); mode = "playing";
  $("arena-overlay").hidden = true; $("arena-name-card").hidden = true; updateHud(); syncHeaderControls(); canvas.focus();
}

function quitArena() {
  if (document.fullscreenElement) document.exitFullscreen?.();
  mode = "menu";
  keys.clear();
  horse = null;
  objectStates = new Map();
  $("arena-name-card").hidden = true;
  $("arena-overlay").hidden = false;
  syncArenaSelector();
  syncAppearanceSelector();
  showSelectionStep("partnership");
  syncHeaderControls();
  draw();
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
  const arenaView = document.body.dataset.view === "arena";
  $("arena-quit").hidden = !arenaView || mode === "menu";
  if (!arenaView) return;
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
  if (speed > 0) horse.phase += dt * (paceLevel === 2 ? 10 : 7);
  const floor = arenaFloor(scene);
  horse.x = clamp(horse.x + Math.cos(horse.angle) * speed * dt, floor.x + 11, floor.x + floor.width - 11);
  horse.y = clamp(horse.y + Math.sin(horse.angle) * speed * dt, floor.y + 14, floor.y + floor.height - 10);
  if (horse.z > 0 || horse.vz > 0) {
    horse.vz -= data.settings.gravity * dt;
    horse.z += horse.vz * dt;
    if (horse.z <= 0) { horse.z = 0; horse.vz = 0; }
  }
  for (const state of objectStates.values()) {
    if (state.fallen) state.fall = Math.min(1, (state.fall || 0) + dt * 4.5);
    if (state.rebuilding) state.rebuild = Math.min(1, (state.rebuild || 0) + dt * 3.2);
  }
  scene.objects.forEach(object => {
    const definition = ARENA_PROP_BY_TYPE.get(object.type);
    const visualState = objectStates.get(object.id);
    if (!definition || definition.behavior === "decor" || visualState?.consumed) return;
    const d = distance(horse, object);
    if (visualState?.fallen) {
      if (shouldRebuildObstacle(d, definition.radius, elapsed, visualState.rebuildAt)) {
        objectStates.set(object.id, { outcome: "rebuilding", rebuilding: true, rebuild: 0 });
      }
      return;
    }
    if (visualState?.rebuilding) return;
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
        if (JUMP_TYPES.includes(object.type)) objectStates.set(object.id, { outcome: "fallen", fallen: true, fall: 0, rebuildAt: elapsed + 1 });
      }
    }
    if (d > definition.radius + 6) touched.delete(object.id);
  });
  for (const [id, visualState] of objectStates) {
    if (visualState.outcome === "cleared" && elapsed > visualState.until) objectStates.delete(id);
    if (visualState.rebuilding && visualState.rebuild >= 1) { objectStates.delete(id); touched.delete(id); }
  }
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
  else showOverlay("Five laps complete!");
}

function showOverlay(title) { $("arena-overlay").hidden = false; syncAppearanceSelector(); showSelectionStep("partnership", title); }
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
  const floor = arenaFloor(scene);
  const perspective = Math.min(10, floor.height * .065);
  const grass = palette.grass || "#426b45";
  const grassGradient = ctx.createLinearGradient(0, 0, 0, 180);
  grassGradient.addColorStop(0, "#294a37"); grassGradient.addColorStop(.55, grass); grassGradient.addColorStop(1, "#52734b");
  ctx.fillStyle = grassGradient; ctx.fillRect(0, 0, 320, 180);
  for (let y = 0; y < 180; y += 8) rect(0, y, 320, 1, y % 16 ? "rgba(255,255,255,.025)" : "rgba(10,34,22,.12)");
  ctx.beginPath();
  ctx.moveTo(floor.x + perspective, floor.y); ctx.lineTo(floor.x + floor.width - perspective, floor.y);
  ctx.lineTo(floor.x + floor.width, floor.y + floor.height); ctx.lineTo(floor.x, floor.y + floor.height); ctx.closePath();
  ctx.lineWidth = 5; ctx.strokeStyle = "#6d4934"; ctx.stroke();
  const sandGradient = ctx.createLinearGradient(0, floor.y, 0, floor.y + floor.height);
  sandGradient.addColorStop(0, "#a97849"); sandGradient.addColorStop(.45, palette.sand || "#c99b62"); sandGradient.addColorStop(1, "#ddb77b");
  ctx.fillStyle = sandGradient; ctx.fill();
  ctx.save(); ctx.clip();
  for (let y = floor.y + 5; y < floor.y + floor.height; y += 7) {
    ctx.beginPath(); ctx.moveTo(floor.x, y); ctx.bezierCurveTo(110, y - 1.4, 215, y + 1.2, floor.x + floor.width, y);
    ctx.strokeStyle = y % 14 < 7 ? "rgba(91,57,35,.13)" : "rgba(255,238,190,.12)"; ctx.lineWidth = .7; ctx.stroke();
  }
  for (let x = floor.x + 12; x < floor.x + floor.width - 8; x += 17) for (let y = floor.y + 10; y < floor.y + floor.height - 6; y += 15) ellipse(x + (y % 5), y, 1.2, .35, "rgba(87,57,37,.16)");
  ctx.restore();
  const rail = palette.rail || "#fff0bd";
  ctx.beginPath(); ctx.moveTo(floor.x + perspective, floor.y); ctx.lineTo(floor.x + floor.width - perspective, floor.y); ctx.lineTo(floor.x + floor.width, floor.y + floor.height); ctx.lineTo(floor.x, floor.y + floor.height); ctx.closePath();
  ctx.strokeStyle = rail; ctx.lineWidth = 2.2; ctx.stroke();
  for (let index = 0; index <= 10; index++) {
    const t = index / 10, x = floor.x + perspective * (1 - t) + (floor.width - perspective * 2 * (1 - t)) * t;
    const y = floor.y + floor.height * t;
    if (index === 0 || index === 10) continue;
    ellipse(floor.x + perspective * (1 - t), y, 1.1, 2.6, "#ead8ae");
    ellipse(floor.x + floor.width - perspective * (1 - t), y, 1.1, 2.6, "#ead8ae");
  }
  for (let x = 14; x < 310; x += 14) { rect(x, 2, 8, 5, palette.crowd || "#693f55"); ellipse(x + 4, 2, 2, 2, ["#f1c27d", "#9a684f", "#d89b72"][x % 3]); }
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
  if (visualState?.fallen || visualState?.rebuilding) {
    const fall = visualState.rebuilding ? 1 - (visualState.rebuild || 0) : visualState.fall || 0;
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
  const runFrame = animationFrame(horse.phase, HORSE_RUN_FRAME_COUNT);
  const jumpFrame = jumpAnimationFrame(horse.z, horse.vz, data.settings.jumpPower);
  const image = eating ? activeHorseEatingSprites[index] : horse.z > 2 ? activeHorseJumpSprites[index]?.[jumpFrame] : activeHorseRunSprites[index]?.[runFrame];
  const bob = eating ? Math.sin(horse.phase * 1.7) * .25 : 0;
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
    selectionStep: mode === "menu" ? selectionStep : null,
    arena: { id: scene?.id || null, name: scene?.name || null, floor: scene ? arenaFloor(scene) : null },
    appearance: { horse: horseAppearance(appearance.horse).label, rider: riderAppearance(appearance.rider).label },
    horse: horse ? { x: +horse.x.toFixed(1), y: +horse.y.toFixed(1), z: +horse.z.toFixed(1), angle: +horse.angle.toFixed(2), direction, pace: horse.eating > 0 ? "Eating" : PACE_NAMES[paceLevel], speed: +paceSpeed(data.settings.baseSpeed, paceLevel).toFixed(1), eatingRemaining: +horse.eating.toFixed(1), eatingAppleId: horse.eatingAppleId, animation: horse.eating > 0 ? { kind: "eating", frame: 0, total: 1 } : horse.z > 2 ? { kind: "jump", frame: jumpAnimationFrame(horse.z, horse.vz, data.settings.jumpPower), total: HORSE_JUMP_FRAME_COUNT } : { kind: "canter", frame: animationFrame(horse.phase, HORSE_RUN_FRAME_COUNT), total: HORSE_RUN_FRAME_COUNT } } : null,
    lap: lap ?? 0, checkpoint: checkpoint ?? 0, elapsed: +(elapsed ?? 0).toFixed(1), clearances: clearances ?? 0, faults: faults ?? 0,
    objects: (scene?.objects || []).map(object => ({ id: object.id, type: object.type, x: object.x, y: object.y, height: object.height, outcome: objectStates.get(object.id)?.outcome || null }))
  });
}

window.arena_render_game_to_text = renderGameToText;
window.arena_advance_time = milliseconds => { const steps = Math.max(1, Math.round(milliseconds / (1000 / 60))); for (let i = 0; i < steps; i++) update(FIXED_STEP); draw(); };

$("arena-start").addEventListener("click", start);
$("arena-next").addEventListener("click", () => showSelectionStep("course"));
$("arena-back").addEventListener("click", () => showSelectionStep("partnership"));
$("arena-scene-select").addEventListener("change", event => selectArena(event.target.value));
$("arena-horse-options").addEventListener("click", event => { const button = event.target.closest("[data-horse]"); if (button) selectAppearance("horse", button.dataset.horse); });
$("arena-rider-options").addEventListener("click", event => { const button = event.target.closest("[data-rider]"); if (button) selectAppearance("rider", button.dataset.rider); });
$("arena-jump").addEventListener("pointerdown", jump);
$("pause").addEventListener("click", () => { if (document.body.dataset.view === "arena") togglePause(); });
$("restart").addEventListener("click", () => { if (document.body.dataset.view === "arena") start(); });
$("arena-quit").addEventListener("click", quitArena);
addEventListener("gameviewchange", syncHeaderControls);
$("arena-score-form").addEventListener("submit", event => { event.preventDefault(); const input = $("arena-player-name"), entry = { name: input.value.trim().slice(0, 16) || "Rider", score: Number(input.dataset.score), time: elapsed, clearances, faults }; storageSet("horseRunnerArenaScores", addScore(storageGet("horseRunnerArenaScores", []), entry)); input.value = ""; $("arena-name-card").hidden = true; drawLeaderboard(); showOverlay("Hall of fame!"); });
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

load().catch(error => { console.error(error); $("arena-start").textContent = "Assets failed to load"; $("arena-next").disabled = true; });
requestAnimationFrame(loop);
