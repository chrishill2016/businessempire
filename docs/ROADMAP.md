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

## Phase 5 — Economy depth ✅
- [x] **Several lenders**, each with their own cities, rates, limits, deadlines and enforcers:
  - Fast Eddie (London, Istanbul): 8%/day, small loans, 7 days, 1 day's grace
  - The Accountant (Amsterdam, Dubai): 4%/day, bigger loans, 5 days, no grace, takes goods
  - Mama Rossi (Marseille, Hong Kong): 6%/day, 6 days, rough enforcers
  - First Meridian Bank (London, Amsterdam, Dubai, Hong Kong): 1%/day, up to half your net worth; needs £10k net worth and no arrests
  - The Syndicate (Istanbul, Hong Kong): 10%/day, up to £50k, brutal enforcers who take goods
- [x] **Reputation per lender** (★–★★★★★): repay on time (+1, or +2 if early) for lower rates and bigger limits; overdue loans cost 3 reputation points; at 1★ or below they won't lend. Arrests and heat raise every rate.
- [x] **Deadlines:** overdue loans add +2%/day, and enforcers may find you (60%/day) in that lender's cities: they take cash, maybe goods, and hurt you, and they can kill you
- [x] **Bank account:** deposit and withdraw at branches; 0.5%/day interest; safe from fines, enforcers and death
- [x] **Stash houses:** lock-up (£1,000, 150 items, 3%/day raid chance) or warehouse (£6,000, 500 items, 1%); raid risk rises with heat
- [x] **City risk profiles:** Low (Amsterdam, Hong Kong), Medium (London, Istanbul) and High (Marseille, Dubai) danger change how often police stop you
- [x] Score = cash + bank − everything owed; if you're killed, the bank still counts and the debts still count
- [ ] Later: loan extensions for good customers, lenders who sell your debt on, insurance on stashes, the bank freezing your account if your heat gets too high

## Phase 6 — Bigger world & score history ✅
- [x] **14 cities in 4 regions:** Europe (London, Amsterdam, Marseille, Naples, Moscow, Istanbul), Middle East & Africa (Dubai, Lagos), Asia-Pacific (Hong Kong, Bangkok, Sydney), Americas (New York, Miami, Bogotá)
  - Travel time from real distances: 1–4 days (Sydney is a long way from anywhere)
  - Road and rail only within linked groups (Europe; Istanbul–Dubai; Hong Kong–Bangkok; New York–Miami)
  - Bogotá and Moscow have no port (flights, or overland for Moscow); London has none either
  - New lender Don Carlos (Miami, Bogotá, Lagos); lenders and the bank spread to the new cities
- [x] **14 goods**, adding pirated games, untaxed fuel, fake medicines, cloned bank cards, stolen car parts and counterfeit cash
  - **Sizes:** fuel and art take 3 spaces each, car parts 4
  - **Region-specific:** fuel, bank cards, car parts and counterfeit cash only trade in certain cities
- [x] **Top 100 high scores** with start/end date and time, days played, how the run ended, best single trade; latest run highlighted; "show more" paging; clear all scores
- [ ] Later: a world map view, seasonal routes/closures, arrested as an end reason when a jail term outlasts the run

## Phase 7 — Intel network ✅
Better information means better trades, at a price.
- [x] **Informants** in every city check any rumour before you travel:
  - Street (£150, 75% reliable) or connected (£600, 95% reliable; also tells you the exact multiplier and end day of a real event)
  - **Trust:** each paid job in a city raises trust (up to ★★★★★), adding up to +15% street / +4% connected accuracy
- [x] **Advance warning:** some spikes and crashes are now planned in secret 1–3 days ahead. A tip-off (£1,000) reveals the next one before anyone else knows. If nothing is brewing it tells you about a crackdown instead, and there's no charge if there's nothing at all.
- [x] **Insider influence:** pay a fixer to cause a shortage (a price spike) in any city two days from now. Price scales with the good, 60% success (+5% per trust), and you only find out if it worked when the day comes.
- [x] **Market contacts** (£400): today's prices in any other city, every active spike or crash there with its exact size and end day, and whether there's a crackdown
- [x] **Police crackdowns:** hidden 2–4 day crackdowns make stops 50% more likely. You learn about them from the police scanner (as they're planned), tip-offs, contacts, or by arriving in the middle of one.
- [x] Intel feed shows insider tips, informant verdicts and known sizes; travel list shows tips, verdicts and crackdowns
- [ ] Later: rival crews planting false tips on purpose, informants who get burned (and stop helping) if you're arrested after using them, buying a contact's standing order (a daily report from one city)

## Phase 8 — Polish ✅
- [x] **Sound and vibration:** synthesised Web Audio effects (no audio files) for buying, selling, travel, police sirens, escapes, arrests, injuries, death, money, intel, finishing and achievements, with matching vibration patterns. Both can be switched off in Settings.
- [x] **Career stats:** runs played, best and average score, best single trade, total trading profit, trades, finished/retired/killed, days played, stops, arrests, escapes, money borrowed, cities visited, daily challenges, best score per difficulty
- [x] **Achievements:** 19 to unlock (First Run, Millionaire, Globetrotter, Clean Record, Escape Artist, Hard Lesson, Made It Happen, Hard Case, Daily Grind and more), shown on the game-over screen and their own page
- [x] **Difficulty modes:** Easy / Normal / Hard change starting cash and debt, interest, police, enforcers, rumour reliability, healing and suitcase size
- [x] **Daily challenge:** a seeded run that's the same for everyone on the same day (Normal difficulty); your best daily score is shown on the home screen
  - Every run now has its own saveable random-number generator, so a run resumed after closing the app plays out exactly as it would have
- [x] Settings page: sound, vibration, test sounds, reset stats and achievements, clear high scores
- [ ] **Optional online leaderboard:** needs a remote database plus an `ISyncService`. Left out for now because the game is offline-only with no account. Would suit the daily challenge, since everyone gets the same seed.

## Ideas backlog
- Rival smugglers who undercut prices or rob you
- Reputation with suppliers (better prices over time)
- Different starting characters with perks
- Weather/disasters closing routes
