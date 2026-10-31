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

## Phase 2 — Market events ✅
- [x] Random events that spike (×2–4) or crash (×0.25–0.5) a good's price in one city for 2–5 days
- [x] Intel feed of headlines, plus a ticker under the status panel
- [x] ~25% of headlines are false rumours; the truth is revealed when you visit (Confirmed / Blown over / False)
- [x] Rumours shown against each city in the travel list, so you can decide if the trip is worth it
- [x] SPIKE / CRASH badges on affected goods in the market
- [ ] Ideas for later: paid informants who tell you which rumours are true, events that hit a whole region, "insider tip" events with bigger multipliers

## Phase 3 — Danger: police & customs ✅
- [x] Stopped on arrival when carrying goods; the more (and the more valuable) you carry, the likelier it is
- [x] Street police on short trips, stricter customs after long-haul (2+ day) trips
- [x] Undercover stings on big deals (£10k+)
- [x] Choices: run, bribe, fight, surrender, with the odds shown
- [x] Health (100), recovers 3 a day; failed runs and fights hurt; death ends the run and your cash dies with you
- [x] Arrest: all goods seized, 20% fine, 1–4 days in jail (debt keeps growing)
- [x] Rap sheet and a short reality check on the game-over screen; ☠ marks deaths in high scores
- [ ] Later: gangs/muggers as well as police, hospital visits to heal faster, lawyers to cut jail time

## Phase 4 — Smuggling logistics
- [ ] **Travel costs money** — every trip has a ticket/fuel price based on distance and travel method, so moving isn't free
  - Prices vary by method (bus cheapest, flights dearest) and can spike with events (strikes, fuel crisis)
  - Can't travel if you can't afford the fare — plan your cash before you sell out
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

### Gear shop
Spend cash on gear that sticks with you for the rest of the run. Each item is a trade-off between spending now and earning or surviving later.
- [ ] **Capacity:** bigger holdall (+50), wheeled case (+100), hire van (+250; road travel only)
- [ ] **Avoiding police:**
  - Hidden-lining suitcase: lowers the chance you're stopped
  - Fake documents: big bonus against customs/airport checks
  - Police scanner app: warns you about a crackdown before you travel
  - Burner phones: harder to trace, lowers sting chance on big deals
- [ ] **Escaping:** running shoes (better run odds), a fast car (much better run odds on road trips)
- [ ] **Survival:** body armour (cuts damage taken), first-aid kit (heal once when hurt)
- [ ] **Bribing:** a "friend in the force" contact who makes bribes cheaper and more likely to work in one city
- [ ] Gear can be **lost**: seized on arrest, or stolen in a mugging, so it's a risk, not a guaranteed upgrade
- [ ] Some gear is only sold in certain cities, and prices vary like goods

## Phase 5 — Economy depth
- [ ] Bank (safe deposit) and loan shark with interest
- [ ] Stash houses in cities (store goods; can be raided)
- [ ] Warehouse / storage space (see the Phase 4 gear shop for carried-capacity upgrades)
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
