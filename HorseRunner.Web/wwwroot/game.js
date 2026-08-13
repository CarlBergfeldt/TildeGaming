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
ctx.imageSmoothingEnabled = false;
const ui = Object.fromEntries(["score","best","lives","status","start-card","pause","restart","sound"].map(id => [id, document.getElementById(id)]));
const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;
let audio;
function readBest() { try { return Number(localStorage.getItem("horseRunnerBest") || 0); } catch { return 0; } }
function writeBest(value) { try { localStorage.setItem("horseRunnerBest", value); } catch { /* Storage is optional. */ } }
let state = "title", last = 0, accumulator = 0, world = 0, score = 0, best = readBest(), lives = 3, spawnIn = 1.1, flash = 0;
let horse = { x:58, y:GROUND-27, w:43, h:27, vy:0, grounded:true, invincible:0, phase:0 };
let obstacles = [], dust = [];
ui.best.textContent = scoreText(best);

function reset() {
  world=0; score=0; lives=3; spawnIn=1.1; obstacles=[]; dust=[]; flash=0;
  horse={x:58,y:GROUND-27,w:43,h:27,vy:0,grounded:true,invincible:0,phase:0};
  state="playing"; ui["start-card"].hidden=true; ui.pause.textContent="Pause"; ui.pause.setAttribute("aria-pressed","false");
  ui.status.textContent="The run has begun. Jump the woodland obstacles!"; canvas.focus(); updateUi(); beep(220,.05);
}
function jump() {
  if (state === "title" || state === "gameover") return reset();
  if (state !== "playing" || !horse.grounded) return;
  horse.vy=-112; horse.grounded=false; burst(horse.x+12,GROUND-2,5); beep(420,.06);
}
function togglePause() {
  if (state !== "playing" && state !== "paused") return;
  state=state === "playing" ? "paused" : "playing";
  ui.pause.textContent=state === "paused" ? "Resume" : "Pause";
  ui.pause.setAttribute("aria-pressed",String(state === "paused"));
  ui.status.textContent=state === "paused" ? "Run paused." : "Back on the trail.";
}
function spawn() {
  const item=obstacleForScore(score);
  obstacles.push({...item,x:W+10,y:GROUND-item.h,hit:false});
  spawnIn=obstacleDelay(score);
}
function burst(x,y,count=8) { if(reducedMotion)return; for(let i=0;i<count;i++) dust.push({x,y,vx:-12-Math.random()*22,vy:-5-Math.random()*16,t:.35+Math.random()*.25}); }
function update(dt) {
  if(state!=="playing") return;
  world+=dt*34; score+=dt*18; horse.phase+=dt*12; horse.invincible=Math.max(0,horse.invincible-dt); flash=Math.max(0,flash-dt);
  if(!horse.grounded){ horse.vy+=260*dt; horse.y+=horse.vy*dt; if(horse.y>=GROUND-horse.h){horse.y=GROUND-horse.h;horse.vy=0;horse.grounded=true;burst(horse.x+18,GROUND-1);beep(150,.035);} }
  spawnIn-=dt; if(spawnIn<=0) spawn();
  const speed=55+clamp(score/70,0,28);
  for(const item of obstacles){ item.x-=speed*dt; const hb=horseHitbox(horse); if(!item.hit && horse.invincible<=0 && overlaps(hb,item)){item.hit=true;lives--;horse.invincible=1.35;flash=.18;burst(item.x,item.y,12);beep(80,.13);ui.status.textContent=lives?"Ouch! Bramble is shaken, but still running.":"The trail wins this time."; if(!lives) endGame();} }
  obstacles=obstacles.filter(o=>o.x>-35);
  for(const p of dust){p.x+=p.vx*dt;p.y+=p.vy*dt;p.vy+=35*dt;p.t-=dt;} dust=dust.filter(p=>p.t>0);
  updateUi();
}
function endGame(){ state="gameover"; best=Math.max(best,Math.floor(score));writeBest(best);ui.best.textContent=scoreText(best);ui["start-card"].hidden=false;ui["start-card"].querySelector(".ribbon").textContent="Trail record · "+scoreText(score);ui["start-card"].querySelector("h2").textContent="Ride again?";ui["start-card"].querySelector("p:not(.ribbon)").textContent="A brave run through Bramblewood. The trail is ready whenever you are.";document.querySelector("#start").textContent="Try again";document.querySelector("#start").focus(); }
function updateUi(){ui.score.textContent=scoreText(score);ui.lives.textContent=Array.from({length:3},(_,i)=>i<lives?"♥":"·").join(" ");ui.lives.setAttribute("aria-label",`${lives} ${lives===1?"life":"lives"}`);}

