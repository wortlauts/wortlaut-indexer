# wortlaut-indexer

Part of [Wortlaut](https://github.com/wortlauts/wortlaut), a search engine over German Bundestag speeches.

The indexer reads the official plenary protocols (XML), splits them into speeches and writes them to Elasticsearch. It is the only component that writes to the index and it owns the index template and the document schema.

> **Status:** early. The source format has been analysed (see [docs/source-format.md](docs/source-format.md)); the parser and the import command are not written yet.

## Planned shape

- A .NET CLI: `import --legislative-period 21` loads protocols, parses them, and writes speeches in bulk.
- One Elasticsearch document per speech, in the index `speeches-v1`, read through the alias `speeches`.
- Idempotent imports (speech id plus content hash): a run only writes new or changed speeches.
- A new mapping means a new index (`speeches-v2`) and an alias switch without downtime.
- Releases publish `index-template.json` and the document schema, which the API pins and tests against.

## Repository layout

```text
docs/        notes on the source format
testdata/    small sample protocols and the official DTD, used by the parser tests
```

## Local development

Start Elasticsearch and Kibana with the stack in [wortlaut-infra](https://github.com/wortlauts/wortlaut-infra), then follow the instructions in this README once the CLI exists.

## Data

Source data are official works of the Bundestag and not protected by copyright. See [testdata/README.md](testdata/README.md) for sources.
