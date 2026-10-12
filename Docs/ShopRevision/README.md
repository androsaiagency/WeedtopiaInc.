# Weedtopia Shop

The tenth round navigation button opens the shared Shop in Burning Hills and Big Rock City. Its authored panel and seventeen product cards are visible in the scene/prefab hierarchy. Five tabs provide Packages, VIP, Money, Grinders and Boosts. Products scroll vertically; a separate confirmation view has permanently visible purchase/back actions and the shared gold-X close action.

## Current use

In the Unity Editor and Development builds, the banner explicitly says **TESTSHOP · Keine echten Zahlungen**. The euro prices are the planned future prices. Purchases are free and repeatable, and grant the exact listed money, grinders and inventory items. Preview/back/close do not grant anything. Construction speedups are used through the existing inventory on a construction site. Bought money does not count as flower-sale income for quests.

All seventeen approved definitions, amounts, prices, product IDs and item quantities are in `catalog.json` and the productive `Assets/Weedtopia/Game/Content/BurningHills.json`. The current release default is unavailable and displays **Bald verfügbar** until a real payment provider is connected.

## VIP behavior

- One activation lasts thirty rolling 24-hour periods, from activation time.
- The first 50 grinders arrive immediately; the next payment becomes due after 24 hours. Thirty days deliver thirty payments, totalling 1,500 grinders.
- Missed payments are automatically credited when the game is running or loaded, including days that passed offline. The saved payment counter and monotonic UTC bound prevent repeat grants when the local clock moves backwards.
- Active production runs at 1.5 times its normal rate. Grow cycles are shortened with the existing whole-second simulation convention. Flower-sale unit revenue is multiplied by 1.5; other resources' prices and passive community income keep their existing formulas.
- The pass applies to both accessible cities and to the corresponding time interval during offline simulation. It stops applying after expiry.
- A repeat activation extends the active pass by thirty days. It does not stack the multipliers or grant a second daily payment immediately. Renewal after expiry preserves outstanding payments before starting a new period. No automatic renewal/payment exists.

## Future payment integration

`IShopPurchaseGateway` separates store listings and asynchronous authorization from presentation and reward fulfillment. `TestShopPurchaseGateway` is selected only for the Editor/Development profile; the release default is `UnavailableShopPurchaseGateway`. A future store adapter must return the store's localized price and a **verified**, approved authorization with a stable unique transaction ID and the matching product ID. Opening a payment sheet is not approval.

`ShopService` validates the response, refuses duplicate transactions and mismatched products/channels, grants rewards and saves atomically. Its durable fulfillment ledger is independent of the limited visual reward history. Save failures restore the complete previous grant state. This prepares the game side of billing; store-product configuration, actual billing SDK, receipt verification, replay of verified pending transactions and platform purchase policies still require implementation and testing.

## Authoring and evidence

- Explicit idempotent installer: **Weedtopia → Shop → Install Shop**. Shared prefabs: `Assets/Weedtopia/Production/Prefabs/Shop/ShopPanel.prefab` and `ShopProductCard.prefab`. Existing navigation, scene object IDs, artwork, audio and gameplay data are protected by the preservation baseline.
- `Tests/EditMode.xml`: complete domain/application/save regression including all seventeen grants, VIP timing, expiry boundaries, offline behavior, rollback, duplicate authorization and historical saves.
- `Tests/PlayMode.xml`: runtime regression including preview/cancel, grant, reload and VIP UI.
- `Tests/PlayMode-provenance.json`: this result combines the full run with explicit successful rechecks after updating the old nine-button expectation to ten. Both original test XMLs are retained; the runtime source was unchanged between these runs.
- `Tests/Assets.xml`: shared authored references, ten-button navigation, idempotent installation and presentation preflight.
- `capture_receipt.json`: actual Game View screenshots at 1920×1080, 2340×1080, 1080×2340 and 844×390, each at UI scale 1.0 and 1.2. Phone views include a 2.75 display-density simulation. Pointer checks and bounds/readability checks are recorded separately from physical device acceptance.
- `validation_receipt.json`: exact final counts and preservation checks.

Automated pointer checks and screenshots do not claim physical Android touch testing or user visual approval. No commit or push is part of this revision.

## Delivered Android test build

`Builds/Android/shop-20261004-105256/WeedtopiaInc-Android-Test.apk` is the completed Development/IL2CPP/ARM64 build. It contains Bootstrap, Burning Hills and Big Rock City and is about 1.38 GB. `build-result.json` records zero errors and two build warnings; `apk-verification.json` confirms the signature, archive alignment, game label and all twenty-six packaged icon entries matching the preceding approved green-city-icon APK. It is debug signed and has not been installed or physically tested by this validation.

To test: open a saved/new game, tap the gift-shaped Shop button in the bottom navigation, select a category and product, then choose **Kostenlos bestätigen**. Verify the updated balance or the five speedups in Inventory. VIP grants its first fifty grinders immediately. All test purchases are repeatable.
