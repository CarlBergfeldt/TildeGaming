export const HORSE_DIRECTIONS = Object.freeze([
  Object.freeze({ id: "east", file: "01.png", flip: false }),
  Object.freeze({ id: "southeast", file: "02.png", flip: true }),
  Object.freeze({ id: "south", file: "03.png", flip: false }),
  Object.freeze({ id: "southwest", file: "04.png", flip: false }),
  Object.freeze({ id: "west", file: "01.png", flip: true }),
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

export const HORSE_RUN_FRAME_COUNT = 6;
export const HORSE_JUMP_FRAME_COUNT = 4;

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
  Object.freeze({ type: "apple", label: "Apple distraction", file: "13.png", width: 9, height: 9, radius: 11, behavior: "distraction" }),
  Object.freeze({ type: "hotdog-stand", label: "Hotdog stand", file: "14.png", width: 43, height: 35, radius: 16, behavior: "decor" }),
  Object.freeze({ type: "drinks-cart", label: "Drinks cart", file: "15.png", width: 31, height: 34, radius: 13, behavior: "decor" }),
  Object.freeze({ type: "judges-table", label: "Judges' table", file: "16.png", width: 45, height: 29, radius: 16, behavior: "decor" }),
  Object.freeze({ type: "photographer", label: "Event photographer", file: "17.png", width: 18, height: 29, radius: 8, behavior: "decor" }),
  Object.freeze({ type: "spectator-group", label: "Spectator group", file: "18.png", width: 41, height: 31, radius: 15, behavior: "decor" }),
  Object.freeze({ type: "grandstand", label: "Covered grandstand", file: "19.png", width: 58, height: 43, radius: 20, behavior: "decor" }),
  Object.freeze({ type: "waiting-horses", label: "Waiting horses", file: "20.png", width: 44, height: 35, radius: 16, behavior: "decor" }),
  Object.freeze({ type: "announcer-booth", label: "Announcer booth", file: "21.png", width: 38, height: 40, radius: 15, behavior: "decor" }),
  Object.freeze({ type: "arena-fence-gate", label: "Arena fence gate", file: "22.png", width: 46, height: 35, radius: 14, behavior: "decor" })
]);

export const ARENA_PROP_BY_TYPE = new Map(ARENA_PROPS.map(prop => [prop.type, prop]));

export function horseSpritePath(direction) {
  return `assets/arena/horse-rider/${direction.file}`;
}

export function horseRunFramePath(direction, frame) {
  return `assets/arena/horse-rider-run/${direction.file.slice(0, 2)}/${String(frame + 1).padStart(2, "0")}.png`;
}

export function horseAnimatedJumpFramePath(direction, frame) {
  return `assets/arena/horse-rider-jump-animated/${direction.file.slice(0, 2)}/${String(frame + 1).padStart(2, "0")}.png`;
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
