import { getArenaData, setArenaData } from "./arena.js";
import { ARENA_PROP_BY_TYPE, ARENA_PROPS, propSpritePath } from "./arena-assets.js";
import { arenaFloor, propFacing, propFacingAngle, validateArena } from "./arena-core.js";

const $ = id => document.getElementById(id);
const canvas = $("editor-canvas");
const ctx = canvas.getContext("2d");
const propImages = new Map();
let data, sceneIndex = 0, selection = [], selectedCheckpoint = -1, editorMode = "objects", drag = null, undo = [], redo = [];
const scene = () => data.scenes[sceneIndex];

function ensureScene(value) {
  value.floor ||= { width: 280, height: 152 };
  value.start ||= { x: 160, y: 145, angle: -Math.PI / 2 };
  value.checkpoints ||= [];
  return value;
}

function loadImage(source) { return new Promise((resolve, reject) => { const image = new Image(); image.onload = () => resolve(image); image.onerror = reject; image.src = source; }); }
Promise.all(ARENA_PROPS.map(async prop => propImages.set(prop.type, await loadImage(propSpritePath(prop))))).then(() => { if (data) draw(); });

function snapshot() { undo.push(JSON.stringify(data)); if (undo.length > 40) undo.shift(); redo = []; }
function restore(stack, target) { if (!stack.length) return; target.push(JSON.stringify(data)); data = JSON.parse(stack.pop()); sceneIndex = Math.min(sceneIndex, data.scenes.length - 1); selection = []; render(); }
function open() { data = getArenaData(); data.scenes.forEach(ensureScene); sceneIndex = 0; selection = []; selectedCheckpoint = -1; editorMode = "objects"; undo = []; redo = []; $("editor").hidden = false; render(); }
function close() {
  document.activeElement?.blur();
  $("editor").hidden = true;
  const gameCanvas = document.body.dataset.view === "arena" ? $("arena-game") : $("game");
  requestAnimationFrame(() => gameCanvas?.focus());
}
function add(type) { const definition = ARENA_PROP_BY_TYPE.get(type); if (!definition) return; editorMode = "objects"; snapshot(); const id = `${type}-${Date.now()}`; scene().objects.push({ id, type, x: 160, y: 90, height: definition.behavior === "jump" ? 1 : 0, rotation: 0, facing: 0 }); selection = [id]; selectedCheckpoint = -1; render(); }
function selected() { return scene().objects.filter(object => selection.includes(object.id)); }

function initializePalette() {
  $("prop-palette").innerHTML = ARENA_PROPS.map(prop => `<button type="button" data-add-type="${prop.type}" title="Add ${prop.label}"><img src="${propSpritePath(prop)}" alt=""><span>${prop.label}</span></button>`).join("");
}

function render() {
  const currentScene = ensureScene(scene());
  $("scene-list").innerHTML = data.scenes.map((value, index) => `<option value="${index}" ${index === sceneIndex ? "selected" : ""}>${value.name}</option>`).join("");
  $("scene-name").value = currentScene.name;
  $("object-list").innerHTML = currentScene.objects.map(object => { const definition = ARENA_PROP_BY_TYPE.get(object.type); return `<button type="button" data-id="${object.id}" class="${selection.includes(object.id) ? "selected" : ""}">${definition?.label || object.type} · ${object.id}</button>`; }).join("");
  const one = selected()[0];
  const oneDefinition = one ? ARENA_PROP_BY_TYPE.get(one.type) : null;
  $("prop-id").value = one?.id || "";
  $("prop-height").value = one?.height ?? 0;
  $("prop-height").disabled = oneDefinition?.behavior !== "jump";
  document.querySelectorAll("#prop-facing").forEach(control => { control.value = one ? propFacing(one) : 0; control.disabled = editorMode !== "objects" || !one; });
  $("setting-laps").value = data.settings.laps; $("setting-speed").value = data.settings.baseSpeed; $("setting-jump").value = data.settings.jumpPower; $("setting-penalty").value = data.settings.hitPenalty;
  const floor = arenaFloor(currentScene);
  $("floor-width").value = floor.width; $("floor-height").value = floor.height; $("floor-size").textContent = `${floor.width} × ${floor.height} arena units`;
  document.querySelectorAll("[data-editor-mode]").forEach(button => button.classList.toggle("active", button.dataset.editorMode === editorMode));
  $("editor-hint").textContent = editorMode === "checkpoints" ? "Click to add ordered goal circles. Drag a circle to adjust the circuit." : editorMode === "start" ? "Click the floor to move the horse's start position." : "Choose a prop, then drag it into place. Shift-click for multiple selection.";
  $("delete-checkpoint").disabled = selectedCheckpoint < 0; $("clear-course").disabled = !currentScene.checkpoints.length;
  $("prop-id").disabled = editorMode !== "objects"; $("prop-height").disabled = editorMode !== "objects" || oneDefinition?.behavior !== "jump";
  $("arena-json").value = JSON.stringify(data, null, 2); $("undo").disabled = !undo.length; $("redo").disabled = !redo.length;
  draw();
}

