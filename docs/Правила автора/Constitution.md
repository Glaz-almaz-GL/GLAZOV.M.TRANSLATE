# GLTranslate Constitution
Version: 1.0
Status: Active

---

# 1. General Principles

## Rule 1. Simplicity over abstraction

Architecture must remain as simple as possible.

Abstractions are introduced only when there are at least two real implementations or when they demonstrably reduce complexity.

No "future-proof" architecture.

---

## Rule 2. XML Documentation

Every public, protected and internal API must contain complete XML documentation.

Documentation must explain:

- purpose
- behavior
- invariants
- thread-safety
- exceptions
- generic parameters
- remarks when required

Use `<inheritdoc/>` whenever documentation is inherited without modification.

Avoid duplicated documentation.

---

## Rule 3. Immutability

All domain objects are immutable.

Collections are immutable.

Objects never expose mutable state.

---

## Rule 4. Thread Safety

Every public object must be thread-safe.

If an object is immutable, this fact should be documented.

---

## Rule 5. Correctness over Convenience

Never sacrifice correctness for shorter code.

Prefer explicit validation.

Prefer deterministic behavior.

---

# 2. Naming

Names describe business concepts.

Avoid abbreviations unless they are international standards.

Examples:

Language

Culture

Region

Script

Iso6391Code

Iso15924Code

Bcp47Code

Avoid generic names like:

Manager

Helper

Util

CommonHelper

DataObject

---

# 3. Value Objects

Every identifier and every code is a Value Object.

Examples:

LanguageId

CultureId

RegionId

ScriptId

Iso6391Code

Iso3166Alpha2Code

Iso15924Code

---

Value objects:

- immutable
- compared by value
- include runtime type in equality
- never expose setters

---

String-based value objects inherit from:

StringValueObject

Other value objects inherit from:

ValueObject<T>

---

# 4. Entities

Entities are compared only by Id.

Entity equality never depends on mutable data.

Entities implement:

IIdentifiable<TId>

Entities are sealed unless inheritance is explicitly required.

---

# 5. Collections

Collections never expose mutable containers.

Preferred immutable collections:

ImmutableArray<T>

ImmutableDictionary<TKey,TValue>

ImmutableHashSet<T>

---

Business collections use dedicated types.

Examples:

CodeSet<TCode>

EntitySet<TEntity>

instead of:

List<T>

Dictionary<TKey,TValue>

HashSet<T>

inside public API.

---

# 6. Code Systems

Every coding system has its own type.

Never represent codes using plain string.

Correct:

Iso6391Code

Iso6392Code

Iso3166Alpha2Code

Iso15924Code

Incorrect:

string LanguageCode

string RegionCode

---

Base classes:

LanguageCode

RegionCode

CultureCode

ScriptCode

All implement:

ICode

---

# 7. Entity Codes

Entities never expose mutable collections of codes.

Use:

CodeSet<TCode>

Supported operations:

Get<T>()

Get(Type)

TryGetValue<T>()

TryGetValue(Type)

Contains<T>()

Contains(Type)

Enumeration

Indexer

Count

---

Each code type may appear only once.

Duplicate code types are prohibited.

---

# 8. Entity Collections

Entity collections use:

EntitySet<TEntity>

Guarantees:

- immutable
- no null
- no duplicates
- allocation-free foreach

---

# 9. Constructors

Prefer primary constructors whenever possible.

Declare explicit constructors only when validation or initialization requires it.

Validation is performed immediately.

Objects cannot exist in an invalid state.

---

# 10. Validation

Validate every public and internal input.

Use:

ArgumentNullException.ThrowIfNull()

ArgumentException.ThrowIfNullOrWhiteSpace()

Avoid delayed failures.

---

# 11. Exceptions

Throw the most specific exception.

Examples:

ArgumentNullException

ArgumentException

InvalidOperationException

Never throw Exception directly.

Exception messages must explain the violated invariant.

---

# 12. Equality

Entities:

equal by Id.

Value objects:

equal by runtime type and value.

Reference equality is checked first.

Hash codes must be consistent with Equals().

---

# 13. Registries

Registries are immutable lookup services.

Responsibilities:

Get()

TryGet()

Contains()

GetAll()

Registries never create entities.

Registries never modify entities.

---

# 14. Provider Independence

Domain objects never depend on translation providers.

Provider-specific identifiers remain inside provider implementations.

Language

Culture

Region

Script

must stay provider-independent.

---

# 15. Performance

Avoid unnecessary allocations.

Materialize collections only once.

Prefer ImmutableArray over List for immutable storage.

