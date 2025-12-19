<p style="text-align: center">
    <img src="resources/icon.svg" alt="Ponyglot icon" width="64"/>
</p>

# Ponyglot

[![NuGet Package](https://img.shields.io/nuget/v/Ponyglot.svg?label=nuget&logo=NuGet)](https://www.nuget.org/packages/Ponyglot/) [![Build](https://img.shields.io/github/actions/workflow/status/Soft-Unicorn/Ponyglot/continuous-integration.yml?branch=main&label=build&logo=GitHub)](https://github.com/Soft-Unicorn/Ponyglot/actions/workflows/continuous-integration.yml)

**Modern gettext for .NET — plural-aware, PO-friendly, and designed for code-driven localization.**

Ponyglot is a lightweight, production-ready localization library for .NET, built around the gettext ecosystem.
It focuses on **correct plural handling**, **clean developer ergonomics**, and **direct use of PO files**, without legacy constraints.

Developed by **Soft-Unicorn** 🦄.

---

## Why Ponyglot?

Localization is easy to get *almost* right — until plurals and real languages enter the picture.

Ponyglot exists because:

* plural rules are **language-specific**, not `if (n == 1)`
* gettext already solved hard linguistic problems
* modern .NET apps deserve a **code-first**, type-safe API
* PO files are still the best interchange format for translators

Ponyglot embraces gettext **without inheriting its historical baggage**.

---

## Key Features

* ✅ Gettext-compatible concepts (`msgid`, `msgctxt`, plurals)
* ✅ First-class plural handling (including zero forms)
* ✅ Direct runtime support for **`.po` files** (no `.mo` required)
* ✅ Stable message identifiers (safe refactoring)
* ✅ Translator-friendly comments and context
* ✅ Designed for modern .NET (async-safe, culture-aware)

---

## Design Goals

Ponyglot is designed to be:

* **Correct** — plural forms follow real linguistic rules
* **Predictable** — no hidden magic, no runtime heuristics
* **Translator-friendly** — works with existing gettext tools
* **Developer-friendly** — readable, expressive code

---

## Non-Goals

Ponyglot intentionally does **not** try to be:

* a UI framework
* a key/value JSON-based i18n system
* a full gettext reimplementation (CLI tools stay external)
* a magic auto-translation solution

---

## Example Usage (Planned)

Simple message

```csharp
var text = Tr.T("Save");
```

Plural-aware message

```csharp
var text = Tr.T(count, "One file found: {1}", "{0} files found: {1}", count, fileList);
```

---

## File Format

Ponyglot works directly with **PO files** at runtime:

* no `.mo` compilation step required
* translations are loaded into memory
* ideal for debugging and hot reload scenarios

Standard gettext tools such as **Poedit**, **Weblate**, or **Crowdin** are fully compatible.

---

## Status

🚧 **Early development / experimental**

The API is to be considered unstable and subject to change.
Feedback and discussion are welcome.

---

## License

MIT

---

## About

Ponyglot is developed and maintained by **Soft-Unicorn**. Ponyglot is a trademark of **Soft-Unicorn**.
Built with care for correctness, clarity, and long-term maintainability.
