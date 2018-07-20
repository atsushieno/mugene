using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LanguageServer;
using LanguageServer.Json;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters.Workspace;

namespace Commons.Music.Midi.Mml
{
	public class MugeneLanguageService
	{
		public static void Main ()
		{
			new MugeneServiceConnection (Console.OpenStandardInput (), Console.OpenStandardOutput ()).Listen ().Wait ();
		}
	}

	public class MugeneServiceConnection : ServiceConnection {
		public MugeneServiceConnection (Stream input, Stream output)
			: base (input, output)
		{
		}


		#region compiler object model
		MmlTokenSet tokens;
		MmlSemanticTreeSet semantic_tree;

		public bool SkipDefaultMmlFiles { get; set; }

		IEnumerable<MmlInputSource> GetInputs ()
		{
			yield break;
		}

		public void Compile ()
		{
			var compiler = new MmlCompiler ();
			tokens = compiler.TokenizeInputs (SkipDefaultMmlFiles, GetInputs ());
			semantic_tree = compiler.BuildSemanticTree (tokens);
		}

		#endregion

		#region editor buffer and text document change receivers

		Dictionary<string, StringBuilder> buffers = new Dictionary<string, StringBuilder> ();

		protected override void DidOpenTextDocument (DidOpenTextDocumentParams @params)
		{
		}

		static StringBuilder Replace (StringBuilder sb, Range range, string text)
		{
			if (range.start.line > range.end.line)
				return sb; // invalid range, do nothing.
			if (range.start.character > int.MaxValue || range.end.character > int.MaxValue)
				return sb; // almost impossible, but we cannot handle such a big buffer. do nothing.

			int lineStart = 0, lineEnd = 0;
			int startPos = 0, endPos = 0;
			if (range.start.line > 0) {
				while (startPos < sb.Length) {
					if (sb [startPos++] == '\n') {
						lineStart++;
						if (lineStart == range.start.line)
							break;
					}
				}
			}
			if (startPos == sb.Length)
				return sb; // no such range, do nothing

			lineEnd = lineStart;
			startPos += (int) range.start.character;
			endPos = startPos;
			if (range.end.line > range.start.line) {
				while (startPos < sb.Length) {
					if (sb [endPos++] == '\n') {
						lineEnd++;
						if (lineEnd == range.end.line)
							break;
					}
				}
			}
			endPos += (int) range.end.character;

			sb.Remove (startPos, endPos - startPos);
			sb.Insert (startPos, text);

			return sb;
		}

		protected override void DidChangeTextDocument (DidChangeTextDocumentParams @params)
		{
			var p = @params;
			// FIXME: consider versions?
			var path = p.textDocument.uri.LocalPath;
			foreach (var c in p.contentChanges) {
				if (c.range == null || c.rangeLength == null)
					buffers [path] = new StringBuilder (c.text);
				else
					buffers [path] = buffers.TryGetValue (path, out var b) ? Replace (b, c.range, c.text) : new StringBuilder (c.text);
			}
		}

		protected override void DidChangeWatchedFiles (DidChangeWatchedFilesParams @params)
		{
			var p = @params;
			foreach (var c in p.changes) {
				switch (c.type) {
				case FileChangeType.Changed:
					buffers [c.uri.LocalPath] = new StringBuilder (File.ReadAllText (c.uri.LocalPath));
					break;
				case FileChangeType.Deleted:
					buffers.Remove (c.uri.LocalPath);
					break;
				}
			}
		}

		protected override void DidCloseTextDocument (DidCloseTextDocumentParams @params)
		{
		}

		#endregion

		#region feature provider implementation

		protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize (InitializeParams @params)
		{
			return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success (
				new InitializeResult () {
					capabilities = new ServerCapabilities () {
						definitionProvider = true,
						documentSymbolProvider = true,
						hoverProvider = true,
					}
				});
		}

		protected override Result<ArrayOrObject<Location, Location>, ResponseError> GotoDefinition (TextDocumentPositionParams @params)
		{
			return base.GotoDefinition (@params);
		}

		static Position ToPosition (MmlLineInfo li) => li == null ? null : new Position { line = li.LineNumber, character = li.LinePosition };

		static Range ToRange (MmlLineInfo start, MmlLineInfo end) => new Range { start = ToPosition (start), end = ToPosition (end) };

		protected override Result<SymbolInformation [], ResponseError> DocumentSymbols (DocumentSymbolParams @params)
		{
			var p = @params;
			Compile ();
			var results = new List<SymbolInformation> ();
			// FIXME: location should not be the first data location.
			foreach (var macro in semantic_tree.Macros.Where (m => p.textDocument.uri.LocalPath == m.Location.File)) {
				results.Add (new SymbolInformation {
					kind = SymbolKind.Function,
					name = macro.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = p.textDocument.uri, range = ToRange (macro.Location, null) }
				});
			}
			foreach (var variable in semantic_tree.Variables.Where (v => p.textDocument.uri.LocalPath == v.Location.File)) {
				results.Add (new SymbolInformation {
					kind = SymbolKind.Variable,
					name = variable.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = p.textDocument.uri, range = ToRange (variable.Location, null) }
				});
			}
			return Result<SymbolInformation [], ResponseError>.Success (results.ToArray ());
		}

		protected override Result<Hover, ResponseError> Hover (TextDocumentPositionParams @params)
		{
			return base.Hover (@params);
		}

		#endregion
	}
}
