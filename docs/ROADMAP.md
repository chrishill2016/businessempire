# Roadmap & Feature Log

Each phase ships as its own pull request into `main`.

## Phase 1 — Core loop
- [ ] Project scaffold (Blazor WASM PWA, MudBlazor dark theme, CI/CD to GitHub Pages)
- [ ] 6 cities, ~8 contraband goods, each with a price range
- [ ] Daily price fluctuation per city
- [ ] Buy / sell with cash and a carry-capacity limit
- [ ] Starting debt that must be repaid to count towards score
- [ ] Travel between cities costs days (1–3 depending on route)
- [ ] 30-day limit, game-over screen, net-worth score
- [ ] Local top-10 high scores (IndexedDB)
- [ ] Version in footer, online/offline indicator, "new version available" prompt

## Phase 2 — Market events
- [ ] Random events that spike or crash a good's price in a city for several days
- [ ] News feed / rumours (some rumours are false)

## Phase 3 — Danger: police & customs
- [ ] Random encounters while travelling or trading
- [ ] Choices: run, bribe, fight, surrender
- [ ] Health; injuries; death ends the run
- [ ] Losing goods/cash when caught; jail time costs days
- [ ] End-of-run consequences message (the real-world message: crime doesn't pay)

## Phase 4 — Smuggling logistics
- [ ] **Travel methods** with different time / cost / risk:
  - Bus/train — cheap, slow, moderate checks
  - Car — medium, random roadblocks
  - Flight — fastest, but **airport security is the strictest**
  - Boat — slow, low checks, only between port cities
- [ ] **Shipping / couriers** — pay to send goods to another city instead of carrying them
  - Costs a fee (% of cargo value), arrives after N days
  - Lower risk than carrying, but shipments can be seized or a courier can steal them
  - Reputable couriers cost more but are safer
- [ ] **Ways to beat the police**
  - Hidden compartments / concealment upgrades (reduce search chance)
  - Bribing officials (per-city, gets more expensive with heat)
  - Fake documents for airports
  - Hiring mules to carry part of the load
  - Contacts/informants who warn about crackdowns
- [ ] **Heat / wanted level** per city — rises with big trades and encounters, cools over time

## Phase 5 — Economy depth
- [ ] Bank (safe deposit) and loan shark with interest
- [ ] Stash houses in cities (store goods; can be raided)
- [ ] Capacity upgrades (bigger bag, van, warehouse)
- [ ] City risk profiles (some cities lucrative but dangerous)

## Phase 6 — Polish
- [ ] Sound and haptics
- [ ] Run statistics, achievements
- [ ] Difficulty modes / seeded "daily challenge"
- [ ] Optional online leaderboard (would add remote sync + `ISyncService`)

## Ideas backlog
- Rival smugglers who undercut prices or rob you
- Reputation with suppliers (better prices over time)
- Different starting characters with perks
- Weather/disasters closing routes
