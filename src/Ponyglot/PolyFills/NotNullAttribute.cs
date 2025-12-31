// ReSharper disable CheckNamespace - Polyfill

#pragma warning disable

namespace System.Diagnostics.CodeAnalysis;

#pragma warning disable
/// <summary>
/// Specifies that an output will not be null even if the corresponding type allows it. Specifies that an input argument was not null when the call returns.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
[ExcludeFromCodeCoverage]
internal sealed class NotNullAttribute : Attribute;