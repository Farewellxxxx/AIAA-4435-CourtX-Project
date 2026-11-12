# Independent read-only review

Reviewed against base 0d6fd73 on 2026-11-12 using a fresh context. No Critical issue. One Important issue identified: nearest-centre hit testing can select a covered player instead of the foreground visible badge when players overlap. Reproduction: Switch/role0 at 3.5 s, 1920x1080, pointer (819,545), 12 should be selected rather than 04.

The reviewer verified scale/letterbox mapping, trail time clipping and duplicate timestamp handling, question freeze/answer-gated target rendering, focus reset on record switches, and compile guards for QA. Actual images showed readable trail contrast and distinct ball/target styling.

The reviewer did not run native input, verify final release bytes or old hashes, or certify the running suites. These remain the implementer's explicit final checks. Display fixtures do not establish learning effectiveness or user preference.

Fix verification and final delivery results are recorded in progress.md and delivery.json.

Disposition: fixed the Important issue. The exact overlap failed before the change at normal, half, and letterboxed sizes, and passes afterward. Six picking checks and all 138 existing checks pass on final source. Native mouse selection of foreground 12, selecting 01, repeat-click/reset and timeline drag passed. Release QA classes are absent; all 139 old-build/replay files remain byte-identical. Final QA rerun: 64 PASS, 32 nonblank captures. No deferred Minor findings.
