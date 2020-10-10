/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';

import * as path from 'path';
import * as os from 'os';
import * as fs from 'fs';
import * as events from 'events';
import * as child_process from 'child_process';
import * as vscode from 'vscode';
import * as rx from 'rx-lite';

import { /*workspace,*/ ExtensionContext } from 'vscode';
//import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';

const mugene_scheme = "mugene";

var diagnostics : vscode.DiagnosticCollection;

class MugeneTextDocumentContentProvider implements vscode.TextDocumentContentProvider, vscode.Disposable {
	private _onDidChange = new vscode.EventEmitter<vscode.Uri> ();
	private _emitter = new events.EventEmitter ();

	private _subscription = rx.Observable.fromEvent<vscode.TextDocumentChangeEvent> (this._emitter, "data")
		.sample (rx.Observable.interval (1000))
		.subscribe (event => {
			if (event.document === vscode.window.activeTextEditor.document) {
				this.update (getSpecialSchemeUri (event.document.uri));
			}
		});

	public dispose () {
		this._subscription.dispose ();
	}

	public provideTextDocumentContent (uri: vscode.Uri): string | Thenable<string> {
		return vscode.workspace.openTextDocument (vscode.Uri.parse (uri.query)).then (doc => {
			return this.convert (doc);
		});
	}

	get onDidChange(): vscode.Event<vscode.Uri> {
		return this._onDidChange.event;
	}

	public update (uri: vscode.Uri) {
		this._onDidChange.fire (uri);
	}

	private convert (document: vscode.TextDocument): string | Promise<string> {
		return new Promise ((resolve, rejected) => {
			processDocument (document).then (
                buf => resolve (buf),
				reason => rejected (reason)
			);
		});
	}
}

// Now I figured out that MSBuild compatible output syntax is kind of shit... but anyways.
function getValidFilePathFromCompilerOutput (s: string) : string {
	var parts = s.split(" (");
	var path = "";
	for (var idx in parts) {
		var part = parts [idx];
		var test = path + (path == "" ? "" : " (") + part;
		try {
			var stat = fs.statSync(test);
			if (stat.isFile) {
				path = test;
				continue;
			}
			return path;
		} catch (err) {
			return path;
		}
	}
	return null;
}

function showPreview (uri: vscode.Uri) {
	if (!(uri instanceof vscode.Uri)) {
		if (vscode.window.activeTextEditor) {
			uri = vscode.window.activeTextEditor.document.uri;
		}
	}
	return vscode.commands.executeCommand ('vscode.previewHtml', getSpecialSchemeUri (uri), vscode.ViewColumn.Two);
}

function getSpecialSchemeUri (uri: any): vscode.Uri {
	return uri.with({
		scheme: mugene_scheme,
		path: uri.path,
		query: uri.toString ()
	});
}

let line_column_regex = / \(([0-9]+),\s([0-9]+)\)\s*:\s*([a-zA-Z]+)\s*:\s*(.*)/;

function compileMugene (uri: vscode.Uri, context : ExtensionContext) {
	if (!(uri instanceof vscode.Uri)) {
		if (vscode.window.activeTextEditor) {
			uri = vscode.window.activeTextEditor.document.uri;
		}
	}

	// The server is implemented in C#
	let mugeneExePath = context.asAbsolutePath(path.join('out', 'tools', 'mugene', 'mugene.exe'));
	let mugeneCommand = (os.platform() === 'win32') ? mugeneExePath : "mono";
	let arg = (os.platform() === 'win32') ? "" : mugeneExePath;
		
	var reports = new Array<vscode.Diagnostic> ();
	var cwd = path.dirname(uri.fsPath);
	if (arg == "")
		var proc = child_process.spawn (mugeneCommand, ["--verbose", uri.fsPath], {cwd: cwd});
	else
		var proc = child_process.spawn (mugeneCommand, [arg, "--verbose", uri.fsPath], {cwd: cwd});
	proc.on("exit", (code, _) => {
		if (code == 0) {
		    vscode.window.showInformationMessage("mugene successfully finished");
		} else {
	    	vscode.window.showInformationMessage("failed to run mugene, at exit code " + code);
		}
		if (diagnostics != null)
			diagnostics.dispose();
		diagnostics = vscode.languages.createDiagnosticCollection("mugene");
		diagnostics.set (uri, reports);
	});
	proc.stdout.on("data", (msg) => {
		reports.push(parseCompilerOutput(uri.fsPath, msg));
	});
	proc.stderr.on("data", (msg) => {
		reports.push(parseCompilerOutput(uri.fsPath, msg));
	});
}

