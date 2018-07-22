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
			foreach (var buffer in buffers)
				yield return new MmlInputSource (buffer.Key, new StringReader (buffer.Value.ToString ()));
		}

		public void Compile ()
		{
			var compiler = new MmlCompiler ();
			tokens = compiler.TokenizeInputs (SkipDefaultMmlFiles, GetInputs ());
			semantic_tree = compiler.BuildSemanticTree (tokens);
		}

		protected override VoidResult<ResponseError> Shutdown ()
		{
			return VoidResult<ResponseError>.Success ();
		}

		#endregion

		#region editor buffer and text document change receivers

		Dictionary<string, StringBuilder> buffers = new Dictionary<string, StringBuilder> ();

		void EnsureDocumentOpened (Uri uri, string text)
		{
			if (buffers.All (a => !FileMatches (uri, a.Key)))
				buffers [uri.LocalPath] = new StringBuilder (text ?? File.ReadAllText (uri.LocalPath));
		}

		protected override void DidOpenTextDocument (DidOpenTextDocumentParams @params)
		{
			var p = @params;
			//Console.Error.WriteLine ("DidOpenTextDocument:::" + p.textDocument.uri.LocalPath);
			EnsureDocumentOpened (p.textDocument.uri, p.textDocument.text);
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
			buffers.Remove (@params.textDocument.uri.LocalPath);
		}

		#endregion

		#region feature provider implementation

		protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize (InitializeParams @params)
		{
			return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success (
				new InitializeResult () {
					capabilities = new ServerCapabilities () {
						//definitionProvider = true,
						documentSymbolProvider = true,
						//hoverProvider = true,
					}
				});
		}

		protected override Result<ArrayOrObject<Location, Location>, ResponseError> GotoDefinition (TextDocumentPositionParams @params)
		{
			return base.GotoDefinition (@params);
		}

		static Position ToPosition (MmlLineInfo li) => li == null ? null : new Position { line = li.LineNumber, character = li.LinePosition };

		static Range ToRange (MmlLineInfo start, MmlLineInfo end) => new Range { start = ToPosition (start), end = ToPosition (end) };

		string last_rel, last_full;
		string last_matched;
		bool FileMatches (Uri uri, string file)
		{
			if (file == null)
				return false;
			if (!object.ReferenceEquals (last_rel, file)) { // object comparison
				last_rel = file;
				last_full = Path.GetFullPath (file);
				if (last_full == uri.LocalPath) // string comparison
					last_matched = file;
			}
			return object.ReferenceEquals (last_matched, file); // object comparison
		}

		protected override Result<SymbolInformation [], ResponseError> DocumentSymbols (DocumentSymbolParams @params)
		{
			var p = @params;
			EnsureDocumentOpened (p.textDocument.uri, null);
			Compile ();
			var results = new List<SymbolInformation> ();
			if (p.textDocument == null)
				throw new Exception ("textDocument");
			if (p.textDocument.uri == null)
				throw new Exception ("textDocument.uri");

			foreach (var macro in semantic_tree.Macros.Where (m => FileMatches (p.textDocument.uri, m.Location.File))) {
				results.Add (new SymbolInformation {
					kind = SymbolKind.Function,
					name = macro.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = p.textDocument.uri, range = ToRange (macro.Location, null) }
				});
			}
			foreach (var variable in semantic_tree.Variables.Values.Where (v => FileMatches (p.textDocument.uri, v.Location?.File))) {
				results.Add (new SymbolInformation {
					kind = SymbolKind.Variable,
					name = variable.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = p.textDocument.uri, range = ToRange (variable.Location, null) }
				});
			}
			//Console.Error.WriteLine ("DocumentSymbols for " + p.textDocument.uri.LocalPath);
			//foreach (var item in results)
			//	Console.Error.WriteLine ($"RESULT: {item.location} {item.kind} {item.name}");
			return Result<SymbolInformation [], ResponseError>.Success (results.ToArray ());
		}

		#endregion
	}
}
