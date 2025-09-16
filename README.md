# Writer Tool

A tool for writing content using AI. It has rejection (e.g. "I can't help with that") detection and translation capabilities.

## Usage

```bash
dotnet run --project Cli/WriterTool.Cli.csproj -- -c config.toml -o output.txt -ot translated.txt
```

[Example Configuration](example-config.toml)

## TODO

- [ ] Publish NativeAOT Executable
- [ ] SkipOriginal on translation