function beginFrame() {
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.setTransform(canvas.width / 320, 0, 0, canvas.height / 180, 0, 0);
  ctx.imageSmoothingEnabled = true;
}

function draw() {
  if (!data) return;
  const currentScene = ensureScene(scene()), palette = currentScene.palette, floor = arenaFloor(currentScene), perspective = Math.min(10, floor.height * .065);
  beginFrame();
  const grass = ctx.createLinearGradient(0, 0, 0, 180); grass.addColorStop(0, "#294a37"); grass.addColorStop(.55, palette.grass); grass.addColorStop(1, "#52734b"); ctx.fillStyle = grass; ctx.fillRect(0, 0, 320, 180);
  ctx.beginPath(); ctx.moveTo(floor.x + perspective, floor.y); ctx.lineTo(floor.x + floor.width - perspective, floor.y); ctx.lineTo(floor.x + floor.width, floor.y + floor.height); ctx.lineTo(floor.x, floor.y + floor.height); ctx.closePath();
  ctx.lineWidth = 5; ctx.strokeStyle = "#6d4934"; ctx.stroke();
  const sand = ctx.createLinearGradient(0, floor.y, 0, floor.y + floor.height); sand.addColorStop(0, "#a97849"); sand.addColorStop(.45, palette.sand); sand.addColorStop(1, "#ddb77b"); ctx.fillStyle = sand; ctx.fill();
  ctx.strokeStyle = palette.rail; ctx.lineWidth = 2; ctx.stroke();
  if (currentScene.checkpoints.length) {
    ctx.save(); ctx.beginPath(); ctx.moveTo(currentScene.start.x, currentScene.start.y); currentScene.checkpoints.forEach(goal => ctx.lineTo(goal.x, goal.y)); ctx.strokeStyle = "rgba(255,250,210,.72)"; ctx.lineWidth = 1.4; ctx.setLineDash([4, 4]); ctx.stroke(); ctx.restore();
  }
  ctx.save(); ctx.translate(currentScene.start.x, currentScene.start.y); ctx.rotate(currentScene.start.angle || 0); ctx.fillStyle = "#77e6ff"; ctx.beginPath(); ctx.moveTo(7, 0); ctx.lineTo(-5, -4); ctx.lineTo(-5, 4); ctx.closePath(); ctx.fill(); ctx.restore();
  currentScene.checkpoints.forEach((goal, index) => { ctx.beginPath(); ctx.arc(goal.x, goal.y, index === selectedCheckpoint ? 7 : 5.5, 0, Math.PI * 2); ctx.fillStyle = index === selectedCheckpoint ? "rgba(255,191,55,.9)" : "rgba(255,248,202,.75)"; ctx.fill(); ctx.strokeStyle = "#713b38"; ctx.lineWidth = 1.2; ctx.stroke(); ctx.fillStyle = "#3b2b25"; ctx.font = "bold 5px sans-serif"; ctx.textAlign = "center"; ctx.textBaseline = "middle"; ctx.fillText(String(index + 1), goal.x, goal.y + .3); });
  [...currentScene.objects].sort((a, b) => a.y - b.y).forEach(object => {
    const definition = ARENA_PROP_BY_TYPE.get(object.type), image = propImages.get(object.type);
    if (!definition) return;
    const facing = propFacing(object), projection = facingProjection(definition.width, definition.height, facing);
    ctx.save(); ctx.translate(object.x, object.y);
    ctx.save(); ctx.rotate(projection.angle); ctx.beginPath(); ctx.ellipse(0, 1, definition.width * .36, Math.max(1.5, definition.height * .09), 0, 0, Math.PI * 2); ctx.fillStyle = "rgba(30,25,20,.22)"; ctx.fill(); ctx.restore();
    if (image) drawFacingImage(ctx, image, definition.width, definition.height, facing);
    else { ctx.fillStyle = "#f07b32"; ctx.fillRect(-4, -8, 8, 8); }
    if (selection.includes(object.id)) { ctx.strokeStyle = "#fff"; ctx.lineWidth = 1; ctx.setLineDash([2, 2]); ctx.strokeRect(-projection.width / 2 - 2, -definition.height - projection.depth - 2, projection.width + 4, definition.height + projection.depth + 5); }
    ctx.restore();
  });
}

