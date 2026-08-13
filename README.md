# TildeGaming
Games by Tilde &amp; Carl

- Horse Runner

   <img width="382" height="273" alt="image" src="https://github.com/user-attachments/assets/5c7b3592-6f38-48bc-a819-d864a24899c6" />

## Browser edition

A browser-playable vertical slice now lives in `HorseRunner.Web`. It uses an
ASP.NET Core host and a dependency-free HTML canvas client.

```bash
dotnet run --project HorseRunner.Web
```

Open `http://localhost:5188`, then use <kbd>Space</kbd>, <kbd>↑</kbd>, or the
on-screen button to jump. Press <kbd>P</kbd> or <kbd>Esc</kbd> to pause.
Use the Restart control to begin a fresh run at any time. The page also includes
keyboard and touch instructions below the game screen.

Choose **Arena five-lap** on the same page for a top-down, automatically moving
horse-riding challenge. Steer with the arrow keys or WASD and jump with Space.
Scores combine five completed laps, successful clearances, faults, and elapsed
time; qualifying riders can enter their name in a browser-local top-five table.

The start menu's **Game editor** provides drag-and-drop arena layout, multiple
selection, object properties, undo/redo, scene creation, JSON validation,
import/export, and local drafts. The shipped arena is inspectable at
`HorseRunner.Web/wwwroot/data/arena.json`; saving a draft never overwrites it.

Run the dependency-free gameplay checks with:

```bash
node --test HorseRunner.Web.Tests/game.test.mjs
```

The staged port and production art workflow are described in [`plan.md`](plan.md).
