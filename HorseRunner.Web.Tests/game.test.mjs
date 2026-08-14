import test from "node:test";
import assert from "node:assert/strict";

test("gameplay helpers remain deterministic", async () => {
  const { clamp, overlaps, scoreText } = await import("../HorseRunner.Web/wwwroot/game-core.js");
  assert.equal(clamp(12, 0, 10), 10);
  assert.equal(scoreText(42.9), "00042");
  assert.equal(overlaps({x:0,y:0,w:10,h:10},{x:9,y:9,w:2,h:2}), true);
  assert.equal(overlaps({x:0,y:0,w:10,h:10},{x:10,y:0,w:2,h:2}), false);
});

test("obstacle progression and timing are bounded", async () => {
  const { obstacleDelay, obstacleForScore } = await import("../HorseRunner.Web/wwwroot/game-core.js");
  assert.equal(obstacleForScore(0).kind, "log");
  assert.equal(obstacleForScore(500).kind, "stump");
  assert.equal(obstacleForScore(1000).kind, "fence");
  assert.equal(obstacleDelay(0, 0), 1.45);
  assert.equal(obstacleDelay(10000, 1), 1.16);
});

test("horse hitbox trims decorative pixels", async () => {
  const { horseHitbox } = await import("../HorseRunner.Web/wwwroot/game-core.js");
  assert.deepEqual(horseHitbox({ x: 58, y: 115, w: 43, h: 27 }), { x: 66, y: 120, w: 28, h: 20 });
});

test("arena validation, clearance, and scores are deterministic", async () => {
  const { APPLE_EATING_SECONDS, ARENA_OBJECT_TYPES, addScore, directionIndex, finalScore, jumpClearance, paceSpeed, qualifies, validateArena } = await import("../HorseRunner.Web/wwwroot/arena-core.js");
  const arena = { version: 1, settings: { laps: 5 }, scenes: [{ id: "a", name: "Arena", floor: { width: 280, height: 152 }, checkpoints: [{ x: 10, y: 10 }], objects: [{ id: "j", type: "jump", x: 1, y: 2, height: 2, rotation: 0 }] }] };
  assert.deepEqual(validateArena(arena), []);
  assert.equal(directionIndex(0), 0);
  assert.equal(directionIndex(Math.PI / 2), 2);
  assert.equal(directionIndex(Math.PI), 4);
  assert.equal(directionIndex(-Math.PI / 2), 6);
  assert.equal(paceSpeed(30, 0), 0);
  assert.equal(paceSpeed(30, 1), 18.6);
  assert.equal(paceSpeed(30, 2), 30);
  for (const type of ARENA_OBJECT_TYPES) {
    arena.scenes[0].objects[0].type = type;
    assert.deepEqual(validateArena(arena), [], `${type} should be valid editor data`);
  }
  assert.equal(APPLE_EATING_SECONDS, 5, "apple distraction should hold the horse for five seconds");
  assert.ok(ARENA_OBJECT_TYPES.includes("apple"), "apple should be a valid arena editor object");
  arena.scenes[0].objects[0].type = "unknown-prop";
  assert.match(validateArena(arena).join(" "), /invalid object/);
  assert.equal(jumpClearance(13, 2), true);
  assert.equal(jumpClearance(12, 2), false);
  assert.equal(finalScore({ elapsed: 60, clearances: 10, faults: 1, laps: 5 }), 6430);
  assert.equal(qualifies([{ score: 100 }, { score: 90 }, { score: 80 }, { score: 70 }, { score: 60 }], 61), true);
  assert.deepEqual(addScore([{name:"A",score:1,time:3}],{name:"B",score:2,time:4}).map(x=>x.name), ["B","A"]);
});

test("arena appearance choices normalize and recolor independently", async () => {
  const { HORSE_APPEARANCES, RIDER_APPEARANCES, normalizeArenaAppearance, remapArenaPixel } = await import("../HorseRunner.Web/wwwroot/arena-appearance.js");
  assert.equal(HORSE_APPEARANCES.length, 4);
  assert.equal(RIDER_APPEARANCES.length, 4);
  assert.deepEqual(normalizeArenaAppearance({ horse: "dapple-grey", rider: "burgundy" }), { horse: "dapple-grey", rider: "burgundy" });
  assert.deepEqual(normalizeArenaAppearance({ horse: "unknown", rider: "unknown" }), { horse: "chestnut", rider: "navy" });
  assert.notDeepEqual(remapArenaPixel(150, 88, 42, 255, 4, 8, "midnight", "navy"), [150, 88, 42, 255]);
  assert.notDeepEqual(remapArenaPixel(31, 44, 92, 255, 4, 8, "chestnut", "forest"), [31, 44, 92, 255]);
});

test("arena animation phases and faulted-fence rebuild are deterministic", async () => {
  const { animationFrame, jumpAnimationFrame, shouldRebuildObstacle } = await import("../HorseRunner.Web/wwwroot/arena-core.js");
  assert.equal(animationFrame(0, 6), 0);
  assert.equal(animationFrame(5.99, 6), 5);
  assert.equal(animationFrame(6, 6), 0);
  assert.equal(jumpAnimationFrame(2, 20, 30), 0);
  assert.equal(jumpAnimationFrame(12, 7, 30), 1);
  assert.equal(jumpAnimationFrame(20, -4, 30), 2);
  assert.equal(jumpAnimationFrame(7, -20, 30), 3);
  assert.equal(shouldRebuildObstacle(28, 10, 3, 2), false);
  assert.equal(shouldRebuildObstacle(29, 10, 3, 2), true);
  assert.equal(shouldRebuildObstacle(40, 10, 1, 2), false);
});
