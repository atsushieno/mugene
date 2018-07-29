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
		// FIXME: compiler should have one.
		string project_directory = Directory.GetCurrentDirectory ();
		MmlTokenSet tokens;
		MmlSemanticTreeSet semantic_tree;

		public bool SkipDefaultMmlFiles { get; set; }

		IEnumerable<MmlInputSource> GetInputs ()
		{
			foreach (var buffer in buffers)
				yield return new MmlInputSource (buffer.Key, new StringReader (buffer.Value.ToString ()));
		}

		MmlCompiler CreateCompiler () => new MmlCompiler ();

		public void Compile ()
		{
			var compiler = CreateCompiler ();
			tokens = compiler.TokenizeInputs (SkipDefaultMmlFiles, GetInputs ());
			semantic_tree = compiler.BuildSemanticTree (tokens);
		}

		protected override void Exit ()
		{
			// FIXME: This isn't called.
		}

		protected override VoidResult<ResponseError> Shutdown ()
		{
			// FIXME: This isn't either. WTF?
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
			var p = @params;
			if (p.rootUri != null)
				EnsureDocumentOpened (p.rootUri, null);

			return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success (
				new InitializeResult () {
					capabilities = new ServerCapabilities () {
						definitionProvider = true,
						documentSymbolProvider = true,
						workspaceSymbolProvider = true,
						//hoverProvider = true,
					}
				});
		}

		MmlToken FindToken (Uri uri, Position pos, MmlTokenType typeFilter = default (MmlTokenType))
		{
			using (
				var log = TextWriter.Null) {//File.CreateText ("/home/atsushi/Desktop/log.txt")) {
				log.WriteLine ($"[FindToken] SEARCH POINT: {uri.LocalPath} ({pos.line}, {pos.character})");
				foreach (var t in tokens.Macros.SelectMany (m => m.Tokens).Concat (tokens.Tracks.SelectMany (t => t.Tokens))) {
					if (typeFilter != default (MmlTokenType) && t.TokenType != typeFilter)
						continue;
					log.WriteLine ($"[FindToken] - vs. {t.Location.File} ({t.Location.LineNumber}, {t.Location.LinePosition} - {t.Value}) ::: {FileMatches (uri, t.Location.File)}/{t.Location.LineNumber - 1 == pos.line}/{t.Location.LinePosition - 1 <= pos.character}/{pos.character < t.Location.LinePosition - 1 + t.Value?.ToString ()?.Length}");
					if (FileMatches (uri, t.Location.File)
					    && t.Location.LineNumber - 1 == pos.line
					    && t.Location.LinePosition - 1 <= pos.character
					    && pos.character < t.Location.LinePosition - 1 + t.Value?.ToString ()?.Length) {
						log.WriteLine ($"[FindToken] FOUND: {t.TokenType}: {t.Value} ({t.Location.LineNumber}, {t.Location.LinePosition})");
						return t;
					}
				}
			}
			return null;
		}

		protected override Result<ArrayOrObject<Location, Location>, ResponseError> GotoDefinition (TextDocumentPositionParams @params)
		{
			Compile ();

			var p = @params;
			var result = new ArrayOrObject<Location, Location> ();
			var token = FindToken (p.textDocument.uri, p.position, MmlTokenType.Identifier);
			if (token == null)
				return Result<ArrayOrObject<Location, Location>, ResponseError>.Success (new Location [0]); // empty

			var name = token.Value.ToString ();

			var m = semantic_tree.Macros.FirstOrDefault (_ => _.Name == name);
			if (m != null) {
				var loc = new Location {
					Uri = GetUri (m.Location.File, true),
					range = new Range {
						start = new Position { line = m.Location.LineNumber - 1, character = m.Location.LinePosition - 1 },
						end = new Position { line = m.Location.LineNumber - 1, character = m.Location.LinePosition - 1 + m.Name.Length }
					}
				};
				using (var log = TextWriter.Null)// File.AppendText ("/home/atsushi/Desktop/log.txt"))

					log.WriteLine ($"-> {loc.Uri.LocalPath} ({loc.range.start.line}, {loc.range.start.character})");
				return Result<ArrayOrObject<Location, Location>, ResponseError>.Success (loc);
			}

			return Result<ArrayOrObject<Location, Location>, ResponseError>.Success (new Location [0]);
		}

		// our comipler line number != LSP expected line number
		static Position ToPosition (MmlLineInfo li) => li == null ? null : new Position { line = li.LineNumber - 1, character = li.LinePosition - 1 };

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

		void AddSymbols (List<SymbolInformation> list, Func<MmlSemanticMacro, bool> macroFilter, Func<MmlSemanticVariable, bool> variableFilter)
		{
			foreach (var macro in semantic_tree.Macros.Where (m => macroFilter (m))) {
				list.Add (new SymbolInformation {
					kind = SymbolKind.Function,
					containerName = macro.Location.File, // FIXME: specifying full path still doesn't open default macros...
					name = macro.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = GetUri (macro.Location.File, true), range = ToRange (macro.Location, macro.Data.Last ().Location) }
				});
			}
			foreach (var variable in semantic_tree.Variables.Values.Where (v => variableFilter (v) && v.Location != null)) {
				list.Add (new SymbolInformation {
					kind = SymbolKind.Variable,
					containerName = variable.Location.File, // FIXME: specifying full path still doesn't open default macros...
					name = variable.Name,
					// FIXME: last location should be of an end of the token.
					location = new Location { Uri = GetUri (variable.Location.File, true), range = ToRange (variable.Location, variable.Location) }
				});
			}
		}

		Dictionary<string, Uri> uri_cache = new Dictionary<string, Uri> ();

		Uri GetUri (string filepath, bool absoluteRequired = false)
		{
			if (uri_cache.TryGetValue (filepath, out var uri))
			    return uri;
			uri_cache [filepath] = new Uri ("file://" + Path.Combine (project_directory, filepath));
			if (absoluteRequired)
				filepath = Path.IsPathRooted (filepath) ? filepath :
					       MmlCompiler.DefaultIncludes.Contains (filepath) ? Path.Combine (Path.GetDirectoryName (new Uri (GetType ().Assembly.CodeBase).LocalPath), "mml", filepath) :
					       Path.GetFullPath (filepath);
			return GetUri (filepath);
		}

		protected override Result<SymbolInformation [], ResponseError> Symbol (WorkspaceSymbolParams @params)
		{
			var p = @params;
			Compile ();
			var results = new List<SymbolInformation> ();
			AddSymbols (results, m => m.Name.Contains (p.query), v => v.Name.Contains (p.query));
			return Result<SymbolInformation [], ResponseError>.Success (results.ToArray ());
		}

		protected override Result<SymbolInformation [], ResponseError> DocumentSymbols (DocumentSymbolParams @params)
		{
			var p = @params;
			EnsureDocumentOpened (p.textDocument.uri, null);
			Compile ();
			var results = new List<SymbolInformation> ();
			AddSymbols (results, m => FileMatches (p.textDocument.uri, m.Location.File), v => FileMatches (p.textDocument.uri, v.Location?.File));
			return Result<SymbolInformation [], ResponseError>.Success (results.ToArray ());
		}

		#endregion
	}
}
