using LanguageServer;
using LanguageServer.Client;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Pipes;

namespace Commons.Music.Midi.Mml.Tests
{
	[TestFixture ()]
	public class Test
	{
		MugeneServiceConnection ls;
		Proxy client;

		[SetUp]
		public void SetUp ()
		{
			var input = new MemoryStream ();
			var output = new MemoryStream ();
			var conn = new Connection (input, output);
			client = new Proxy (conn);
			AnonymousPipeClientStream stream;
		}

		[Test ()]
		public void TestCase ()
		{
		}
	}
}
