import {
  FIXED_STEP,
  GROUND,
  LOGICAL_HEIGHT as H,
  LOGICAL_WIDTH as W,
  clamp,
  horseHitbox,
  obstacleDelay,
  obstacleForScore,
  overlaps,
  scoreText
} from "./game-core.js";

const canvas = document.querySelector("#game");
const ctx = canvas.getContext("2d");
const ui = Object.fromEntries(["score", "best", "lives", "status", "start-card", "pause", "restart", "sound"].map(id => [id, document.getElementById(id)]));
const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;
const horseSprites = [];
const obstacleSprites = new Map();
let backgroundImage = null;
let audio;
let state = "title", last = 0, accumulator = 0, world = 0, score = 0, best = readBest(), lives = 3, spawnIn = 1.1, flash = 0;
let horse = newHorse();
let obstacles = [], dust = [];

function newHorse() { return { x: 58, y: GROUND - 27, w: 43, h: 27, vy: 0, grounded: true, invincible: 0, phase: 0 }; }
function readBest() { try { return Number(localStorage.getItem("horseRunnerBest") || 0); } catch { return 0; } }
function writeBest(value) { try { localStorage.setItem("horseRunnerBest", value); } catch {} }
function loadImage(source) { return new Promise((resolve, reject) => { const image = new Image(); image.onload = () => resolve(image); image.onerror = reject; image.src = source; }); }

async function preloadAssets() {
  const [background, ...images] = await Promise.all([
    loadImage("assets/runner/bramblewood-trail.png"),
    ...Array.from({ length: 8 }, (_, index) => loadImage(`assets/runner/horse/${String(index + 1).padStart(2, "0")}.png`)),
    loadImage("assets/arena/props/02.png"),
    loadImage("assets/arena/props/08.png"),
    loadImage("assets/arena/props/03.png")
  ]);
  backgroundImage = background;
  images.slice(0, 8).forEach((image, index) => horseSprites[index] = image);
  obstacleSprites.set("log", images[8]); obstacleSprites.set("stump", images[9]); obstacleSprites.set("fence", images[10]);
}

ui.best.textContent = scoreText(best);

function reset() {
  world = 0; score = 0; lives = 3; spawnIn = 1.1; obstacles = []; dust = []; flash = 0; horse = newHorse();
  state = "playing"; ui["start-card"].hidden = true; syncHeaderControls();
  ui.status.textContent = "The run has begun. Jump the woodland obstacles!"; canvas.focus(); updateUi(); beep(220, .05);
}

function jump() {
  if (state === "title" || state === "gameover") return reset();
  if (state !== "playing" || !horse.grounded) return;
  horse.vy = -112; horse.grounded = false; burst(horse.x + 12, GROUND - 2, 5); beep(420, .06);
}

function togglePause() {
  if (state !== "playing" && state !== "paused") return;
  state = state === "playing" ? "paused" : "playing";
  syncHeaderControls();
  ui.status.textContent = state === "paused" ? "Run paused." : "Back on the trail.";
}

function syncHeaderControls() {
  if (document.body.dataset.view !== "runner") return;
  ui.pause.textContent = state === "paused" ? "Resume" : "Pause";
  ui.pause.setAttribute("aria-pressed", String(state === "paused"));
}

function spawn() {
  const item = obstacleForScore(score);
  obstacles.push({ ...item, x: W + 10, y: GROUND - item.h, hit: false });
  spawnIn = obstacleDelay(score);
}

function burst(x, y, count = 8) {
  if (reducedMotion) return;
  for (let index = 0; index < count; index++) dust.push({ x, y, vx: -12 - Math.random() * 22, vy: -5 - Math.random() * 16, t: .35 + Math.random() * .25 });
}

function update(dt) {
  if (state !== "playing") return;
  world += dt * 34; score += dt * 18; horse.phase += dt * 12; horse.invincible = Math.max(0, horse.invincible - dt); flash = Math.max(0, flash - dt);
  if (!horse.grounded) {
    horse.vy += 260 * dt; horse.y += horse.vy * dt;
    if (horse.y >= GROUND - horse.h) { horse.y = GROUND - horse.h; horse.vy = 0; horse.grounded = true; burst(horse.x + 18, GROUND - 1); beep(150, .035); }
  }
  spawnIn -= dt; if (spawnIn <= 0) spawn();
  const speed = 55 + clamp(score / 70, 0, 28);
  for (const item of obstacles) {
    item.x -= speed * dt;
    const hitbox = horseHitbox(horse);
    if (!item.hit && horse.invincible <= 0 && overlaps(hitbox, item)) {
      item.hit = true; lives--; horse.invincible = 1.35; flash = .18; burst(item.x, item.y, 12); beep(80, .13);
      ui.status.textContent = lives ? "Ouch! Bramble is shaken, but still running." : "The trail wins this time.";
      if (!lives) endGame();
    }
  }
  obstacles = obstacles.filter(obstacle => obstacle.x > -45);
  for (const particle of dust) { particle.x += particle.vx * dt; particle.y += particle.vy * dt; particle.vy += 35 * dt; particle.t -= dt; }
  dust = dust.filter(particle => particle.t > 0);
  updateUi();
}

