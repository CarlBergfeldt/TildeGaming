import { FIXED_STEP, clamp } from "./game-core.js";
import { addScore, distance, finalScore, jumpClearance, qualifies, validateArena } from "./arena-core.js";

const canvas = document.querySelector("#arena-game"), ctx = canvas.getContext("2d");
ctx.imageSmoothingEnabled = false;
const $ = id => document.getElementById(id);
const keys = new Set();
let data, scene, mode = "menu", last = 0, accumulator = 0, horse, lap, checkpoint, elapsed, clearances, faults, touched;

function storageGet(key, fallback) { try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; } }
function storageSet(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch {} }
export function getArenaData() { return structuredClone(data); }
export function setArenaData(next) { const errors=validateArena(next); if(errors.length) throw new Error(errors.join("\n")); data=structuredClone(next); scene=data.scenes[0]; }

async function load() {
  const defaults = await fetch("data/arena.json").then(response => response.json());
  setArenaData(storageGet("horseRunnerArenaDraft", defaults));
  drawLeaderboard(); draw();
}

function start() {
  scene=data.scenes[0]; const s=scene.start;
  horse={x:s.x,y:s.y,angle:s.angle,z:0,vz:0};lap=0;checkpoint=0;elapsed=0;clearances=0;faults=0;touched=new Set();mode="playing";
  $("arena-overlay").hidden=true; $("arena-name-card").hidden=true; updateHud(); canvas.focus();
}
function jump(){if(mode==="playing"&&horse.z===0)horse.vz=data.settings.jumpPower;}
function update(dt){
  if(mode!=="playing")return;
  elapsed+=dt; const steer=(keys.has("ArrowRight")||keys.has("KeyD")?1:0)-(keys.has("ArrowLeft")||keys.has("KeyA")?1:0);
  const throttle=keys.has("ArrowUp")||keys.has("KeyW")?1:keys.has("ArrowDown")||keys.has("KeyS")?-.45:0;
  horse.angle+=steer*data.settings.turnSpeed*dt; const speed=data.settings.baseSpeed*(1+throttle*.35);
  horse.x=clamp(horse.x+Math.cos(horse.angle)*speed*dt,31,289);horse.y=clamp(horse.y+Math.sin(horse.angle)*speed*dt,27,153);
  if(horse.z>0||horse.vz>0){horse.vz-=data.settings.gravity*dt;horse.z+=horse.vz*dt;if(horse.z<=0){horse.z=0;horse.vz=0;}}
  scene.objects.forEach(o=>{const d=distance(horse,o);if(d<9&&!touched.has(o.id)){touched.add(o.id);if(o.type==="jump"&&jumpClearance(horse.z,o.height)){clearances++;}else{faults++;elapsed+=data.settings.hitPenalty;}}if(d>14)touched.delete(o.id);});
  const target=scene.checkpoints[checkpoint];if(distance(horse,target)<18){checkpoint=(checkpoint+1)%scene.checkpoints.length;if(checkpoint===0){lap++;if(lap>=data.settings.laps)finish();}}
  updateHud();
}
function finish(){mode="finished";const score=finalScore({elapsed,clearances,faults,laps:lap});$("arena-result").textContent=`${score} points · ${elapsed.toFixed(1)}s · ${clearances} clear · ${faults} faults`;const scores=storageGet("horseRunnerArenaScores",[]);if(qualifies(scores,score)){$("arena-name-card").hidden=false;$("arena-player-name").dataset.score=score;$("arena-player-name").focus();}else showOverlay("Five laps complete!","Ride again");}
function showOverlay(title,button){$("arena-title").textContent=title;$("arena-start").textContent=button;$("arena-overlay").hidden=false;}
function updateHud(){$("arena-lap").textContent=`${Math.min(lap+1,data.settings.laps)}/${data.settings.laps}`;$("arena-time").textContent=elapsed.toFixed(1);$("arena-faults").textContent=faults;}
function drawLeaderboard(){const scores=storageGet("horseRunnerArenaScores",[]);$("arena-scores").innerHTML=scores.length?scores.map((s,i)=>`<li><span>${i+1}. ${escapeHtml(s.name)}</span><strong>${s.score}</strong></li>`).join(""):"<li>No champions yet</li>";}
function escapeHtml(value){const span=document.createElement("span");span.textContent=value;return span.innerHTML;}
function rect(x,y,w,h,c){ctx.fillStyle=c;ctx.fillRect(Math.round(x),Math.round(y),w,h);}
function draw(){
  const p=scene?.palette||{};rect(0,0,320,180,p.grass||"#426b45");rect(18,12,284,156,"#7b553e");rect(23,17,274,146,p.sand||"#c99b62");
  for(let x=26;x<296;x+=8)for(let y=20;y<160;y+=8)if((x+y)%16===0)rect(x,y,2,1,"#b78652");
  rect(18,12,284,5,p.rail||"#fff0bd");rect(18,158,284,5,p.rail||"#fff0bd");rect(18,17,5,141,p.rail||"#fff0bd");rect(297,17,5,141,p.rail||"#fff0bd");
  for(let x=25;x<300;x+=12){rect(x,4,8,6,p.crowd||"#693f55");rect(x+2,2,4,3,["#f1c27d","#9a684f","#d89b72"][x%3]);}
  scene?.objects.forEach(o=>{if(o.type==="cone"){rect(o.x-3,o.y-3,6,6,"#f07b32");rect(o.x-4,o.y+2,8,2,"#fff0bd");}else{ctx.save();ctx.translate(o.x,o.y);ctx.rotate(o.rotation);rect(-10,-3,20,3,["#78aadb","#f0bc3e","#e85d4a"][o.height-1]);rect(-9,-6,2,12,"#fff0bd");rect(7,-6,2,12,"#fff0bd");ctx.restore();}});
  if(horse){ctx.save();ctx.translate(horse.x,horse.y-horse.z);ctx.rotate(horse.angle);rect(-8,-4,15,8,"#9b5237");rect(4,-6,7,6,"#bd6841");rect(-2,-8,5,5,"#355276");rect(-6,4,3,5,"#3b2927");rect(4,4,3,5,"#3b2927");ctx.restore();rect(horse.x-7,horse.y+5,15,2,"rgba(20,20,20,.3)");}
  requestAnimationFrame(draw);
}
function loop(now){const dt=Math.min((now-last)/1000,.1)||0;last=now;accumulator+=dt;while(accumulator>=FIXED_STEP){update(FIXED_STEP);accumulator-=FIXED_STEP;}requestAnimationFrame(loop);}

$("arena-start").addEventListener("click",start);$("arena-jump").addEventListener("pointerdown",jump);
$("arena-score-form").addEventListener("submit",e=>{e.preventDefault();const input=$("arena-player-name"),entry={name:input.value.trim().slice(0,16)||"Rider",score:Number(input.dataset.score),time:elapsed,clearances,faults};storageSet("horseRunnerArenaScores",addScore(storageGet("horseRunnerArenaScores",[]),entry));input.value="";$("arena-name-card").hidden=true;drawLeaderboard();showOverlay("Hall of fame!","Ride again");});
addEventListener("keydown",e=>{if(document.body.dataset.view!=="arena")return;if(["Space"].includes(e.code)){e.preventDefault();jump();}keys.add(e.code);});addEventListener("keyup",e=>keys.delete(e.code));
document.addEventListener("visibilitychange",()=>{if(document.hidden&&mode==="playing")mode="paused";else if(!document.hidden&&mode==="paused")mode="playing";});
load();requestAnimationFrame(loop);
