using Agibuild.Fulora.Cli;

var rootCommand = CliRootCommand.Create();
return await rootCommand.Parse(args).InvokeAsync();
