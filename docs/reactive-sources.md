# Open-source reuse review — 2026-11-09

| Source | Revision / license | Decision |
|---|---|---|
| [BasketWorld](https://github.com/EvanZ/basketworld) | c9c5112ee4c2a9d603bec6ff0e295c9157b63539, MIT | Port the small distance/facing pressure calculation from `basketworld/envs/core/pressure.py` into continuous-space C#. Use it as a relative pressure indicator, not as a steal probability. Keep physical ball contact authoritative. No Python/JAX/training/server dependencies. |
| [UnityHFSM](https://github.com/Inspiaaa/UnityHFSM) | f8fdc2efb862522acd26fab6cd940886c84cde37, package 2.3.0, MIT | Evaluated README/API/package. Its UPM supports Unity 2019.4+, but this feature only needs three defensive states and explicit timers. Introducing generic nested machines would not simplify it, so no package import or claim of tested runtime compatibility. |
| [NeuralCourt](https://github.com/henry753951/neuralcourt-basketball) | Mixed/unclear whole-repository permissions, Unity 6000.5.3f1 + URP/inference | Architecture reference only. No source, models, animation or characters copied. |
| [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) | Apache-2.0 toolkit | Not needed for local deterministic rules. No trained basketball policy imported. |
| [FIBA/WABC coaching manual](https://assets.fiba.basketball/image/upload/documents-corporate-wabc-coaching-manual-level-2-eng.pdf) | Coaching reference, not a code library | Terminology/reference for screen coverage and help/recovery; prototype rules and tuning are original, not validated coaching assessment. |

Implementation categories: pressure calculation = adapted MIT source; defensive coordinator, teammate rules, physical-risk adapter, journal and review = original CourtX implementation; UnityHFSM/NeuralCourt = evaluated, not dependencies. No complete open-source package was identified that can be dropped into this Unity 2022 project to supply all requested 3v3 behavior.

Pressure adaptation changes: replace hex coordinates and grid distance with planar metres; use continuous facing-to-basket cosine and exponential falloff; expose unitless 0–1 pressure, **not** probability of losing possession. Contact is still decided exclusively by TacticalBall's swept geometry. Preserve the upstream MIT text in `ThirdPartyNotices/BasketWorld-MIT.txt` and the distributed program.
