//
// Copyright (C) 2008-2013 Kody Brown (kody@bricksoft.com).
// 
// MIT License:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace cat.dos
{
	public class BatchFile : ICataloger
	{
		public string Description { get { return _description; } }
		private string _description = "DOS Batch files (.bat .cmd).";

		public bool CanCat( CatOptions catOptions, string fileName )
		{
			string ext = Path.GetExtension(fileName);
			return ext.Equals(".bat", StringComparison.CurrentCultureIgnoreCase)
				|| ext.Equals(".cmd", StringComparison.CurrentCultureIgnoreCase);
		}

		public bool Cat( CatOptions catOptions, string fileName )
		{
			return Cat(catOptions, fileName, 0, long.MaxValue);
		}

		public bool Cat( CatOptions catOptions, string fileName, int lineStart, long linesToWrite )
		{
			BatConfig bdc = new BatConfig();

			int lineNumber;
			int padLen;
			int winWidth = Console.WindowWidth - 1;
			string l, lt;
			Regex varRegex, argRegex;
			Match m;

			lineStart = Math.Max(lineStart, 0);
			lineNumber = 0;
			padLen = catOptions.showLineNumbers ? 3 : 0;
			if (linesToWrite < 0) {
				linesToWrite = long.MaxValue;
			}

			varRegex = new Regex(@"[\!%](?<var>[^%]+)[\!%]", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
			argRegex = new Regex(@"%(?<var>[%]*[\*_~a-zA-Z0-9]*)", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);


			Console.ForegroundColor = bdc.defaultColor;
			Console.BackgroundColor = bdc.defaultBackground;

			using (StreamReader reader = File.OpenText(fileName)) {
				while (!reader.EndOfStream) {
					if (lineNumber >= linesToWrite) {
						break;
					}

					l = reader.ReadLine();
					lt = l.Trim();
					lineNumber++;

					if (lineNumber < lineStart) {
						continue;
					}

					if (catOptions.ignoreLines.Length > 0 && l.StartsWith(catOptions.ignoreLines, StringComparison.CurrentCultureIgnoreCase)) {
						continue;
					} else if (catOptions.ignoreBlankLines && l.Length == 0) {
						continue;
					} else if (catOptions.ignoreWhitespaceLines && lt.Length == 0) {
						continue;
					}

					if (catOptions.showLineNumbers) {
						Console.BackgroundColor = catOptions.lineNumBackColor;
						Console.ForegroundColor = catOptions.lineNumForeColor;
						Console.Write("{0," + padLen + "}", lineNumber);
						Console.BackgroundColor = catOptions.defaultBackColor;
						Console.ForegroundColor = catOptions.defaultForeColor;
					}

					if (lt.StartsWith(":")) {
						int i = l.IndexOf(' ');
						if (i == -1) {
							i = l.Length;
						}

						Console.ForegroundColor = bdc.func;
						Console.Write(l.Substring(0, i));
						Console.ForegroundColor = bdc.defaultColor;
						Console.WriteLine(l.Substring(i).TrimEnd());
						continue;
					}

					//if (lt.StartsWith("@")) {
					//	Console.ForegroundColor = bdc.echoSymbol;
					//	if (catOptions.wrapText) {
					//		Console.WriteLine(Bricksoft.PowerCode.Text.Wrap(l.TrimEnd(), winWidth, 0, padLen));
					//	} else {
					//		Console.WriteLine(l.TrimEnd());
					//	}
					//	Console.ForegroundColor = bdc.defaultColor;
					//	continue;
					//}

					if (lt.IndexOf("rem ") > -1) {
						int i = l.IndexOf("rem ");
						if (i == -1) {
							i = l.Length;
						}

						Console.ForegroundColor = bdc.defaultColor;
						Console.Write(l.Substring(0, i));
						Console.ForegroundColor = bdc.comment;
						if (catOptions.wrapText) {
							Console.WriteLine(Bricksoft.PowerCode.Text.Wrap(l.Substring(i).TrimEnd(), winWidth, 0, padLen));
						} else {
							Console.WriteLine(l.Substring(i).TrimEnd());
						}
						Console.ForegroundColor = bdc.defaultColor;
						continue;
					}

					m = varRegex.Match(l);
					if (m.Success) {
						Console.ForegroundColor = bdc.defaultColor;
						Console.Write(l.Substring(0, m.Index));
						Console.ForegroundColor = bdc.varSymbol;
						Console.Write(m.Groups[0].Value[0]);
						Console.ForegroundColor = bdc.varName;
						Console.Write(m.Groups[1]);
						Console.ForegroundColor = bdc.varSymbol;
						Console.Write(m.Groups[0].Value[0]);
						Console.ForegroundColor = bdc.defaultColor;
						Console.WriteLine(l.Substring(m.Index + m.Groups[1].Length + 2));
						continue;
					}

					m = argRegex.Match(l);
					if (m.Success) {
						Console.ForegroundColor = bdc.defaultColor;
						Console.Write(l.Substring(0, m.Index));
						Console.ForegroundColor = bdc.varSymbol;
						Console.Write("%");
						Console.ForegroundColor = bdc.varName;
						Console.Write(m.Groups[1]);
						Console.ForegroundColor = bdc.defaultColor;
						Console.WriteLine(l.Substring(m.Index + m.Groups[1].Length + 1));
						continue;
					}

					if (lt.Length > 0) {
						if (catOptions.wrapText) {
							Console.WriteLine(Bricksoft.PowerCode.Text.Wrap(l.TrimEnd(), winWidth, 0, padLen));
						} else {
							Console.WriteLine(l.TrimEnd());
						}
					} else {
						Console.WriteLine("  ");
					}
				}

				reader.Close();
			}

			Console.ForegroundColor = bdc.originalColor;
			Console.BackgroundColor = bdc.originalBackground;

			return true;
		}

		public class BatConfig
		{
			public ConsoleColor debugColor { get; set; }

			public ConsoleColor defaultColor { get; set; }
			public ConsoleColor originalColor { get; set; }
			public ConsoleColor defaultBackground { get; set; }
			public ConsoleColor originalBackground { get; set; }

			public ConsoleColor func { get; set; }

			public ConsoleColor echoSymbol { get; set; }
			public ConsoleColor comment { get; set; }

			public ConsoleColor varSymbol { get; set; }
			public ConsoleColor varName { get; set; }

			public BatConfig()
			{
				debugColor = ConsoleColor.Yellow;

				originalColor = Console.ForegroundColor;
				originalBackground = Console.BackgroundColor;

				// TODO load these settings from a .config file
				defaultColor = ConsoleColor.Gray;
				defaultBackground = ConsoleColor.Black;

				func = ConsoleColor.Cyan;

				echoSymbol = ConsoleColor.DarkYellow;
				comment = ConsoleColor.DarkGreen;

				varSymbol = ConsoleColor.DarkCyan;
				varName = ConsoleColor.DarkCyan;
			}
		}
	}
}
