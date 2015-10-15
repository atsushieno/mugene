using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Commons.Music.Midi.Mml;
using NUnit.Framework;

namespace Commons.Music.Midi.Mml.Tests
{
	[TestFixture]
	public class IntegratedTest
	{
		[Test]
		public void CompileAllSamples ()
		{
			var dir = new Uri (Assembly.GetExecutingAssembly ().CodeBase).LocalPath;
			while (true) {
				var parent = Directory.GetParent (dir).FullName;
				if (parent == dir || parent.Length == 0)
					throw new Exception ("samples not found");
				if (Directory.Exists (Path.Combine (dir, "samples")))
					break;
				dir = parent;
			}
				
			foreach (var file in Directory.GetFiles (Path.Combine (dir, "samples"), "*.mml")) {
				if (!IsBlacklisted (file)) {
					Console.Error.WriteLine ("compiling {0} ...", file);
					MmlTestUtility.TestCompile (file, File.ReadAllText (file));
				}
			}
		}
		
		bool IsBlacklisted (string file)
		{
			return Path.GetFileName (file) == "evy1.mml";
		}
	}
}