Prefer allocation-free enumeration.

Avoid LINQ in hot paths when a loop is clearer and faster.

---

# 16. Public API

Public API must be:

predictable

minimal

consistent

strongly typed

self-documenting

Every public member should exist because it solves a real problem.

---

# 17. Architecture Evolution

The project evolves incrementally.

Large rewrites are prohibited.

Changes are introduced in small, verifiable steps.

Backward compatibility is preserved whenever practical.

---

# 18. Review Checklist

Before accepting any change, verify:

✓ XML documentation is complete.

✓ Public API is minimal.

✓ Object is immutable.

✓ Thread safety is preserved.

✓ Validation is complete.

✓ Equality is correct.

✓ No duplicate responsibilities.

✓ No unnecessary abstractions.

✓ Collections are immutable.

✓ Provider independence is preserved.

✓ Code follows existing naming conventions.

✓ Backward compatibility is maintained whenever possible.

---

# 19. Core Philosophy

GLTranslate is a domain-first library.

The domain model is the source of truth.

External standards (ISO, BCP, providers) describe the domain but never define it.

Architecture must remain clean, explicit, immutable, strongly typed, and understandable without hidden behavior.

---

# 20. SOLID Principles

The project follows SOLID principles unless a documented architectural decision explicitly states otherwise.

## Single Responsibility Principle

Every type has one clearly defined responsibility.

If a class description requires the word "and", it probably has multiple responsibilities.

---

## Open / Closed Principle

The system is extended by adding new implementations rather than modifying existing code.

Examples:

new LanguageCode

new RegionCode

new TranslationProvider

instead of modifying existing classes.

---

## Liskov Substitution Principle

Inheritance must never change observable behavior.

Value objects compare runtime types.

Entity equality is never polymorphic.

If LSP cannot be preserved, prefer composition.

---

## Interface Segregation Principle

Interfaces expose only the members actually required by their consumers.

Prefer several focused interfaces over one large interface.

---

## Dependency Inversion Principle

High-level domain abstractions never depend on infrastructure.

Infrastructure depends on abstractions.

The Abstractions project never references implementation projects.

---

# 21. Domain Driven Design

GLTranslate follows Domain-Driven Design where applicable.

## Entities

Represent identity.

Mutable only through controlled construction.

Compared by identifier.

---

## Value Objects

Represent values.

Immutable.

Compared by value.

---

## Registries

Represent read-only domain knowledge.

Contain predefined entities.

Never mutate.

Never create domain logic.

---

## Domain Objects

Must express business concepts rather than implementation details.

Avoid technical names inside the domain model.

---

# 22. Namespace Organization

Namespaces follow the domain.

Example:

GLTranslate.Abstractions

GLTranslate.Abstractions.Common

GLTranslate.Abstractions.Interfaces

GLTranslate.Abstractions.Linguistics

GLTranslate.Abstractions.Linguistics.Languages

GLTranslate.Abstractions.Linguistics.Cultures

GLTranslate.Abstractions.Linguistics.Scripts

GLTranslate.Abstractions.Linguistics.Regions

GLTranslate.Abstractions.Translation

Avoid namespaces like:

Helpers

Utils

Misc

Shared

General

---

# 23. File Organization

One public type per file.

File name equals type name.

Partial classes are allowed only when justified.

Generated code must remain isolated.

---

# 24. Coding Style

Use file-scoped namespaces.

Use primary constructors whenever practical.

Prefer expression-bodied members only when readability improves.

Prefer explicit types when they improve readability.

Use var only when the type is obvious.

Prefer collection expressions.

Example:

[]

[.. values]

instead of verbose collection creation.

---

# 25. Performance Rules

Avoid unnecessary allocations.

Avoid repeated enumeration.

Materialize IEnumerable only once.

Avoid defensive copying of immutable collections.

Prefer ImmutableArray over IReadOnlyCollection for storage.

Prefer allocation-free foreach.

Avoid reflection in hot paths.

Avoid LINQ in performance-critical code when a loop is simpler.

---

# 26. Binary Compatibility

Public API is considered stable.

Breaking changes require strong justification.

Prefer extension over modification.

Never rename public types without migration strategy.

---

# 27. Testing

Every public type should have unit tests.

Equality must be tested.

Validation must be tested.

Boundary cases must be tested.

Failure paths must be tested.

---

# 28. Error Messages

Exception messages describe the violated invariant.

Avoid generic messages.

Good:

Duplicate code types are not allowed.

Bad:

Invalid argument.

---

# 29. Documentation Style