function facingProjection(width, height, facing) {
  const angle = propFacingAngle(facing);
  return { angle, width: Math.max(3, Math.abs(Math.cos(angle)) * width), depth: Math.abs(Math.sin(angle)) * width * .42, height };
}

function drawFacingImage(context, image, width, height, facing) {
  const { angle } = facingProjection(width, height, facing), cosine = Math.cos(angle), sine = Math.sin(angle), slices = 48;
  for (let index = 0; index < slices; index++) {
    const sourceX = index * image.naturalWidth / slices, sourceWidth = image.naturalWidth / slices + .5;
    const unit = (index + .5) / slices - .5, projectedWidth = Math.max(.7, width / slices * Math.max(.18, Math.abs(cosine)));
    const x = unit * width * cosine, y = unit * width * sine * .42;
    context.drawImage(image, sourceX, 0, sourceWidth, image.naturalHeight, x - projectedWidth / 2, -height + y, projectedWidth, height);
  }
}

function point(event) { const bounds = canvas.getBoundingClientRect(); return { x: (event.clientX - bounds.left) * 320 / bounds.width, y: (event.clientY - bounds.top) * 180 / bounds.height }; }
canvas.addEventListener("pointerdown", event => {
  const position = point(event);
  const currentScene = ensureScene(scene());
  if (editorMode === "start") { snapshot(); const floor = arenaFloor(currentScene); constrainToFloor(position, floor); currentScene.start.x = position.x; currentScene.start.y = position.y; selectedCheckpoint = -1; render(); return; }
  if (editorMode === "checkpoints") {
    const checkpointIndex = currentScene.checkpoints.findIndex(goal => Math.hypot(goal.x - position.x, goal.y - position.y) < 8);
    if (checkpointIndex >= 0) { selectedCheckpoint = checkpointIndex; snapshot(); drag = { kind: "checkpoint", index: checkpointIndex }; canvas.setPointerCapture(event.pointerId); }
    else { snapshot(); constrainToFloor(position, arenaFloor(currentScene)); currentScene.checkpoints.push(position); selectedCheckpoint = currentScene.checkpoints.length - 1; }
    selection = []; render(); return;
  }
  const hit = [...scene().objects].reverse().find(object => Math.hypot(object.x - position.x, object.y - position.y) < (ARENA_PROP_BY_TYPE.get(object.type)?.radius || 10) + 4);
  if (!hit) { selection = []; render(); return; }
  if (event.shiftKey) selection = selection.includes(hit.id) ? selection.filter(id => id !== hit.id) : [...selection, hit.id];
  else if (!selection.includes(hit.id)) selection = [hit.id];
  snapshot(); drag = { kind: "objects", position, starts: selected().map(object => ({ object, x: object.x, y: object.y })) }; canvas.setPointerCapture(event.pointerId); render();
});
canvas.addEventListener("pointermove", event => { if (!drag) return; const position = point(event); if (drag.kind === "checkpoint") { constrainToFloor(position, arenaFloor(scene())); scene().checkpoints[drag.index] = position; } else drag.starts.forEach(value => { value.object.x = Math.max(4, Math.min(316, value.x + position.x - drag.position.x)); value.object.y = Math.max(8, Math.min(176, value.y + position.y - drag.position.y)); }); render(); });
canvas.addEventListener("pointerup", () => drag = null);

