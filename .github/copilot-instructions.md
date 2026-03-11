# Copilot Instructions

## Architecture Reference
- **Architecture overview:** `docs/ARCHITECTURE.md` — project structure, design patterns, dependency flow, data flow, external dependencies. Read this first when starting a new session.

## Project Guidelines
- When asked to "review" code, provide findings as a report only — do not make changes unless explicitly asked.
- **Never commit without explicit user request. Do not stage or commit changes autonomously.**

## Git & Terminal Rules
- Always use `--no-pager` for git commands that produce output (`diff`, `log`, `show`, etc.)
- For multi-line commit messages, write to a temp file and use `git --no-pager commit -F <file>`. Never use inline here-strings (`@"..."@`) — they hang the terminal.
- Prefer `get_file` / `code_search` tools over terminal commands for reading code.

## C# Coding Style
- Target C# 12 / .NET 8 features.
- Prefer file-scoped namespaces and single-line using directives.
- Use pattern matching, switch expressions, and expression-bodied members where appropriate.
- Use `nameof(...)` instead of string literals for member names.

### Naming
- PascalCase for public types/members; camelCase for private fields, locals, and private methods.
- Prefix interfaces with `I`.

### Nullable Reference Types
- Respect nullability annotations. Use `is null` / `is not null` instead of `== null` / `!= null`.

## WPF & MVVM
- Follow MVVM: logic in ViewModels, UI in Views/XAML. No business logic in code-behind.
- Use `ObservableCollection<T>` and `INotifyPropertyChanged` for data binding.
- Prefer async/await for hardware and I/O calls — never block the UI thread.

# Release Documentation

- **Release scratchpad:** `docs/release/NEXT_RELEASE.md` — items for the next unreleased version
- **Changelog:** `docs/release/CHANGELOG.md` — user-facing changes per released version
- **Release process:** `docs/release/RELEASE_PROCESS.md` — tagging and publishing steps
- 
Add notes to `NEXT_RELEASE.md` as features/fixes are developed. After tagging a release, copy items to `CHANGELOG.md` and reset `NEXT_RELEASE.md`.
