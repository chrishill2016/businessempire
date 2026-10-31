# Business Empire (working title)

A mobile-first, offline-capable trading game. You're a black-market smuggler with **30 days** to turn a small stake (and a big debt) into the largest fortune you can. Buy low in one city, move the goods, sell high in another — while avoiding customs, police and rivals.

When the 30 days are up, your net worth is your score. Beat your top score and go again.

## Status
Planning / scaffolding. See [`docs/ROADMAP.md`](docs/ROADMAP.md) for phases and the future-feature log.

## Tech
- Blazor WebAssembly (standalone) PWA, .NET 10 LTS, csharp
- MudBlazor UI, dark mode by default
- Local storage in IndexedDB (offline-first, no account needed)
- Hosted on GitHub Pages, deployed by GitHub Actions

## Run locally
_Available from Phase 1:_
```bash
dotnet run --project src/<AppName>
dotnet test
```

## Deployment
- Pull requests to `main`: restore, build, test.
- Merge to `main`: build, test, publish and deploy to GitHub Pages (version stamped with the workflow run number).

## Workflow
Each phase is developed on a feature branch and opened as a pull request for review before merging to `main`.
