# Light Weaver v5.0 — Polish, Economy, Responsive UI & Main Menu Plan

## Phase 1: Core Balance, Flashlight Range, Dual Collectibles & Meta-Shop Backend
1. **Flashlight Range (`LightConeCaster2D.cs`)**
   - Preserve beam cone angles (`standardHalfAngleDeg = 22f`, `focusedHalfAngleDeg = 8.5f`).
   - Tighten forward visibility (`standardRange`: `10.5f -> 6.8f`, `focusedRange`: `16.0f -> 10.2f`) so players must actively read incoming terrain rather than seeing overly far ahead.
2. **Battery Economy & Distance-Capped Drain (`BatterySystem.cs`)**
   - Add meta-upgrade support: `ApplyMetaUpgrades(float bonusCapacity, float efficiencyMultiplier)` (base `100f` + up to `+100f` from shop, drain reduction up to `-20%`).
   - Scale drain smoothly with distance (`baseDrainPerSecond = 2.6f` -> `maxDistanceDrainPerSecond = 4.9f` over `420f` meters) with a hard cap (`maxTotalDrainRate = 6.0f` before focused multiplier) so late-game runs stay tense without instant drain.
3. **Currency & Persistent Shop (`ProgressManager.cs`)**
   - Add `Coins` (`AddCoins`, `SpendCoins`), `BatteryCapacityLevel` (`0..5`, `+20` max battery/level), `BatteryEfficiencyLevel` (`0..5`, `-4%` drain/level), and `CoinMagnetLevel` (`0..4`, `+0.65m` collectible pull radius/level).
   - Provide clean `GetUpgradeLevel`, `GetMaxUpgradeLevel`, `GetUpgradeCost`, and `TryPurchaseUpgrade` APIs.
4. **Dual Collectibles (`CoinCollectible.cs` + `EnergyOrb.cs` + `EndlessChunkDirector.cs` + `GameFlowManager.cs`)**
   - Add `CoinCollectible.cs` (`PoolScarabCoin`) with magnet attraction & Zero-GC pickup events.
   - Rebalance `EndlessChunkDirector.cs`: ~75% of former orb placements spawn Scarab Coins (single & arc lines), while Battery Cells spawn more sparsely as high-value strategic refills (`+24f` / `+36f`).
   - Wire `GameFlowManager.cs`, `WorldVfxManager.cs`, and `HarmonicAudioManager.cs` for coin counting, VFX bursts, and crisp coin chime SFX.

## Phase 2: Art Pack Upgrades — Battery Cell, Scarab Coin & Enemies/Obstacles
- Generate & extract distinct, cohesive Ancient Egypt painted sprites:
  - `egypt_battery_cell.png`: Unmistakable cylindrical brass/glass Sun-Cell Battery with glowing cyan core.
  - `egypt_gold_coin.png`: Ornate stamped Ancient Gold Scarab Coin.
  - Upgraded `egypt_umbra_stalker.png`, `egypt_void_leech.png`, and `egypt_shadow_obelisk.png` with sharper silhouettes and crimson/violet threat readability.

## Phase 3: UI/UX Feel Overhaul & Multi-Resolution Responsive Mobile Optimization
- Refine `LightWeaverHUD.cs`, `MenuScreensUI.cs`, `SafeAreaFit.cs`, and `LightWeaverSetupUtility.cs`:
  - Sleek dark-obsidian panels with subtle gold hairline borders, clean typography hierarchy, dynamic battery color transitions, coin counter pill, tactile button press feedback (`UIButtonJuice`).
  - Responsive anchoring & `CanvasScaler` tuning across `16:9`, `19.5:9`, `20:9`, `21:9`, and `4:3`.

## Phase 4: Atmospheric Full-Screen Main Menu + Integrated Relic Workshop (Shop)
- Replace the cramped centered popup card with a scenic full-screen Main Menu:
  - Live animated Giza pyramids, stars, and idle detective with gently sweeping lantern.
  - Top title + currency/best-run pill, bottom-right primary `EXPEDITION` CTA, `WORKSHOP` shop button, and compact audio/haptic toggles.
  - Integrated **Relic Workshop** modal displaying live Coin balance, upgrade cards (`Battery Capacity`, `Solar Efficiency`, `Scarab Magnet`), level pips, and buy buttons.

## Phase 5: Multi-Resolution & Play Mode Verification, Cleanup & Push
- Rebuild scene via `LightWeaverSetupUtility`, test Play Mode & simulated aspect ratios (`1920x1080`, `2400x1080`, `2796x1290`, `2048x1536`), clean temp screenshots, update `PROJECT_MEMORY.md` & `Docs/PROJECT_TRACKER.md`, commit & push.
