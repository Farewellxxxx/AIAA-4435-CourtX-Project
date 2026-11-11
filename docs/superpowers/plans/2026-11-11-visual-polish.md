# CourtX visual polish

Scope confirmed in chat: font, formatting, fuzzy line segments and page usability. Continue within existing approved functionality; no additional approval round requested. Base e07c449, stable tag v0.5-learning-stable-20261111, branch feature/learning-visual-polish. Preserve Windows_Learning and its two user replay files; new build Windows_LearningPolish.

Observed causes: virtual-canvas scaling also scales already-rasterized font glyphs; thin rotated white rectangles have no antialiasing and become close to one pixel after court/canvas scaling. Small footer type, dense role paragraphs, opaque default slider and weak control states make the page harder to read.

1. Render learning-page labels and controls in integer screen-pixel rectangles, with scaled integer font sizes and minimum readable text sizes. Keep design-grid geometry, including scroll content, without scaling glyph textures through GUI.matrix.
2. Draw court strokes with a narrow antialias fringe, minimum screen-pixel thickness, clearer contrast and smoother curves. Render jersey numbers at native pixel sizes. Keep court geometry, ball/actor coordinates and tactical behavior unchanged.
3. Improve spacing, role text grouping, slider visibility, disabled/hover states and warning sizing. Preserve all four page interactions and existing data semantics.
4. Capture real player screens at 1024x576, 1280x720 and 1920x1080; inspect lesson, quiz, history and comparison layouts. Run existing catalog/lifecycle/recap tests, then a real-window input smoke check. Visual-only changes use rendered before/after evidence rather than implementation-mirroring tests.
5. Build/package independently, verify old archive/build and user replay hashes, copy user replays to the new local program only, save before/after results and push the new branch/tag. Close only test instances; keep computer on.

This is a focused repair of the existing UI, not a new interaction design. Routine implementation choices are delegated by the user's ongoing request to inspect and improve the current version.
