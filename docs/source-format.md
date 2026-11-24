# Source format: Bundestag plenary protocols (XML)

Findings from reading real protocols of legislative periods 21, 20 and 19 against the official DTD (`dbtplenarprotokoll.dtd`, version 1.0.2, valid from period 19). Samples are in [`testdata/protocols`](../testdata/protocols). Checked on 2026-11-24 with session 108 (period 21), 214 (period 20) and 239 (period 19).

## Where the files come from

- The [Bundestag Open Data page](https://www.bundestag.de/services/opendata) lists one XML file per session, for example `https://www.bundestag.de/resource/blob/1233202/21108.xml`. Archive files per period (for example `pp19.zip`) exist for older periods.
- The file lists on that page are loaded dynamically from `https://www.bundestag.de/ajax/filterlist/de/services/opendata/<list-id>?offset=<n>`. This endpoint is undocumented and can change, so the downloader must not be the only way to get data: the importer also accepts a local directory of XML files.
- The blob id in the URL changes with every revision, so URLs are not stable identifiers. Use the protocol's own `wahlperiode` and `sitzung-nr` instead.
- A whole protocol is 0.7 to 1.6 MB of XML.

## Structure

All three periods share the same structure.

```text
dbtplenarprotokoll  (attributes: wahlperiode, sitzung-nr, sitzung-datum dd.mm.yyyy, status, version, ...)
  vorspann          header data and table of contents
  sitzungsverlauf   sitzungsbeginn, tagesordnungspunkt+, sitzungsende
    tagesordnungspunkt (top-id)   top-titel + rede*
      rede (id)
        p klasse="redner"  <redner id=...><name>...</name></redner>  "Name, role:"
        p klasse="J_1" | "J" | "O"   paragraphs of the speech
        kommentar                    reactions, interjections
        name + p                     the presiding officer speaks (not the speaker)
  anlagen           appendices, includes speeches given "zu Protokoll"
  rednerliste       list of all speakers of the session
```

## Things the parser has to handle

- **A `rede` is not only the speaker's text.** The presiding officer's interjections ("Präsidentin Julia Klöckner: Aber Ihre Zeit!") are `name` plus `p` elements inside the same `rede`, and the speaker resumes with a new `p klasse="redner"`. The closing remarks of the president after the speech ("Ich danke Ihnen. - Für die AfD-Fraktion hat nun ... das Wort") sit inside the speech as well. The text of a speech must be assembled from the speaker's segments only.
- **Speeches in `anlagen`.** Speeches given in writing are `rede` elements in the appendix. The DTD has a `redeart` attribute, but it is not set in any sample, so it cannot be used to tell the cases apart. Decide by position (`sitzungsverlauf` or `anlagen`) and keep that as a field.
- **Reactions are `kommentar` elements** and come in these shapes:
  - applause: `(Beifall bei der CDU/CSU sowie bei Abgeordneten der SPD)`, also `Lebhafter Beifall`, `Anhaltender Beifall`
  - named interjection: `(Nicole Gohlke [Die Linke]: Eine linke Schulreform!)`, sometimes with an addressee: `(Name [Group], an die AfD gewandt: ...)`
  - unnamed: `Zuruf`, `Zurufe`, `Lachen`, `Heiterkeit`, `Widerspruch`
  - combined: several parts joined by ` – `, for example `(Beifall bei der FDP sowie bei Abgeordneten der AfD – Friedrich Merz [CDU/CSU]: Herr Vogel!)`
  - stage directions that are not reactions: `(Abg. X [AfD] meldet sich zu einer Zwischenfrage)`, `(Die Abgeordneten ... erheben sich)`

  A comment therefore has to be split on ` – ` and each part classified. The share of named interjections is large (about a third of all comments in the samples), so heckle search is worth the effort.
- **Speaker data.** `redner/@id` is the speaker id; `name` has `titel`, `vorname`, `nachname`, `namenszusatz`, `ortszusatz` (for example `Carsten Schneider [Erfurt]`), `fraktion` and `rolle` (`rolle_lang`, `rolle_kurz`). The XML knows the parliamentary group (`fraktion`), not the party. Government members and guests have a `rolle` and usually no `fraktion`. The spelling varies (`Die Linke`, `DIE LINKE`, `Fraktionslos`, `fraktionslos`, a non-breaking space in `BÜNDNIS 90/DIE GRÜNEN`), so values are normalised before they go into filters and facets.
- **Agenda item.** The title of the surrounding `tagesordnungspunkt` is the `agenda_item`.
- **Ids.** `rede/@id` looks like `ID2110800600` (period 21, session 108, running number 00600) and is unique within a protocol. A stable global id can be built as `<period>-<session>-<number>`.
- **Revisions.** The root has `status` (`erfasst`, `korrektur`, `freigegeben`) and `version`. Whether speech numbers stay stable between revisions is not verified.
- **The DTD is not strictly followed.** The sample of period 19 fails validation because the fixed `vertrieb` attribute differs. The parser must be tolerant and must not rely on DTD validation.

## Open points

- The index has a `speaker.faction` field, not a party (see ADR 0004). A `party` field needs the member master data (`MdB-Stammdaten.zip` on the Open Data page).
- Questions of other members and the words of the presiding officer are not indexed yet.
- Verify that revisions keep speech ids stable before relying on id plus content hash for idempotency.
- Check periods before 19: they are known to differ in format and are out of scope for phase 1.
