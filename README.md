# Black Market Empire

A mobile-first trading game you can install on your phone, and it works offline. You're a smuggler with **30 days** to turn a small stake and a loan shark's debt into the biggest fortune you can. Buy cheap in one city, carry it to another, and sell high.

When the 30 days are up, your score is **cash plus savings, minus everything you owe**. Your top 100 scores, with the dates of each run, are kept on your device.

> It's a game. In real life, smuggling ends in prison or worse.

**Play:** https://chrishill2016.github.io/businessempire/ (on your phone, use *Add to Home Screen* / *Install app*)

## How it plays
- 14 cities across Europe, the Middle East & Africa, Asia-Pacific and the Americas, and 14 black-market goods. Some goods are bulky, and some only trade in certain cities.
- Prices change every day, and each city is cheaper or dearer for certain goods.
- Travel by **train, car, plane or boat**. Each has its own fare, journey time and police risk, and airports are the strictest. Your £5,000 debt grows 5% every day until you pay it back.
- Your suitcase holds 100 items. Anything still unsold when the run ends is worthless.
- **Market events:** the intel feed reports price spikes and crashes in other cities. They only last a few days, and about a quarter of the rumours are false. You only find out by going there.
- **Police and customs:** the more you carry, the more likely you are to be stopped. You can run, bribe, fight or surrender. Get arrested and you lose your goods, pay a fine and spend days in jail. Get hurt too badly and you're dead.
- **Couriers and gear:** send goods ahead with a courier (they might get seized or stolen), and buy gear such as bigger bags, hidden linings, fake documents, a police scanner or body armour. Every city has its own **heat**, which rises when you cause trouble there.
- **Money:** borrow from three loan sharks, a bank or the Syndicate. Each has its own rates and deadlines, and repaying on time improves your reputation and gets you better rates. Miss a deadline and enforcers come for you. Bank your cash to keep it safe, and rent a lock-up or warehouse to stash goods (they can be raided). Some cities are more dangerous than others.
- **Intel:** pay informants to check rumours, buy tip-offs about spikes and crashes before they start, get price reports from contacts in other cities, or pay a fixer to cause a shortage. Watch out for police crackdowns.
- Your run saves after every move, so you can close the app and carry on later.

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for upcoming phases and the feature log.

## Tech
- Blazor WebAssembly (standalone) PWA, .NET 10 LTS, csharp
- MudBlazor UI, dark theme
- IndexedDB for on-device storage. There's no account and no server.
- xUnit + bUnit tests

## Project layout
```
src/BlackMarketEmpire/
  Features/        Home, Game (market, travel, dialogs, game over), Scores
  Services/        GameEngine (rules), MarketService (prices), EventService (events & rumours), EncounterService (police), TravelService (routes & fares), ShippingService (couriers), FinanceService (loans, bank, stashes), IntelService (informants, tips, contacts, fixers), GameSession, HighScoreService, IndexedDbStore
  Models/          GameState, Good, City, GameRules, HighScore
  wwwroot/         index.html, service worker, manifest, icons, js interop
tests/BlackMarketEmpire.Tests/
build/prepare-pages.sh   GitHub Pages fix-ups (base href, 404.html, .nojekyll, SW hash)
```
Balancing numbers (days, starting cash, debt, interest, capacity) are in `Models/GameRules.cs`. Goods, cities and travel times are in `Services/GameCatalog.cs`.

## Run locally
Requires the .NET 10 SDK.
```bash
dotnet run --project src/BlackMarketEmpire
dotnet test
```

## Deployment
- **Pull request to `main`:** restore, build, test (`.github/workflows/ci.yml`).
- **Merge to `main`:** build, test, publish and deploy to GitHub Pages (`.github/workflows/deploy.yml`). The version is stamped as `1.0.<run number>` and shown in the footer.
- One-time setup: repo **Settings → Pages → Source: GitHub Actions**.

## Workflow
Each phase is built on a feature branch and opened as a pull request for review before merging to `main`.
