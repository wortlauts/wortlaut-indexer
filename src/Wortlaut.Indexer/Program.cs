using System.CommandLine;
using Wortlaut.Indexer.Cli;

var rootCommand = new RootCommand("Wortlaut indexer: loads Bundestag plenary protocols and indexes their speeches")
{
    DownloadCommand.Create(),
    ImportCommand.Create(),
};

return await rootCommand.Parse(args).InvokeAsync();
