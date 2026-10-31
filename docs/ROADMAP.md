# Roadmap & Feature Log

Each phase ships as its own pull request into `main`.

## Phase 1 — Core loop ✅
- [x] Project scaffold (Blazor WASM PWA, MudBlazor dark theme, CI/CD to GitHub Pages)
- [x] 6 cities, 8 black-market goods, each with a price range and per-city price bias
- [x] Daily price changes per city; not every good is traded every day
- [x] Buy / sell with cash and a 100-item suitcase limit
- [x] £5,000 starting debt with 5% daily interest; repay any time
- [x] Travel between cities costs days (1–3 depending on route)
- [x] 30-day limit, "wait a day", retire early, game-over screen, score = cash − debt
- [x] Local top-10 high scores (IndexedDB)
- [x] Run auto-saves after every action and resumes when you reopen the app
- [x] Version in footer, online/offline indicator, "new version available" prompt

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

## Phase 6 — Bigger world & score history
- [ ] **More cities** — e.g. New York, Miami, Bogotá, Lagos, Bangkok, Sydney, Moscow, Naples
  - Regions/continents so long-haul trips take longer (and later need flights)
  - Some cities only reachable by certain travel methods (ties into Phase 4)
- [ ] **More goods** — e.g. untaxed fuel, pirated games, counterfeit cash, rare wildlife parts (high risk), stolen car parts, fake medicines, cloned bank cards, rare whisky
  - Goods with different sizes (a painting takes more suitcase space than a phone)
  - Region-specific goods only sold in certain cities
- [ ] **Top 100 high scores** kept on the device (currently top 10)
  - Each entry shows the **run's start and end dates** (start date is already saved with each run)
  - Extra details per entry: days played, how the run ended (finished / retired early / arrested / killed), best single trade
  - Scroll/paginate the table, highlight your latest run, option to clear scores

## Phase 7 — Polish
- [ ] Sound and haptics
- [ ] Run statistics, achievements
- [ ] Difficulty modes / seeded "daily challenge"
- [ ] Optional online leaderboard (would add remote sync + `ISyncService`)

## Ideas backlog
- Rival smugglers who undercut prices or rob you
- Reputation with suppliers (better prices over time)
- Different starting characters with perks
- Weather/disasters closing routes
