using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clypse.core.Enums;

/// <summary>
/// Represents the different types of dictionaries that can be used for password generation.
/// </summary>
public enum DictionaryType
{
    /// <summary>
    /// No specific dictionary type or default type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Adjective dictionary type.
    /// </summary>
    Adjective = 1,

    /// <summary>
    /// Verb dictionary type.
    /// </summary>
    Verb = 2,

    /// <summary>
    /// Noun dictionary type.
    /// </summary>
    Noun = 3,
}
