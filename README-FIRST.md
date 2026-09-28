# TriviaIQ — NFL Football: Quarterback Stars

A complete Unity source project converted from the supplied WordIQ clone.

## Open and play

1. Extract this ZIP into a **new folder**, rather than overlaying an old WordIQ checkout.
2. In Unity Hub, Add project from disk and select the `TriviaIQ` folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Use Unity **6000.5.7f1**, matching the supplied project. Allow Unity to import the assets.
4. Open `Assets/Scenes/TspMenuScene.unity` and press Play. The old scene filenames remain to preserve build references; the screens are now TriviaIQ.

The interface is created by `Assets/Scripts/TriviaIQApp.cs` at runtime. The scene hierarchy is intentionally minimal. Both supplied scenes have been replaced, so neither Play nor How to Play uses WordIQ content. The menu uses a simple football-field design with a TriviaIQ title.

## First edition

12 easy quarterback puzzles: Patrick Mahomes, Tom Brady, Josh Allen, Aaron Rodgers, Lamar Jackson, Joe Burrow, Justin Herbert, Jalen Hurts, Matthew Stafford, Dak Prescott, Jared Goff and Baker Mayfield.

These players were active within 2021–2026; this does not mean all are still active. Clues deliberately use completed historical achievements through 2023 and avoid live totals or current-team assumptions. Primary-source links and the complete clue list are in `FACTS-AND-SOURCES.md`.

- START exposes clue 1 and starts the clock.
- Clues 2, 3 and 4 appear after 30, 60 and 90 seconds. All previous clues remain visible.
- Full names, surnames and the explicit aliases in the pack are accepted, ignoring case, spaces, accents and punctuation. Arbitrary spelling mistakes are not accepted.
- GUESS or Enter submits a name. Wrong non-empty guesses add 10 seconds to the final result; hint timing uses actual elapsed time.
- The clock continues after the final clue until solved or revealed. Time spent away from the tab counts.
- REVEAL ends the attempt without saving a successful score. REPLAY starts a practice attempt.
- Best adjusted times and solved flags are saved with PlayerPrefs for this device/browser. Clearing browser data can erase them. There is no account, server or cross-device sync.
- PREV/NEXT and the All Puzzles/Unsolved filter are locked during an attempt. MENU abandons the attempt. A solved puzzle stays visible until the next navigation/filter action.
- Music toggle and the link to the IQ Games collection are included.

## Build for itch.io

Install Unity's WebGL Build Support module. Use **Tools > TriviaIQ > Build WebGL**. This builds both scenes to `Builds/TriviaIQ-WebGL`, selects the responsive template and disables compression for straightforward static hosting.

Zip the **contents** of that output folder (so `index.html` is at the ZIP root), then upload to itch.io as an HTML game. The ZIP delivered here is the source project, not a compiled browser build. Obsolete WordIQ browser builds, APKs and generated build caches from the original upload are not included.

## More packs and editions

`Assets/Resources/TriviaPacks/nfl-quarterback-stars-01.json` is the starter pack. Duplicate it for a new pack, give it a unique `id`, and edit the title, edition, difficulty, answers, aliases, four hints and source URLs. Stable puzzle IDs preserve saved progress. Change a puzzle ID if you replace its answer/content and want a fresh record. Keep exactly four hints per puzzle.

Multiple valid JSON packs automatically enable CHANGE PACK on the menu. No code changes are required for a new subject. New packs require a fresh Unity build; this version does not download or sell packs.

## Validation status

Checked JSON structure, 12 unique puzzle IDs, all 48 hints, answer/alias uniqueness, resource paths, scene script references and source ZIP integrity. Football clues checked against the listed primary sources.

**Unity is not installed in the preparation environment. The Editor compile, rendered layouts, audio/input behavior and WebGL build have not been run.** Run the following smoke checks in Unity before publishing:

1. Menu: Play, How to Play, Back and sound toggle.
2. Start a puzzle: first clue appears immediately; subsequent clues arrive at 30/60/90 seconds.
3. Submit blank/punctuation-only input (no penalty), a wrong name (+10 seconds), and a correct surname in mixed case.
4. Correct answer stops timer, shows answer and saves best time; a slower replay does not replace it.
5. Reveal does not mark a new puzzle solved. PREV/NEXT wrap safely. Solve all questions and confirm Unsolved shows the completion message.
6. Reload: saved records remain separate from WordIQ. Check portrait and landscape, including touch keyboard entry.
7. Build WebGL and check both scenes and the loading title in a browser.

## Camera fix

The shared UI startup now creates an enabled camera when none is rendering, removing the Unity Game view "No cameras rendering" overlay in both menu and gameplay. For an existing installation, stop Play mode and replace only `Assets/Scripts/TriviaIQApp.cs` with this version.

## Menu cleanup

Removed the top IQ GAMES / EDITION 01 label. The sound toggle now shows a white speaker icon on a transparent background, retaining its clickable area and on/off behavior. To update an existing project, stop Play mode and copy `Assets/Scripts/TriviaIQApp.cs` from this ZIP over the same file in your project, then rebuild WebGL.
