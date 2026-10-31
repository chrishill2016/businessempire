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

## Phase 4 — Smuggling logistics ✅
- [x] **Travel costs money:** a fare per trip based on distance and method. You can't travel if you can't afford it.
- [x] **Travel methods** with different time / cost / risk:
  - Train: cheap, normal checks (overland routes only)
  - Car: dearer, fewer stops (roadblocks), and +15% to outrun police
  - Flight: fastest, works everywhere, but **airport customs are the strictest** (×1.6 stop chance)
  - Boat: slow and cheap, quietest checks, port cities only
- [x] **Couriers:** pay to send goods ahead instead of carrying them
  - Budget (10% fee, +1 day, 25% chance it's lost) vs Professional (25% fee, 8% chance it's lost)
  - Delivered goods wait at the destination until you collect them (space permitting)
- [x] **Heat** per city: rises with stops, escapes, arrests and big deals; cools 5/day; raises your chance of being stopped
- [x] **Gear shop:** big holdall (+50), wheeled trunk (+100), hidden-lining case, fake documents, police scanner app, burner phones, running shoes, body armour, first-aid kits
  - Concealment gear (lining, documents) is seized if you're arrested
- [ ] Still to come from this phase:
  - Hire van (+250 space, road trips only)
  - Hiring mules to carry part of the load
  - Contacts/informants who warn about crackdowns
  - Bribing officials ahead of time (price rises with heat); a "friend in the force" per city
  - Gear stolen in muggings; some gear only sold in certain cities, with varying prices
  - Fares that spike with events (strikes, fuel crisis)

## Phase 5 — Economy depth
- [ ] **Loans:** borrow more money mid-run to fund bigger deals
  - **Several loan sharks**, each with their own terms and personality, for example:
    - *"Fast Eddie"* (London): small loans, 8%/day, forgiving if you're a day late
    - *"The Accountant"* (Amsterdam): bigger loans, 4%/day, strict deadlines, no extensions
    - *"Mama Rossi"* (Marseille): mid-size, 6%/day, sends enforcers the day you're overdue
    - Each lender only operates in certain cities, so you have to be there to borrow or repay
  - **Reputation decides your rates.** Each lender tracks how you've treated them:
    - Repay on time or early → lower interest, higher borrowing limits, longer deadlines
    - Pay late or skip repayments → higher rates, smaller limits, and eventually they won't lend to you at all
    - A big arrest record or a lot of heat makes every lender nervous (worse rates across the board)
    - Reputation shown as a simple rating per lender (e.g. ★–★★★★★) with the rate they'd offer you now
  - **Bank loan:** cheaper interest, but only in some cities and only if your net worth is good enough. It's refused if your rap sheet has arrests.
  - **Underground lender** in certain cities: big sums, but miss the repayment date and their enforcers come after you (health damage, goods taken)
  - Repayment deadlines: if a loan is overdue, interest rises, and loan-shark "visits" can happen when you arrive in a city
  - Any unpaid loans are taken off your final score, like the starting debt
- [ ] **Bank (safe deposit):** keep cash safe from fines, muggings and death. Small interest, and only usable in cities with a branch.
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
