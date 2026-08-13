import { getArenaData, setArenaData } from "./arena.js";
import { ARENA_PROP_BY_TYPE, ARENA_PROPS, propSpritePath } from "./arena-assets.js";
import { validateArena } from "./arena-core.js";

const $ = id => document.getElementById(id);
const canvas = $("editor-canvas");
const ctx = canvas.getContext("2d");
const propImages = new Map();
let data, sceneIndex = 0, selection = [], drag = null, undo = [], redo = [];
const scene = () => data.scenes[sceneIndex];

function loadImage(source) { return new Promise((resolve, reject) => { const image = new Image(); image.onload = () => resolve(image); image.onerror = reject; image.src = source; }); }
Promise.all(ARENA_PROPS.map(async prop => propImages.set(prop.type, await loadImage(propSpritePath(prop))))).then(() => { if (data) draw(); });

function snapshot() { undo.push(JSON.stringify(data)); if (undo.length > 40) undo.shift(); redo = []; }
function restore(stack, target) { if (!stack.length) return; target.push(JSON.stringify(data)); data = JSON.parse(stack.pop()); sceneIndex = Math.min(sceneIndex, data.scenes.length - 1); selection = []; render(); }
function open() { data = getArenaData(); sceneIndex = 0; selection = []; undo = []; redo = []; $("editor").hidden = false; render(); }
function close() { $("editor").hidden = true; }
function add(type) { const definition = ARENA_PROP_BY_TYPE.get(type); if (!definition) return; snapshot(); const id = `${type}-${Date.now()}`; scene().objects.push({ id, type, x: 160, y: 90, height: definition.behavior === "jump" ? 1 : 0, rotation: 0 }); selection = [id]; render(); }
function selected() { return scene().objects.filter(object => selection.includes(object.id)); }

function initializePalette() {
  $("prop-palette").innerHTML = ARENA_PROPS.map(prop => `<button type="button" data-add-type="${prop.type}" title="Add ${prop.label}"><img src="${propSpritePath(prop)}" alt=""><span>${prop.label}</span></button>`).join("");
}

function render() {
  const currentScene = scene();
  $("scene-list").innerHTML = data.scenes.map((value, index) => `<option value="${index}" ${index === sceneIndex ? "selected" : ""}>${value.name}</option>`).join("");
  $("object-list").innerHTML = currentScene.objects.map(object => { const definition = ARENA_PROP_BY_TYPE.get(object.type); return `<button type="button" data-id="${object.id}" class="${selection.includes(object.id) ? "selected" : ""}">${definition?.label || object.type} · ${object.id}</button>`; }).join("");
  const one = selected()[0];
  const oneDefinition = one ? ARENA_PROP_BY_TYPE.get(one.type) : null;
  $("prop-id").value = one?.id || "";
  $("prop-height").value = one?.height ?? 0;
  $("prop-height").disabled = oneDefinition?.behavior !== "jump";
  $("prop-rotation").value = one ? Math.round(one.rotation * 180 / Math.PI) : 0;
  $("setting-laps").value = data.settings.laps; $("setting-speed").value = data.settings.baseSpeed; $("setting-jump").value = data.settings.jumpPower; $("setting-penalty").value = data.settings.hitPenalty;
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
  const currentScene = scene(), palette = currentScene.palette;
  beginFrame();
  ctx.fillStyle = palette.grass; ctx.fillRect(0, 0, 320, 180);
  ctx.fillStyle = "#6d4934"; ctx.fillRect(18, 12, 284, 156);
  ctx.fillStyle = palette.sand; ctx.fillRect(22, 16, 276, 148);
  ctx.strokeStyle = palette.rail; ctx.lineWidth = 4; ctx.strokeRect(20, 14, 280, 152);
  [...currentScene.objects].sort((a, b) => a.y - b.y).forEach(object => {
    const definition = ARENA_PROP_BY_TYPE.get(object.type), image = propImages.get(object.type);
    if (!definition) return;
    ctx.save(); ctx.translate(object.x, object.y); ctx.rotate(object.rotation || 0);
    ctx.beginPath(); ctx.ellipse(0, 1, definition.width * .36, Math.max(1.5, definition.height * .09), 0, 0, Math.PI * 2); ctx.fillStyle = "rgba(30,25,20,.22)"; ctx.fill();
    if (image) ctx.drawImage(image, -definition.width / 2, -definition.height, definition.width, definition.height);
    else { ctx.fillStyle = "#f07b32"; ctx.fillRect(-4, -8, 8, 8); }
    if (selection.includes(object.id)) { ctx.strokeStyle = "#fff"; ctx.lineWidth = 1; ctx.setLineDash([2, 2]); ctx.strokeRect(-definition.width / 2 - 2, -definition.height - 2, definition.width + 4, definition.height + 5); }
    ctx.restore();
  });
}

