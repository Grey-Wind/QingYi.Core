// Copyright (c) 2025 Tsing Yi Studio - Grey-Wind
using System;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Enumeration of supported string encoding formats for base encoding/decoding operations.
    /// </summary>
    /// <remarks>
    /// This enum is used to specify text encoding when converting between strings and byte arrays
    /// in various base encoding schemes (Base64, Base85, Base91, etc.).
    /// </remarks>
    [Flags]
    public enum StringEncoding
    {
        /// <summary>
        /// UTF-8 (8-bit Unicode Transformation Format)
        /// </summary>
        /// <remarks>
        /// Default encoding for most modern applications. Supports all Unicode characters
        /// and is backward compatible with ASCII.
        /// </remarks>
        UTF8,

        /// <summary>
        /// UTF-16 Little Endian (16-bit Unicode Transformation Format)
        /// </summary>
        /// <remarks>
        /// Uses 2 bytes per character (or 4 bytes for surrogate pairs). Little Endian
        /// means the least significant byte comes first in memory.
        /// </remarks>
        UTF16LE,

        /// <summary>
        /// UTF-16 Big Endian (16-bit Unicode Transformation Format)
        /// </summary>
        /// <remarks>
        /// Uses 2 bytes per character (or 4 bytes for surrogate pairs). Big Endian
        /// means the most significant byte comes first in memory.
        /// </remarks>
        UTF16BE,

        /// <summary>
        /// ASCII (American Standard Code for Information Interchange)
        /// </summary>
        /// <remarks>
        /// 7-bit character encoding limited to 128 characters. Only supports basic
        /// English characters and control codes.
        /// </remarks>
        ASCII,

        /// <summary>
        /// UTF-32 (32-bit Unicode Transformation Format)
        /// </summary>
        /// <remarks>
        /// Uses 4 bytes per character. Provides fixed-width encoding for all Unicode
        /// characters but is less space-efficient than UTF-8 or UTF-16.
        /// </remarks>
        UTF32,

#if NET6_0_OR_GREATER
        /// <summary>
        /// Latin1 (ISO-8859-1)
        /// </summary>
        /// <remarks>
        /// 8-bit character encoding that covers Western European languages. Each character
        /// is represented by a single byte. Supported in .NET 6.0 and later.
        /// </remarks>
        Latin1,
#endif

        /// <summary>
        /// UTF-7 (7-bit Unicode Transformation Format)
        /// </summary>
        /// <remarks>
        /// Obsolete encoding that was designed for 7-bit transport mechanisms. Not recommended
        /// for use as it has security vulnerabilities and is inefficient.
        /// </remarks>
        [Obsolete("UTF-7 is insecure and should not be used. It is provided for legacy compatibility only.", error: false)]
        UTF7,
    }
}
