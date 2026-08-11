import test from "node:test";
import assert from "node:assert/strict";

globalThis.document = { querySelector: () => null };

test("gameplay helpers remain deterministic", async () => {
  const source = await import("node:fs/promises").then(fs => fs.readFile(new URL("../HorseRunner.Web/wwwroot/game.js", import.meta.url), "utf8"));
  const helpers = source.slice(0, source.indexOf("const canvas"));
  const url = `data:text/javascript,${encodeURIComponent(helpers)}`;
  const { clamp, overlaps, scoreText } = await import(url);
  assert.equal(clamp(12, 0, 10), 10);
  assert.equal(scoreText(42.9), "00042");
  assert.equal(overlaps({x:0,y:0,w:10,h:10},{x:9,y:9,w:2,h:2}), true);
  assert.equal(overlaps({x:0,y:0,w:10,h:10},{x:10,y:0,w:2,h:2}), false);
});