function point(event) { const bounds = canvas.getBoundingClientRect(); return { x: (event.clientX - bounds.left) * 320 / bounds.width, y: (event.clientY - bounds.top) * 180 / bounds.height }; }
canvas.addEventListener("pointerdown", event => {
  const position = point(event);
  const hit = [...scene().objects].reverse().find(object => Math.hypot(object.x - position.x, object.y - position.y) < (ARENA_PROP_BY_TYPE.get(object.type)?.radius || 10) + 4);
  if (!hit) { selection = []; render(); return; }
  if (event.shiftKey) selection = selection.includes(hit.id) ? selection.filter(id => id !== hit.id) : [...selection, hit.id];
  else if (!selection.includes(hit.id)) selection = [hit.id];
  snapshot(); drag = { position, starts: selected().map(object => ({ object, x: object.x, y: object.y })) }; canvas.setPointerCapture(event.pointerId); render();
});
canvas.addEventListener("pointermove", event => { if (!drag) return; const position = point(event); drag.starts.forEach(value => { value.object.x = Math.max(24, Math.min(296, value.x + position.x - drag.position.x)); value.object.y = Math.max(19, Math.min(161, value.y + position.y - drag.position.y)); }); render(); });
canvas.addEventListener("pointerup", () => drag = null);

$("editor-open").addEventListener("click", open); $("editor-close").addEventListener("click", close);
$("prop-palette").addEventListener("click", event => { const button = event.target.closest("[data-add-type]"); if (button) add(button.dataset.addType); });
$("apply-settings").addEventListener("click", () => { snapshot(); data.settings.laps = Number($("setting-laps").value); data.settings.baseSpeed = Number($("setting-speed").value); data.settings.jumpPower = Number($("setting-jump").value); data.settings.hitPenalty = Number($("setting-penalty").value); render(); });
$("delete-object").addEventListener("click", () => { if (!selection.length) return; snapshot(); scene().objects = scene().objects.filter(object => !selection.includes(object.id)); selection = []; render(); });
$("object-list").addEventListener("click", event => { const id = event.target.dataset.id; if (id) { selection = event.shiftKey ? [...new Set([...selection, id])] : [id]; render(); } });
$("scene-list").addEventListener("change", event => { sceneIndex = Number(event.target.value); selection = []; render(); });
$("add-scene").addEventListener("click", () => { snapshot(); const copy = structuredClone(scene()); copy.id = `scene-${Date.now()}`; copy.name = `New arena ${data.scenes.length + 1}`; copy.objects = []; data.scenes.push(copy); sceneIndex = data.scenes.length - 1; render(); });
$("delete-scene").addEventListener("click", () => { if (data.scenes.length === 1) return; snapshot(); data.scenes.splice(sceneIndex, 1); sceneIndex = 0; render(); });
$("undo").addEventListener("click", () => restore(undo, redo)); $("redo").addEventListener("click", () => restore(redo, undo));
function updateProperties() { const items = selected(); if (!items.length) return; snapshot(); items.forEach((object, index) => { const definition = ARENA_PROP_BY_TYPE.get(object.type); if (index === 0 && $("prop-id").value.trim()) object.id = $("prop-id").value.trim(); object.height = definition?.behavior === "jump" ? Number($("prop-height").value) : 0; object.rotation = Number($("prop-rotation").value) * Math.PI / 180; }); selection = items.map(object => object.id); render(); }
$("prop-id").addEventListener("change", updateProperties); $("prop-height").addEventListener("change", updateProperties); $("prop-rotation").addEventListener("change", updateProperties);
$("apply-json").addEventListener("click", () => { try { const parsed = JSON.parse($("arena-json").value), errors = validateArena(parsed); if (errors.length) throw new Error(errors.join("\n")); snapshot(); data = parsed; sceneIndex = 0; selection = []; $("editor-message").textContent = "JSON is valid."; render(); } catch (error) { $("editor-message").textContent = error.message; } });
$("export-json").addEventListener("click", () => { const anchor = document.createElement("a"); anchor.href = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: "application/json" })); anchor.download = "horse-runner-arena.json"; anchor.click(); URL.revokeObjectURL(anchor.href); });
$("import-json").addEventListener("change", async event => { $("arena-json").value = await event.target.files[0].text(); $("apply-json").click(); });
$("save-draft").addEventListener("click", () => { try { setArenaData(data); localStorage.setItem("horseRunnerArenaDraftV2", JSON.stringify(data)); $("editor-message").textContent = "Draft saved and ready to play."; } catch (error) { $("editor-message").textContent = error.message; } });

initializePalette();
