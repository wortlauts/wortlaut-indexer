# Test data

Small samples of official Bundestag plenary protocols (amtliche Werke, not protected by copyright under section 5 UrhG; see the [Bundestag imprint](https://www.bundestag.de/services/impressum)). Source: [Bundestag Open Data](https://www.bundestag.de/services/opendata).

| File | Source protocol | Period, session |
| --- | --- | --- |
| `protocols/21-108.sample.xml` | `21108.xml` | 21, 108 (12 Nov 2026) |
| `protocols/20-214.sample.xml` | `20214.xml` | 20, 214 (18 Mar 2025) |
| `protocols/19-239.sample.xml` | `19239.xml` | 19, 239 (7 Sep 2021) |
| `protocols/dbtplenarprotokoll.dtd` | official DTD, version 1.0.2 (5 Sep 2023) | valid from period 19 |

## How the samples were made

Each file is the original protocol, reduced to a few kilobytes: the table of contents, appendices and speaker list are cut down, and only one agenda item with three speeches is kept. The speeches were picked to cover the interesting cases: one with interjections of the presiding officer and two with named interjections and applause. Everything else (attributes, element names, text of the kept speeches) is unchanged.

The samples validate against the DTD, except `19-239.sample.xml`, where the fixed `vertrieb` attribute differs in the original protocol already. That is intentional: the parser has to cope with it.