function rect(x,y,w,h,c){ctx.fillStyle=c;ctx.fillRect(Math.round(x),Math.round(y),w,h);}
function polygon(points,c){ctx.fillStyle=c;ctx.beginPath();ctx.moveTo(points[0][0],points[0][1]);points.slice(1).forEach(p=>ctx.lineTo(p[0],p[1]));ctx.closePath();ctx.fill();}
function drawBackground(){
  rect(0,0,W,H,"#6e9a94"); for(let y=0;y<75;y+=3) rect(0,y,W,1,y%6?"#75a19a":"#719b96");
  rect(244,19,19,19,"#f2cf78");rect(247,16,13,25,"#f2cf78");
  const far=-(world*.12)%72; for(let x=far-72;x<W+72;x+=72){polygon([[x,91],[x+25,54],[x+46,75],[x+72,46],[x+72,106],[x,106]],"#496f68");polygon([[x+25,54],[x+35,69],[x+29,66]],"#b5b68c");}
  const trees=-(world*.28)%48; for(let x=trees-48;x<W+48;x+=48){rect(x+20,70,5,57,"#574b3d");polygon([[x+22,42],[x+4,91],[x+42,91]],"#2e5a4b");polygon([[x+22,59],[x+1,105],[x+44,105]],"#356551");}
  rect(0,119,W,23,"#517343");for(let x=-(world*.55)%17;x<W;x+=17){rect(x,126,10,2,"#6f8d4f");rect(x+4,122,2,5,"#86a95c");}
  rect(0,GROUND,W,H-GROUND,"#80583b");rect(0,GROUND,W,3,"#c19457");for(let x=-(world%14);x<W;x+=14){rect(x,151,7,2,"#66422f");rect(x+6,167,10,2,"#966944");}
}
function drawHorse(){
  const blink=horse.invincible>0&&Math.floor(horse.invincible*12)%2===0;if(blink)return;
  const bob=horse.grounded?Math.round(Math.sin(horse.phase)*1.2):0,x=horse.x,y=horse.y+bob;
  rect(x+5,GROUND-2,37,2,"rgba(28,30,30,.35)");
  const leg=horse.grounded?Math.round(Math.sin(horse.phase)*4):2;
  rect(x+12,y+20,5,9+leg,"#5a3029");rect(x+31,y+20,5,9-leg,"#5a3029");rect(x+11,y+27+leg,8,3,"#271f25");rect(x+30,y+27-leg,8,3,"#271f25");
  rect(x+7,y+7,29,17,"#9b5237");rect(x+4,y+10,8,11,"#bd6841");rect(x+31,y+3,9,15,"#a85a3a");rect(x+37,y,7,11,"#bd6841");rect(x+41,y+2,2,2,"#191923");rect(x+39,y-3,3,5,"#713a30");polygon([[x+9,y+9],[x,y+5],[x+5,y+16]],"#56302c");
  rect(x+19,y,10,11,"#355276");rect(x+21,y-6,7,7,"#d69b73");rect(x+20,y-9,9,4,"#78402f");rect(x+25,y+8,4,10,"#e2b47d");
}
function drawObstacle(o){if(o.kind==="log"){rect(o.x,o.y+3,o.w,o.h-3,"#6b3e2e");rect(o.x+2,o.y+1,o.w-5,4,"#9a6841");rect(o.x+o.w-5,o.y+4,5,8,"#c09157");rect(o.x+o.w-3,o.y+6,2,4,"#745038");}else if(o.kind==="stump"){rect(o.x+3,o.y,o.w-6,o.h,"#71432e");rect(o.x,o.y,o.w,5,"#ae7d4d");rect(o.x+2,o.y+2,o.w-4,1,"#6d4b35");}else{rect(o.x+2,o.y,3,o.h,"#eee0a2");rect(o.x+o.w-5,o.y,3,o.h,"#eee0a2");rect(o.x,o.y+5,o.w,4,"#b74d3d");rect(o.x,o.y+13,o.w,4,"#f0dc96");}}
function draw(){drawBackground();obstacles.forEach(drawObstacle);dust.forEach(p=>rect(p.x,p.y,2,2,"#d3ad69"));drawHorse();rect(8,8,82,12,"#172536");rect(10,10,clamp(78-(score%500)/500*78,0,78),8,"#f0bc3e");ctx.fillStyle="#fff0bd";ctx.font="7px monospace";ctx.fillText("BRAMBLEWOOD",11,6);if(state==="paused"){rect(0,0,W,H,"rgba(10,16,25,.65)");ctx.fillStyle="#fff0bd";ctx.font="bold 16px monospace";ctx.textAlign="center";ctx.fillText("TRAIL PAUSED",W/2,88);ctx.textAlign="start";}if(flash>0)rect(0,0,W,H,"rgba(255,226,170,.28)");}
function loop(now){const dt=Math.min((now-last)/1000,.1)||0;last=now;accumulator+=dt;while(accumulator>=FIXED_STEP){update(FIXED_STEP);accumulator-=FIXED_STEP;}draw();requestAnimationFrame(loop);}
function beep(freq,duration){if(ui.sound.getAttribute("aria-pressed")==="true")return;try{audio??=new AudioContext();const osc=audio.createOscillator(),gain=audio.createGain();osc.type="square";osc.frequency.value=freq;gain.gain.setValueAtTime(.025,audio.currentTime);gain.gain.exponentialRampToValueAtTime(.001,audio.currentTime+duration);osc.connect(gain).connect(audio.destination);osc.start();osc.stop(audio.currentTime+duration);}catch{}}
document.querySelector("#start").addEventListener("click",reset);document.querySelector("#jump").addEventListener("pointerdown",e=>{e.preventDefault();jump();});canvas.addEventListener("pointerdown",jump);ui.pause.addEventListener("click",togglePause);ui.restart.addEventListener("click",reset);ui.sound.addEventListener("click",()=>{const muted=ui.sound.getAttribute("aria-pressed")!=="true";ui.sound.setAttribute("aria-pressed",String(muted));ui.sound.textContent=muted?"♪ Off":"♪ On";});
addEventListener("keydown",e=>{if(document.body.dataset.view!=="runner")return;if(["Space","ArrowUp","KeyW"].includes(e.code)){e.preventDefault();jump();}if(e.code==="KeyP"||e.code==="Escape")togglePause();});addEventListener("blur",()=>{if(state==="playing")togglePause();});
document.addEventListener("visibilitychange",()=>{if(document.hidden&&state==="playing")togglePause();});
requestAnimationFrame(loop);