function endGame() {
  state = "gameover"; best = Math.max(best, Math.floor(score)); writeBest(best); ui.best.textContent = scoreText(best); ui["start-card"].hidden = false;
  ui["start-card"].querySelector(".ribbon").textContent = "Trail record · " + scoreText(score);
  ui["start-card"].querySelector("h2").textContent = "Ride again?";
  ui["start-card"].querySelector("p:not(.ribbon)").textContent = "A brave run through Bramblewood. The trail is ready whenever you are.";
  document.querySelector("#start").textContent = "Try again"; document.querySelector("#start").focus(); syncHeaderControls();
}

function updateUi() {
  ui.score.textContent = scoreText(score);
  ui.lives.textContent = Array.from({ length: 3 }, (_, index) => index < lives ? "♥" : "·").join(" ");
  ui.lives.setAttribute("aria-label", `${lives} ${lives === 1 ? "life" : "lives"}`);
}

function beginFrame() {
  ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.setTransform(canvas.width / W, 0, 0, canvas.height / H, 0, 0); ctx.imageSmoothingEnabled = true;
}
function rect(x, y, width, height, color) { ctx.fillStyle = color; ctx.fillRect(x, y, width, height); }
function ellipse(x, y, radiusX, radiusY, color) { ctx.beginPath(); ctx.ellipse(x, y, radiusX, radiusY, 0, 0, Math.PI * 2); ctx.fillStyle = color; ctx.fill(); }

function drawBackground() {
  if (backgroundImage) {
    const drift = (world * .18) % 28;
    ctx.drawImage(backgroundImage, drift, 0, backgroundImage.naturalWidth - drift * 2, backgroundImage.naturalHeight, 0, 0, W, H);
  } else rect(0, 0, W, H, "#6e9a94");
  const trailGradient = ctx.createLinearGradient(0, 122, 0, H);
  trailGradient.addColorStop(0, "rgba(108,72,39,.05)"); trailGradient.addColorStop(1, "rgba(42,28,19,.2)");
  rect(0, 121, W, H - 121, trailGradient);
  for (let x = -(world % 22); x < W; x += 22) { ellipse(x, 161, 4, .7, "rgba(54,39,25,.18)"); ellipse(x + 12, 134, 2, .45, "rgba(255,226,155,.15)"); }
}

function currentHorseFrame() {
  if (horse.grounded) return Math.floor(horse.phase * .45) % 4;
  const jumpHeight = GROUND - horse.h - horse.y;
  if (horse.vy < -55) return 4;
  if (horse.vy < 8 || jumpHeight > 17) return horse.vy < -12 ? 5 : 6;
  return 7;
}

function drawHorse() {
  const blink = horse.invincible > 0 && Math.floor(horse.invincible * 12) % 2 === 0;
  if (blink) return;
  const image = horseSprites[currentHorseFrame()];
  const baseline = horse.y + horse.h + 3;
  const airborne = !horse.grounded;
  ellipse(horse.x + 23, GROUND + 3, clamp(19 - (GROUND - baseline) * .15, 11, 19), 3.4, "rgba(28,24,19,.3)");
  if (image) ctx.drawImage(image, horse.x - 19, baseline - 76, 84, 84);
  else { rect(horse.x, horse.y, horse.w, horse.h, "#9b5237"); }
  if (!airborne && Math.sin(horse.phase) > .72) ellipse(horse.x + 7, GROUND + 1, 4, 1.2, "rgba(211,173,105,.45)");
}

function drawObstacle(obstacle) {
  const image = obstacleSprites.get(obstacle.kind);
  const sizes = { log: [39, 29], stump: [28, 22], fence: [41, 31] };
  const [width, height] = sizes[obstacle.kind] || [obstacle.w, obstacle.h];
  ellipse(obstacle.x + obstacle.w / 2, GROUND + 2, width * .36, 2.1, "rgba(30,24,17,.28)");
  if (image) ctx.drawImage(image, obstacle.x + obstacle.w / 2 - width / 2, GROUND - height, width, height);
  else rect(obstacle.x, obstacle.y, obstacle.w, obstacle.h, "#6b3e2e");
}

