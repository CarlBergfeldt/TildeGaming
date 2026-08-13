import { getArenaData, setArenaData } from "./arena.js";
import { validateArena } from "./arena-core.js";

const $=id=>document.getElementById(id), canvas=$("editor-canvas"),ctx=canvas.getContext("2d");
let data,sceneIndex=0,selection=[],drag=null,undo=[],redo=[];
const scene=()=>data.scenes[sceneIndex];
function snapshot(){undo.push(JSON.stringify(data));if(undo.length>40)undo.shift();redo=[];}
function restore(stack,target){if(!stack.length)return;target.push(JSON.stringify(data));data=JSON.parse(stack.pop());sceneIndex=Math.min(sceneIndex,data.scenes.length-1);selection=[];render();}
function open(){data=getArenaData();sceneIndex=0;selection=[];undo=[];redo=[];$("editor").hidden=false;render();}
function close(){$("editor").hidden=true;}
function add(type){snapshot();const id=`${type}-${Date.now()}`;scene().objects.push({id,type,x:160,y:90,height:type==="jump"?1:0,rotation:0});selection=[id];render();}
function selected(){return scene().objects.filter(o=>selection.includes(o.id));}
function render(){
  const s=scene();$("scene-list").innerHTML=data.scenes.map((v,i)=>`<option value="${i}" ${i===sceneIndex?"selected":""}>${v.name}</option>`).join("");
  $("object-list").innerHTML=s.objects.map(o=>`<button type="button" data-id="${o.id}" class="${selection.includes(o.id)?"selected":""}">${o.type} · ${o.id}</button>`).join("");
  const one=selected()[0];$("prop-id").value=one?.id||"";$("prop-height").value=one?.height??0;$("prop-rotation").value=one?Math.round(one.rotation*180/Math.PI):0;
  $("setting-laps").value=data.settings.laps;$("setting-speed").value=data.settings.baseSpeed;$("setting-jump").value=data.settings.jumpPower;$("setting-penalty").value=data.settings.hitPenalty;
  $("arena-json").value=JSON.stringify(data,null,2);$("undo").disabled=!undo.length;$("redo").disabled=!redo.length;draw();
}
function draw(){const s=scene(),p=s.palette;ctx.fillStyle=p.grass;ctx.fillRect(0,0,320,180);ctx.fillStyle=p.sand;ctx.fillRect(22,17,276,146);ctx.strokeStyle=p.rail;ctx.lineWidth=4;ctx.strokeRect(20,15,280,150);s.objects.forEach(o=>{ctx.save();ctx.translate(o.x,o.y);ctx.rotate(o.rotation);ctx.fillStyle=o.type==="cone"?"#f07b32":["#78aadb","#f0bc3e","#e85d4a"][o.height-1];ctx.fillRect(o.type==="cone"?-4:-11,-4,o.type==="cone"?8:22,8);if(selection.includes(o.id)){ctx.strokeStyle="#fff";ctx.lineWidth=1;ctx.strokeRect(-13,-7,26,14);}ctx.restore();});}
function point(e){const r=canvas.getBoundingClientRect();return{x:(e.clientX-r.left)*320/r.width,y:(e.clientY-r.top)*180/r.height};}
canvas.addEventListener("pointerdown",e=>{const p=point(e),hit=[...scene().objects].reverse().find(o=>Math.hypot(o.x-p.x,o.y-p.y)<14);if(!hit){selection=[];render();return;}if(e.shiftKey)selection=selection.includes(hit.id)?selection.filter(id=>id!==hit.id):[...selection,hit.id];else if(!selection.includes(hit.id))selection=[hit.id];snapshot();drag={p,starts:selected().map(o=>({o,x:o.x,y:o.y}))};canvas.setPointerCapture(e.pointerId);render();});
canvas.addEventListener("pointermove",e=>{if(!drag)return;const p=point(e);drag.starts.forEach(v=>{v.o.x=Math.max(24,Math.min(296,v.x+p.x-drag.p.x));v.o.y=Math.max(19,Math.min(161,v.y+p.y-drag.p.y));});render();});canvas.addEventListener("pointerup",()=>drag=null);
$("editor-open").addEventListener("click",open);$("editor-close").addEventListener("click",close);$("add-jump").addEventListener("click",()=>add("jump"));$("add-cone").addEventListener("click",()=>add("cone"));
$("apply-settings").addEventListener("click",()=>{snapshot();data.settings.laps=Number($("setting-laps").value);data.settings.baseSpeed=Number($("setting-speed").value);data.settings.jumpPower=Number($("setting-jump").value);data.settings.hitPenalty=Number($("setting-penalty").value);render();});
$("delete-object").addEventListener("click",()=>{if(!selection.length)return;snapshot();scene().objects=scene().objects.filter(o=>!selection.includes(o.id));selection=[];render();});
$("object-list").addEventListener("click",e=>{const id=e.target.dataset.id;if(id){selection=e.shiftKey?[...new Set([...selection,id])]:[id];render();}});
$("scene-list").addEventListener("change",e=>{sceneIndex=Number(e.target.value);selection=[];render();});
$("add-scene").addEventListener("click",()=>{snapshot();const copy=structuredClone(scene());copy.id=`scene-${Date.now()}`;copy.name=`New arena ${data.scenes.length+1}`;copy.objects=[];data.scenes.push(copy);sceneIndex=data.scenes.length-1;render();});
$("delete-scene").addEventListener("click",()=>{if(data.scenes.length===1)return;snapshot();data.scenes.splice(sceneIndex,1);sceneIndex=0;render();});
$("undo").addEventListener("click",()=>restore(undo,redo));$("redo").addEventListener("click",()=>restore(redo,undo));
function props(){const items=selected();if(!items.length)return;snapshot();items.forEach((o,i)=>{if(i===0&&$("prop-id").value.trim())o.id=$("prop-id").value.trim();o.height=o.type==="cone"?0:Number($("prop-height").value);o.rotation=Number($("prop-rotation").value)*Math.PI/180;});selection=items.map(o=>o.id);render();}
$("prop-id").addEventListener("change",props);$("prop-height").addEventListener("change",props);$("prop-rotation").addEventListener("change",props);
$("apply-json").addEventListener("click",()=>{try{const parsed=JSON.parse($("arena-json").value),errors=validateArena(parsed);if(errors.length)throw new Error(errors.join("\n"));snapshot();data=parsed;sceneIndex=0;selection=[];$("editor-message").textContent="JSON is valid.";render();}catch(error){$("editor-message").textContent=error.message;}});
$("export-json").addEventListener("click",()=>{const a=document.createElement("a");a.href=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:"application/json"}));a.download="horse-runner-arena.json";a.click();URL.revokeObjectURL(a.href);});
$("import-json").addEventListener("change",async e=>{$("arena-json").value=await e.target.files[0].text();$("apply-json").click();});
$("save-draft").addEventListener("click",()=>{try{setArenaData(data);localStorage.setItem("horseRunnerArenaDraft",JSON.stringify(data));$("editor-message").textContent="Draft saved and ready to play.";}catch(error){$("editor-message").textContent=error.message;}});
