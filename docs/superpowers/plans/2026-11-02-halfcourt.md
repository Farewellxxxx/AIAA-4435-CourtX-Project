# CourtX half-court implementation plan
Approved scope from the chat: a separate Unity half court, six replaceable 3v3 spawn poses, temporary first-person walking and an immediately reviewable preview.
1. Author the reusable gym prefab at one unit per metre with a level floor and solid boundaries.
2. Separate the scenario registry, preview actors and desktop controller. Validate six distinct ground-level poses, anchor access and independent replacement.
3. Open the serialized scene, exercise motion, rotation, reset and collision in play mode, render both views and build the Windows preview.
