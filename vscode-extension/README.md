Language support for mugene MML compiler: https://github.com/atsushieno/mugene

This extension sources were originally copied from https://github.com/matarillo/vscode-languageserver-csharp-example except that we have our own mugene language server.
Though there have been some changes and I don't remember what it used to be like anymore.

To build the extension, run `npm install` and `npm run compile`. It will automatically run msbuild to build C# baaed LSP service.
F5 from vscode does not fully build the C# part.

