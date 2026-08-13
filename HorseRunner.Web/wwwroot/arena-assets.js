export const HORSE_DIRECTIONS = Object.freeze([
  Object.freeze({ id: "east", file: "01.png", flip: true }),
  Object.freeze({ id: "southeast", file: "02.png", flip: false }),
  Object.freeze({ id: "south", file: "03.png", flip: false }),
  Object.freeze({ id: "southwest", file: "04.png", flip: false }),
  Object.freeze({ id: "west", file: "01.png", flip: false }),
  Object.freeze({ id: "northwest", file: "05.png", flip: false }),
  Object.freeze({ id: "north", file: "07.png", flip: false }),
  Object.freeze({ id: "northeast", file: "08.png", flip: true })
]);

export const HORSE_EATING_DIRECTIONS = Object.freeze([
  Object.freeze({ id: "east", file: "01.png", flip: true }),
  Object.freeze({ id: "southeast", file: "02.png", flip: false }),
  Object.freeze({ id: "south", file: "03.png", flip: false }),
  Object.freeze({ id: "southwest", file: "04.png", flip: false }),
  Object.freeze({ id: "west", file: "05.png", flip: false }),
  Object.freeze({ id: "northwest", file: "06.png", flip: false }),
  Object.freeze({ id: "north", file: "07.png", flip: false }),
  Object.freeze({ id: "northeast", file: "08.png", flip: true })
]);

export const ARENA_PROPS = Object.freeze([
  Object.freeze({ type: "jump", label: "Show jump", file: "01.png", width: 40, height: 30, radius: 11, behavior: "jump" }),
  Object.freeze({ type: "log-jump", label: "Log jump", file: "02.png", width: 38, height: 29, radius: 11, behavior: "jump" }),
  Object.freeze({ type: "vertical-rails", label: "Vertical rails", file: "03.png", width: 38, height: 29, radius: 11, behavior: "jump" }),
  Object.freeze({ type: "oxer", label: "Oxer", file: "04.png", width: 40, height: 30, radius: 12, behavior: "jump" }),
  Object.freeze({ type: "cone", label: "Training cone", file: "05.png", width: 12, height: 17, radius: 7, behavior: "fault" }),
  Object.freeze({ type: "dressage-marker", label: "Dressage marker", file: "06.png", width: 12, height: 18, radius: 7, behavior: "fault" }),
  Object.freeze({ type: "barrel", label: "Barrel", file: "07.png", width: 15, height: 20, radius: 8, behavior: "fault" }),
  Object.freeze({ type: "hay-bale", label: "Hay bale", file: "08.png", width: 23, height: 16, radius: 9, behavior: "fault" }),
  Object.freeze({ type: "flower-planter", label: "Flower planter", file: "09.png", width: 24, height: 18, radius: 8, behavior: "decor" }),
  Object.freeze({ type: "saddle-rack", label: "Saddle rack", file: "10.png", width: 21, height: 26, radius: 8, behavior: "decor" }),
  Object.freeze({ type: "water-trough", label: "Water trough", file: "11.png", width: 25, height: 16, radius: 10, behavior: "fault" }),
  Object.freeze({ type: "arena-fence", label: "Arena fence", file: "12.png", width: 31, height: 21, radius: 10, behavior: "decor" }),
  Object.freeze({ type: "apple", label: "Apple distraction", file: "13.png", width: 9, height: 9, radius: 11, behavior: "distraction" })
]);

export const ARENA_PROP_BY_TYPE = new Map(ARENA_PROPS.map(prop => [prop.type, prop]));

export function horseSpritePath(direction) {
  return `assets/arena/horse-rider/${direction.file}`;
}

export function horseJumpSpritePath(direction) {
  return `assets/arena/horse-rider-jump/${direction.file}`;
}

export function horseEatingSpritePath(direction) {
  return `assets/arena/horse-rider-eating/${direction.file}`;
}

export function propSpritePath(prop) {
  return `assets/arena/props/${prop.file}`;
}
