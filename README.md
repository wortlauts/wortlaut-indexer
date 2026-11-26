# wortlaut-indexer

Part of [Wortlaut](https://github.com/wortlauts/wortlaut), a search engine over German Bundestag speeches.

The indexer downloads the official plenary protocols (XML), splits them into speeches and writes them to Elasticsearch. It is the only component that writes to the index, and it owns the index template and the document schema.

## Requirements

- .NET 10 SDK
- Docker, for the local Elasticsearch and for the integration tests

## Quick start

Start Elasticsearch and Kibana with the stack from [wortlaut-infra](https://github.com/wortlauts/wortlaut-infra):

```powershell
docker compose -f ../wortlaut-infra/compose/dev/compose.yaml up -d --wait
```

Download the protocols of a legislative period and import them:

```powershell
dotnet run --project src/Wortlaut.Indexer -- download --legislative-period 21
dotnet run --project src/Wortlaut.Indexer -- import --legislative-period 21
```

Then search in Kibana (http://localhost:5601, Dev Tools):

```text
GET speeches/_search
{
  "query": { "match": { "text": "Digitalisierung" } },
  "highlight": { "fields": { "text": {} } }
}
```

## Commands

| Command | Purpose |
| --- | --- |
| `download --legislative-period <n> [--target <dir>]` | Download the protocols of period 19, 20 or 21 into `data/protocols`. Files that are already present are skipped. |
| `import [--legislative-period <n>] [--source <dir>] [--elasticsearch <url>] [--elasticsearch-username <user>] [--index <name>] [--alias <name>]` | Parse the protocols in the source directory and write their speeches to Elasticsearch. |

When Elasticsearch has security enabled, pass `--elasticsearch-username` (or set `ELASTICSEARCH_USERNAME`) and put the password into the `ELASTICSEARCH_PASSWORD` environment variable. The password is never taken from the command line, so it does not show up in the process list.

Imports are idempotent: every speech has a stable id and a content hash, and only new or changed speeches are written. A run over unchanged protocols writes nothing.

## What a document looks like

One document is the text of one speaker within one `rede` of a protocol, with the reactions during these words. The index is `speeches-v1`, read through the alias `speeches`. The mapping is strict and lives in [schema/index-template.json](schema/index-template.json); the decisions behind it are in [ADR 0004](https://github.com/wortlauts/wortlaut/blob/main/docs/adr/0004-speech-document-model.md).

| Field | Content |
| --- | --- |
| `id` | `<period>-<session>-<number>`, for example `21-108-00600` |
| `legislative_period`, `session`, `date` | origin and time |
| `agenda_item` | title of the agenda item |
| `delivery` | `plenary` or `written_statement` |
| `speaker` | `id`, `name`, `faction`, `role` |
| `text` | full text, German analyzer; sub-field `text.exact` for exact phrases |
| `reactions` | nested: `type` (`applause`, `laughter`, `heckle`, `objection`), `source`, `text` |
| `word_count` | to normalise trends |
| `source_url` | link to the original protocol (PDF) |
| `rights` | `license`, `attribution`, `allowed_actions` |
| `content_hash` | SHA-256 of the document, used for idempotent imports |

Notes on the source format are in [docs/source-format.md](docs/source-format.md).

## Container

The `Dockerfile` builds a small image that runs the same commands. Mount a volume at `/data` for the downloaded protocols:

```powershell
docker build -t wortlaut-indexer .
docker run --rm -v protocols:/data wortlaut-indexer download --legislative-period 21 --target /data
docker run --rm -v protocols:/data -e ELASTICSEARCH_PASSWORD wortlaut-indexer import --source /data --elasticsearch http://host.docker.internal:9200
```

## Development

```powershell
dotnet build
dotnet test
```

The integration tests start a real Elasticsearch with Testcontainers and need Docker. They carry the trait `Category=Integration`.

```text
src/Wortlaut.Indexer/     the CLI
  Parsing/                protocol XML to speeches
  Indexing/               Elasticsearch store and the import
  Sources/                download of the protocols
  Cli/                    command definitions
  Model/                  the document model
schema/                   index template, published with every release
tests/                    unit and integration tests
testdata/                 small sample protocols and the official DTD
docs/                     notes on the source format
```

## Known limitations

- The words of the presiding officer and questions of other members during a speech are not indexed yet.
- Members of the government (chancellor, ministers) have no `speaker.faction`, because the protocols do not state one for them. Filtering by faction therefore misses their speeches until the member master data is used.
- Applause is attributed to the phrase of the protocol (for example `CDU/CSU sowie bei Abgeordneten der SPD`), not split into single groups.
- Periods before 19 are not supported. Their protocols have a different format.

## Data

Source data are official works of the Bundestag and not protected by copyright. See [testdata/README.md](testdata/README.md) for sources.
