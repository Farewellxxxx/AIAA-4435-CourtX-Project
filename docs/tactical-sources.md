# Tactical references and asset provenance

Reviewed 2026-11-07. This prototype uses original C# finite-state and geometry logic. No external training model, dataset, animation pack or basketball repository code is bundled.

| Reference | License / use | Relevance and limits |
| --- | --- | --- |
| [BasketWorld](https://github.com/EvanZ/basketworld), [license](https://github.com/EvanZ/basketworld/blob/main/LICENSE.md) | MIT, copyright Evan Zamir 2025. Conceptual reference only; no copied code/assets. | Public half-court multi-agent simulator, role-based offense/defense, passing and turnovers. Its hex-grid Gymnasium/PPO implementation is not a Unity movement controller; importing the training stack would not directly solve this prototype. |
| [FIBA / WABC coaching manuals](https://about.fiba.basketball/en/wabc-documents), [Level 2](https://assets.fiba.basketball/image/upload/documents-corporate-wabc-coaching-manual-level-2-eng.pdf) | Coaching reference; no diagrams or document text redistributed. | High-post reception, pressure release and a cut to the basket inform the second drill. Our distances, timings and role rules are deliberately simplified local 3v3 exercises. |
| [NBA 2K26 official gameplay announcement](https://newsroom-anz.2k.com/news/nba-2k26-debuts-new-gen-9-gameplay-improvements-including-an-all-new-dynamic-motion-engine-powered-by-proplay) | Public product description; no source-code or asset license. | Describes reactive offensive/defensive AI and directional screens. Used as interaction inspiration, not as evidence of or access to 2K's internal algorithm. |

The current gym, floor textures, primitive character models, ball seams, procedural poses and new tactical logic are authored within CourtX. Existing Unity built-in components remain under Unity's applicable terms. Number textures are generated locally using an installed font; font binaries are not copied into this repository. The UI requests Microsoft YaHei / Arial at runtime on Windows.

The original sketch depicts five defenders in a 2–3 zone. A 3v3 drill cannot reproduce that complete defense. Here three defenders use man assignments, a short hedge at the screen, weak-side help and recovery. The implementation is named accordingly.

No third-party code license is assigned to the user's whole project by this reference list. Before importing an external model or code later, record its exact revision, license, attribution and redistribution terms here.
