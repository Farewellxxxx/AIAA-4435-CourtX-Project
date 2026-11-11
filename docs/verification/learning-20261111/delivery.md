# CourtX Learning delivery

Production source: `2a906bc5ef4497105aad7b4af4cb2d7ec6faaa5f`.
Branch: `feature/tactical-learning-hub`.
Prior stable source: `08ce3ae72e02d6f1f82d7782d5f0d23cd0287737`, tag `v0.4-recap-stable-20261111`.

- Run: `Windows_Learning/CourtX Learning.exe` with its whole folder.
- Package: `Preview/Playtests/Learning-20261111/CourtX-Learning-20261111.zip`, 27,930,990 bytes, 133 entries.
- SHA256: `12E332E99CEDC136DFC5FC665A1155354D0AF01BD6C7A1C7936419EC240A517E`.
- Recordings: same Preview folder, `round1-learning.mp4`, `round2-learning.mp4`, `round3-learning.mp4`.
- Guide and results: `docs/LEARNING_GUIDE_ZH.md`, `docs/LEARNING_RESULTS.md`.

## Requirement audit

| Requirement | Evidence |
|---|---|
| Four integrated learning/practice/review pages | LearningHub/UI source; three standalone videos; release-menu and release-empty-compare screenshots |
| Existing two drills, 01/04 roles, six actors | 79 catalog checks and 83 live scenario checks |
| At least six grounded questions | Catalog anchors, feedback checks; native-wrong-feedback screenshot; exact question/lesson lifecycle assertions |
| Actual same-opening A/B | Shared-start validation, actual QA A/B rounds and screenshots; seed/role/geometry/incomplete rejection tests |
| Practice and actual/historical recap | 27 lifecycle checks; native V and scroll; ball/court/six-player render evidence |
| Preserve controls and optional provider interfaces | All 277 pre-existing checks; local mode by default |
| At least three iterations | Round 1/2/3 check files, videos, small-window and long-text screenshots; RED/GREEN bug evidence |
| Independent review | independent-review.md; two material findings fixed in one pass, full 383-check suite green |
| Visible persistence failure and recovery | Blocked-path scene regression; actual final-binary native warning/retry and saved JSON/TXT; release-smoke.json |
| Production binary without QA | release-smoke.json and package-verification.json assembly checks |
| Package and prior builds | All 133 package entries matched. Previous recap148/reactive155 files matched old archives; old tactical assembly unchanged |
| Stable source preserved | Stable tag, source archive in Preview/Backups/Recap-20261111, and preserved previous branches |
| Final test cleanup | Production and isolated smoke copy closed normally (exit 0); no CourtX or task Unity test processes remained |

The new branch and stable tag are the only intended remote updates; existing main and earlier feature heads remain unchanged. Binary packages and raw videos stay local under the existing repository policy. Source, guides and compact verification evidence are committed.

Known minor: R retries live practice, while review uses the visible retry button. No educational-effectiveness claim or live LLM-service validation is made. Implementation rulings and scope limits are in LEARNING_RESULTS.md and independent-review.md. The computer remains on.
