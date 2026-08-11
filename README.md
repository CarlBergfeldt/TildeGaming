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

The staged port and production art workflow are described in [`plan.md`](plan.md).
