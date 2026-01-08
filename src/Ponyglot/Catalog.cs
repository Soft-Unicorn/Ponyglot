using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Ponyglot;

/// <summary>
/// A loaded translation catalog that can provide translations for a specific culture.
/// </summary>
[SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Implementation pending")]
public class Catalog
{
    /// <summary>
    /// The <see cref="Uri"/> that identifies the catalog resource.
    /// </summary>
    public virtual Uri Uri => throw new NotImplementedException();

    /// <summary>
    /// The domain or catalog name the translations belong to.
    /// </summary>
    public virtual string Domain => throw new NotImplementedException();

    /// <summary>
    /// The culture of the translations.
    /// </summary>
    public virtual CultureInfo Culture => throw new NotImplementedException();

    /// <summary>
    /// Tries to find a translation.
    /// </summary>
    /// <param name="context">The context to use to discriminate similar translations.</param>
    /// <param name="count">For plural scenarios, the count used for plural selection; <c>null</c> for non-plural scenarios.</param>
    /// <param name="messageId">The identifier of the message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the found <see cref="TranslationForm"/>; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    public virtual bool TryGet(string context, long? count, string messageId, [NotNullWhen(true)] out TranslationForm? translation) => throw new NotImplementedException();
}