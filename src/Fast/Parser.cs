using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Fast.AST;

namespace Microsoft.Fast
{
    /// <summary>
    /// Provides methods to parse fast programs.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Parses a fast program from multiple files.
        /// Throws FastParseException when parsing fails.
        /// </summary>
        /// <param name="files">given source files</param>
        /// <param name="infersorts">if false sort inference is omitted</param>
        public static FastPgm ParseFromFiles(string[] files, bool infersorts = true)
        {
            if (files == null)
                throw new ArgumentNullException("files");
            if (files.Length == 0)
                throw new ArgumentException("must be nonempty", "files");

            var pgm = FastPgmParser.ParseFiles(files);
            if (infersorts)
                pgm.Typecheck();
            return pgm;
        }

        /// <summary>
        /// Parses a fast program from a given file.
        /// Throws FastParseException when parsing fails.
        /// </summary>
        /// <param name="file">given source file</param>
        /// <param name="infersorts">if false sort inference is omitted</param>
        public static FastPgm ParseFromFile(string file, bool infersorts = true)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (file.Length == 0)
                throw new ArgumentException("must be nonempty string", "file");

            var pgm = FastPgmParser.ParseFiles(file);
            if (infersorts)
                pgm.Typecheck();
            return pgm;
        }

        /// <summary>
        /// Parses a fast program from text.
        /// Throws FastParseException when parsing fails.
        /// </summary>
        /// <param name="source">given source string</param>
        /// <param name="infersorts">if false sort inference is omitted</param>
        public static FastPgm ParseFromString(string source, bool infersorts = true)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (source.Length == 0)
                throw new ArgumentException("must be nonempty string", "source");

            var pgm = FastPgmParser.Parse(source);
            if (infersorts)
                pgm.Typecheck();
            return pgm;
        }

        /// <summary>
        /// Parses a fast program from a stream.
        /// Throws FastParseException when parsing fails.
        /// </summary>
        /// <param name="source">given source stream</param>
        /// <param name="infersorts">if false sort inference is omitted</param>
        public static FastPgm ParseFromStream(Stream source, bool infersorts = true)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var pgm = FastPgmParser.Parse(source);
            if (infersorts)
                pgm.Typecheck();
            return pgm;
        }
    }
}
