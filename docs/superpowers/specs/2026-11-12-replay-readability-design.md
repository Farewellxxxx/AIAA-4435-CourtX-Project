# Replay readability

The user reports muddy 2D player trajectories and delegates further improvements to our judgment. Improve reading movement, ball flight and pass decisions in the existing offline learning hub. Implement locally and provide a separate runnable version for comparison.

## Approaches

- Color-only correction is cheap but leaves overlapping paths and the two gold line meanings unresolved.
- Selected approach (agent judgment): stronger, screen-sized paths, optional individual focus, distinct ball/target styles and visible pass marks on the timeline. It fits the current Unity IMGUI viewer and recorded data.
- A new UI framework or tactical/gameplay redesign adds unrelated risk and is outside this iteration.

## Design

Keep the teal/orange team identity. Give player paths a bright core with a narrow dark border and age progression over the existing two-second window; clip the window accurately. Draw actor paths in separate passes and show a small direction cue where enough movement exists. Use gold for recorded airborne basketball movement and an ivory dashed arrow for the recorded release target. Keep current recorded positions and playback interpolation unchanged.

Click a player badge to show only that player's path; click again or use the visible reset control to show all paths. Player positions and ball remain visible. Focus is presentation state, resets when changing record/page, and never changes the controlled role or the live simulation. Explain the interaction beside the map. Preserve fixed question timestamps and answer hiding rules.

Add small pass ticks to the existing timeline and retain its drag/play/node controls. Improve marker separation with dark borders. Keep the four-page layout, readable minimum font sizes and all existing actions.

## Delivery and evidence

Use Unity 2022.3.62f3c1, existing dependencies and a single sequential editor checkout. New branch and `Windows_Readability` output; preserve the entire `Windows_LearningPolish` build and all replay bytes. No remote publication. Baseline four suites passed 138 checks in the preceding inspection.

Validate using actual Unity renders at 1024x576, 1280x720, 1920x1080 and 1280x800; include crowded motion, selected path, quiz, history and comparison states. These are display fixtures, not user performance evidence. Run existing regression checks and native mouse input, then a fresh code review. Visual and reversible UI changes use rendered evidence rather than tests that mirror colors or styles.