$("editor-open").addEventListener("click", open); $("editor-close").addEventListener("click", close);
$("editor").addEventListener("keydown", event => { if (!$("editor").hidden) event.stopPropagation(); });
$("prop-palette").addEventListener("click", event => { const button = event.target.closest("[data-add-type]"); if (button) add(button.dataset.addType); });
document.querySelector(".editor-mode-tabs").addEventListener("click", event => { const button = event.target.closest("[data-editor-mode]"); if (!button) return; editorMode = button.dataset.editorMode; selection = []; selectedCheckpoint = -1; render(); });
$("apply-settings").addEventListener("click", () => { snapshot(); data.settings.laps = Number($("setting-laps").value); data.settings.baseSpeed = Number($("setting-speed").value); data.settings.jumpPower = Number($("setting-jump").value); data.settings.hitPenalty = Number($("setting-penalty").value); render(); });
$("delete-object").addEventListener("click", () => { if (!selection.length) return; snapshot(); scene().objects = scene().objects.filter(object => !selection.includes(object.id)); selection = []; render(); });
$("object-list").addEventListener("click", event => { const id = event.target.dataset.id; if (id) { selection = event.shiftKey ? [...new Set([...selection, id])] : [id]; render(); } });
$("scene-list").addEventListener("change", event => { sceneIndex = Number(event.target.value); selection = []; selectedCheckpoint = -1; render(); });
$("scene-name").addEventListener("change", () => { const name = $("scene-name").value.trim(); if (!name) return render(); snapshot(); scene().name = name; render(); });
$("add-scene").addEventListener("click", () => { snapshot(); const copy = structuredClone(ensureScene(scene())); copy.id = `arena-${Date.now()}`; copy.name = `New arena ${data.scenes.length + 1}`; copy.objects = []; const floor = arenaFloor(copy); copy.start = { x: 160, y: floor.y + floor.height - 18, angle: -Math.PI / 2 }; copy.checkpoints = [{ x: 160, y: floor.y + 20 }, { x: 160, y: floor.y + floor.height - 20 }]; data.scenes.push(copy); sceneIndex = data.scenes.length - 1; render(); });
$("delete-scene").addEventListener("click", () => { if (data.scenes.length === 1) return; snapshot(); data.scenes.splice(sceneIndex, 1); sceneIndex = 0; render(); });
$("undo").addEventListener("click", () => restore(undo, redo)); $("redo").addEventListener("click", () => restore(redo, undo));
function constrainToFloor(point, floor) { point.x = Math.max(floor.x + 8, Math.min(floor.x + floor.width - 8, point.x)); point.y = Math.max(floor.y + 10, Math.min(floor.y + floor.height - 8, point.y)); }
$("floor-width").addEventListener("input", () => $("floor-size").textContent = `${$("floor-width").value} × ${$("floor-height").value} arena units`);
$("floor-height").addEventListener("input", () => $("floor-size").textContent = `${$("floor-width").value} × ${$("floor-height").value} arena units`);
$("apply-floor").addEventListener("click", () => { snapshot(); scene().floor = { width: Number($("floor-width").value), height: Number($("floor-height").value) }; const floor = arenaFloor(scene()); constrainToFloor(scene().start, floor); scene().checkpoints.forEach(goal => constrainToFloor(goal, floor)); render(); });
$("delete-checkpoint").addEventListener("click", () => { if (selectedCheckpoint < 0) return; snapshot(); scene().checkpoints.splice(selectedCheckpoint, 1); selectedCheckpoint = -1; render(); });
$("clear-course").addEventListener("click", () => { if (!scene().checkpoints.length) return; snapshot(); scene().checkpoints = []; selectedCheckpoint = -1; render(); });
function updateProperties() { const items = selected(); if (!items.length) return; snapshot(); items.forEach((object, index) => { const definition = ARENA_PROP_BY_TYPE.get(object.type); if (index === 0 && $("prop-id").value.trim()) object.id = $("prop-id").value.trim(); object.height = definition?.behavior === "jump" ? Number($("prop-height").value) : 0; object.facing = Number($("prop-facing").value); object.rotation = 0; }); selection = items.map(object => object.id); render(); }
function updateFacing(event) {
  const items = selected();
  if (!items.length) { $("editor-message").textContent = "Select a prop before changing its facing."; return; }
  const facing = Number(event.currentTarget.value);
  snapshot();
  items.forEach(object => { object.facing = facing; object.rotation = 0; });
  $("editor-message").textContent = `Facing changed to ${event.currentTarget.options[event.currentTarget.selectedIndex].text}.`;
  render();
}
$("prop-id").addEventListener("change", updateProperties); $("prop-height").addEventListener("change", updateProperties);
document.querySelectorAll("#prop-facing").forEach(control => control.addEventListener("input", updateFacing));
$("apply-json").addEventListener("click", () => { try { const parsed = JSON.parse($("arena-json").value), errors = validateArena(parsed); if (errors.length) throw new Error(errors.join("\n")); snapshot(); data = parsed; sceneIndex = 0; selection = []; $("editor-message").textContent = "JSON is valid."; render(); } catch (error) { $("editor-message").textContent = error.message; } });
$("export-json").addEventListener("click", () => { const anchor = document.createElement("a"); anchor.href = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: "application/json" })); anchor.download = "horse-runner-arena.json"; anchor.click(); URL.revokeObjectURL(anchor.href); });
$("import-json").addEventListener("change", async event => { $("arena-json").value = await event.target.files[0].text(); $("apply-json").click(); });
$("save-draft").addEventListener("click", () => { try { setArenaData(data); localStorage.setItem("horseRunnerArenaDraftV2", JSON.stringify(data)); $("editor-message").textContent = "Draft saved and ready to play."; } catch (error) { $("editor-message").textContent = error.message; } });

initializePalette();