function parseCompilerOutput(contextFile: string, msg: String | Buffer): vscode.Diagnostic {
	var file = getValidFilePathFromCompilerOutput(msg.toString());
	var line = 0;
	var col = 0;
	var type = vscode.DiagnosticSeverity.Information;
	if (file == contextFile) {
		msg = msg.slice (file.length); // " (n,m) error: msg"
		var match = line_column_regex.exec(msg.toString());
		if (match != null) {
			line = Number.parseInt (match [1]) - 1;
			col = Number.parseInt (match [2]) - 1;
			switch (match [3].toLowerCase()) {
				case "error": type = vscode.DiagnosticSeverity.Error; break;
				case "warning": type = vscode.DiagnosticSeverity.Warning; break;
			}
			msg = match [4];
		}
	}
	return new vscode.Diagnostic(new vscode.Range (line, col, line, col), msg.toString(), type);
}

function processDocument (_: vscode.TextDocument) : Promise<string> {
    // process vexflow
    return Promise.resolve ("done");
}


export function activate(context: ExtensionContext) {
	activateCompiler(context)
	activatePreview(context)
	// FIXME: enable it once issue is fixed
	//activateLSP(context)
}

function activateCompiler(context: ExtensionContext) {
	// Compile command
	let compileCommand = vscode.commands.registerCommand ("mugene.compile", uri => compileMugene (uri, context));

	context.subscriptions.push(compileCommand);
}

function activatePreview(context: ExtensionContext) {

	let previewDocProvider = new MugeneTextDocumentContentProvider ();
	vscode.workspace.onDidChangeTextDocument ((event: vscode.TextDocumentChangeEvent) => {
		if (event.document === vscode.window.activeTextEditor.document) {
			previewDocProvider.update (getSpecialSchemeUri (event.document.uri));
		}
	});
	let registration = vscode.workspace.registerTextDocumentContentProvider (mugene_scheme, previewDocProvider);
	let cmd = vscode.commands.registerCommand ("mugene.showPreview", uri => showPreview (uri), vscode.ViewColumn.Two);
	context.subscriptions.push (cmd, registration, previewDocProvider);

}

// FIXME: the LSP server somehow causes 100% CPU usage on mono. Disable it until the issue gets fixed.
/*
function activateLSP(context: ExtensionContext) {

	// The server is implemented in C#
	let serverCommand = context.asAbsolutePath(path.join('out', 'server', 'mugene.languageserver.tool.exe'));
	let commandOptions = { stdio: 'pipe' };
	
	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	let serverOptions: ServerOptions =
		(os.platform() === 'win32') ? {
			run : { command: serverCommand, options: commandOptions },
			debug: { command: serverCommand, options: commandOptions }
		} : {
			run : { command: 'mono', args: ["--debug", serverCommand], options: commandOptions },
			debug: { command: 'mono', args: ["--debug", serverCommand], options: commandOptions }
		}
	
	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{scheme: 'file', language: 'mugene'}],
		synchronize: {
			// Synchronize the setting section 'languageServerExample' to the server
			configurationSection: 'mugene',
			// Notify the server about file changes to '.clientrc files contain in the workspace
			fileEvents: workspace.createFileSystemWatcher('** /.clientrc')
		}
	}
	
	// Create the language client and start the client.
	let lsp = new LanguageClient('mugene', 'mugene Language Server', serverOptions, clientOptions).start();
	
	// Push the disposable to the context's subscriptions so that the 
	// client can be deactivated on extension deactivation
	context.subscriptions.push(lsp);
}
*/