function draw() {
  beginFrame(); drawBackground(); obstacles.forEach(drawObstacle); dust.forEach(particle => rect(particle.x, particle.y, 2, 2, `rgba(225,190,125,${clamp(particle.t * 2, 0, 1)})`)); drawHorse();
  const hud = ctx.createLinearGradient(8, 5, 94, 22); hud.addColorStop(0, "rgba(17,31,35,.92)"); hud.addColorStop(1, "rgba(39,66,57,.88)");
  rect(8, 8, 86, 14, hud); rect(11, 17, 79, 2, "rgba(255,255,255,.12)"); rect(11, 17, clamp(79 - (score % 500) / 500 * 79, 0, 79), 2, "#f0bc3e");
  ctx.fillStyle = "#fff4cf"; ctx.font = "bold 6px Georgia,serif"; ctx.fillText("BRAMBLEWOOD TRAIL", 13, 14);
  if (state === "paused") { rect(0, 0, W, H, "rgba(10,16,20,.48)"); ctx.fillStyle = "#fff4cf"; ctx.font = "bold 15px Georgia,serif"; ctx.textAlign = "center"; ctx.fillText("TRAIL PAUSED", W / 2, 88); ctx.textAlign = "start"; }
  if (flash > 0) rect(0, 0, W, H, "rgba(255,226,170,.28)");
}

function loop(now) {
  const dt = Math.min((now - last) / 1000, .1) || 0; last = now; accumulator += dt;
  while (accumulator >= FIXED_STEP) { update(FIXED_STEP); accumulator -= FIXED_STEP; }
  draw(); requestAnimationFrame(loop);
}

function beep(frequency, duration) {
  if (ui.sound.getAttribute("aria-pressed") === "true") return;
  try { audio ??= new AudioContext(); const oscillator = audio.createOscillator(), gain = audio.createGain(); oscillator.type = "triangle"; oscillator.frequency.value = frequency; gain.gain.setValueAtTime(.025, audio.currentTime); gain.gain.exponentialRampToValueAtTime(.001, audio.currentTime + duration); oscillator.connect(gain).connect(audio.destination); oscillator.start(); oscillator.stop(audio.currentTime + duration); } catch {}
}

function renderRunnerToText() {
  return JSON.stringify({ coordinateSystem: "origin top-left; x increases right; y increases down", mode: state, score: Math.floor(score), lives, horse: { x: +horse.x.toFixed(1), y: +horse.y.toFixed(1), vy: +horse.vy.toFixed(1), grounded: horse.grounded, animationFrame: currentHorseFrame() }, obstacles: obstacles.map(obstacle => ({ kind: obstacle.kind, x: +obstacle.x.toFixed(1), y: obstacle.y, hit: obstacle.hit })) });
}

window.runner_render_game_to_text = renderRunnerToText;
window.runner_advance_time = milliseconds => { const steps = Math.max(1, Math.round(milliseconds / (1000 / 60))); for (let index = 0; index < steps; index++) update(FIXED_STEP); draw(); };

document.querySelector("#start").addEventListener("click", reset);
document.querySelector("#jump").addEventListener("pointerdown", event => { event.preventDefault(); jump(); });
canvas.addEventListener("pointerdown", jump);
ui.pause.addEventListener("click", () => { if (document.body.dataset.view === "runner") togglePause(); });
ui.restart.addEventListener("click", () => { if (document.body.dataset.view === "runner") reset(); });
ui.sound.addEventListener("click", () => { const muted = ui.sound.getAttribute("aria-pressed") !== "true"; ui.sound.setAttribute("aria-pressed", String(muted)); ui.sound.textContent = muted ? "♪ Off" : "♪ On"; });
addEventListener("gameviewchange", syncHeaderControls);
addEventListener("keydown", event => {
  if (document.body.dataset.view !== "runner") return;
  if (["Space", "ArrowUp", "KeyW"].includes(event.code)) { event.preventDefault(); jump(); }
  if (event.code === "KeyP" || event.code === "Escape") togglePause();
  if (event.code === "KeyF") { event.preventDefault(); if (!document.fullscreenElement) canvas.parentElement.requestFullscreen?.(); else document.exitFullscreen?.(); }
});
addEventListener("blur", () => { if (document.body.dataset.view === "runner" && state === "playing") togglePause(); });
document.addEventListener("visibilitychange", () => { if (document.hidden && state === "playing") togglePause(); });

preloadAssets().then(draw).catch(console.error);
requestAnimationFrame(loop);