Documentation describes behavior rather than implementation.

Prefer:

Represents...

Gets...

Returns...

Determines whether...

Avoid:

This class stores...

This method simply...

Documentation should explain *why*, not restate the code.

---

# 30. Architectural Decisions

Significant architectural decisions are documented using ADR.

ADR documents never replace this Constitution.

If an ADR conflicts with the Constitution, the Constitution must be updated first.

---

# 31. AI Collaboration

AI-generated code follows this Constitution exactly.

AI must not introduce:

unnecessary abstractions

unused interfaces

speculative architecture

temporary solutions

hidden behavior

Before proposing a new abstraction, AI should verify that at least one of the following is true:

• two or more real implementations exist;
• duplication would otherwise occur;
• the abstraction simplifies the public API.

Otherwise, the simpler design is preferred.

---

# 32. Final Principle

Readable code is more valuable than clever code.

Explicit code is preferred over implicit code.

Correctness is preferred over brevity.

Domain clarity is preferred over technical elegance.

Every line of code should help another developer understand the domain.

---

# 33. Design Philosophy

The architecture of GLTranslate is driven by the domain.

Technology serves the domain, never the other way around.

Every architectural decision should make the domain model clearer, more explicit and easier to understand.

---

## Domain First

The domain model is the primary artifact of the project.

APIs, providers, infrastructure and implementation details exist to support the domain model.

If a technical solution makes the domain less expressive, it should be reconsidered.

---

## Explicitness

The library prefers explicit models over implicit behavior.

Objects should clearly express what they represent.

Business concepts should never be hidden behind generic abstractions.

---

## Strong Typing

Primitive values should not represent domain concepts.

Instead of:

string

Guid

int

prefer dedicated types such as:

LanguageId

CultureId

Iso6391Code

Iso3166Alpha2Code

ScriptId

The compiler should prevent incorrect usage whenever possible.

---

## Composition over Inheritance

Inheritance is used only when an "is-a" relationship is unquestionably true.

When behavior can be composed, composition is preferred.

Avoid inheritance solely for code reuse.

---

## Minimal Public Surface

Every public type becomes part of the library contract.

Public APIs should therefore remain as small as possible.

Prefer internal implementation details until a public abstraction is genuinely required.

---

## Progressive Design

Architecture evolves from real requirements.

Avoid speculative design.

Avoid introducing extension points before they are needed.

A simple implementation today is preferable to an abstract implementation that may never be used.

---

## Consistency

Similar concepts should behave similarly.

If Language exposes a CodeSet, then Region, Script and Culture should expose their codes in the same manner.

Naming, validation, equality and construction patterns should remain consistent throughout the project.

---

## Predictability

Consumers of the library should never be surprised by object behavior.

Methods should do exactly what their names imply.

Constructors should always produce valid objects.

Invalid state should never be observable.

---

## Separation of Concerns

Each layer has a clearly defined responsibility.

Abstractions define contracts.

Domain expresses business concepts.

Infrastructure implements technical behavior.

Providers integrate external services.

Presentation formats data for users.

Responsibilities must never leak between layers.

---

## Standard Independence

International standards are integrated, not adopted as the domain model.

ISO, BCP and provider identifiers describe the domain but never define it.

GLTranslate remains independent of any external organization.

Standards may evolve without requiring changes to the domain model.

---

## Evolution without Disruption

The architecture should support gradual evolution.

Adding support for new standards, providers or code systems should require minimal modification of existing code.

Whenever possible, existing public APIs remain compatible across versions.

---

## Readability over Cleverness

Code is written primarily for people.

A straightforward implementation is preferred over a shorter but less obvious one.

Optimizations should never reduce maintainability unless they solve a demonstrated performance problem.

---

## Self-Documenting Design

Types, members and namespaces should communicate intent.

Documentation complements the design but never compensates for poor naming.

If extensive explanation is required to understand a class, the design should be reconsidered.

---

## Single Source of Truth

Every concept has exactly one authoritative representation.

Examples:

Language represents a language.

Culture represents a culture.

Script represents a writing system.

Region represents a region.

LanguageId represents the identity of a language.

Duplicate representations of the same concept are prohibited.

---

## Architectural Integrity

The consistency of the architecture is more important than the convenience of an individual implementation.

New code should adapt to the architecture.

The architecture should not adapt to isolated implementation details.

---

## Long-Term Maintainability

Every design decision should be evaluated from the perspective of future maintainability.

The preferred solution is the one that will still be understandable years later by a developer who has never seen the project before.

Short-term convenience must never outweigh long-term clarity.