# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

**Shiny.Extensions** — a set of standalone .NET libraries (most ship as source generators) that are independent of the main Shiny client stack. Repo: `https://github.com/shinyorg/extensions`. Packages are AOT/trim-clean where possible (`IsAotCompatible` is on for shipping libraries; source generators target `netstandard2.0`).

- Target framework: `net10.0` (see `Directory.build.props`, `BaseTargetFramework`).
- Versioning: Nerdbank.GitVersioning via `version.json`. Release branches are named `v{major}` (current branch `v5`, main/PR target `v4`).
- Packages are produced on `Release` builds (`GeneratePackageOnBuild`).

## Layout

- `src/` — shipping libraries (each has a matching `*.SourceGenerators` project where relevant)
- `tests/` — xUnit test projects (`*.Tests`) plus Reflector benchmarks
- `samples/` — runnable samples (`Sample`, `Sample.Maui`, `Sample.Web`, `Sample.CopilotConsole`, `Reflector`)
- `skills/` — Claude Code skills shipped by this repo's plugin (`.claude-plugin/`)
- `readme.md` — the NuGet/GitHub readme (also packed via `PackageReadmeFile`)

## Build & test

```bash
dotnet build Build.slnf          # build the shipping set
dotnet test Extensions.slnx      # run all test projects
```

## Library ↔ docs ↔ skill map

Every shipping library has a documentation area and (almost always) a local skill. When you change a library, the readme, its skill, and its docs must move together — see the workflow rules below.

| `src/` project | Docs area (`~/Desktop/dev/documentation/src/content/docs/`) | Skill (`skills/`) |
| --- | --- | --- |
| `Shiny.Extensions.DependencyInjection` | `di/` | `shiny-di` |
| `Shiny.Extensions.Reflector` | `reflector/` | `shiny-reflector` |
| `Shiny.Extensions.Serialization` | `serialization/` | `shiny-serialization` |
| `Shiny.Extensions.Stores` / `.Stores.Web` | `stores/` | `shiny-stores` |
| `Shiny.Extensions.WebHosting` | `webhost/` | `shiny-web-hosting` |
| `Shiny.Extensions.MauiHosting` | `mauihost/` | `shiny-maui-hosting` |
| `Shiny.Extensions.BlazorHosting` | `blazorhost/` | `shiny-blazor-hosting` |
| `Shiny.Extensions.Reactive` | — | — |

The docs live in an Astro/Starlight site at `~/Desktop/dev/documentation` (source, not the built `shinyorg.github.io`). The sidebar/menu is defined in `~/Desktop/dev/documentation/src/sidebar-topics.mjs`.

## Workflow rules (always apply when changing a library)

When you fix or add behavior in any `src/` library, do **all** of the following as part of the same change — do not stop at the code:

1. **Update `readme.md`** — keep the repo/NuGet readme accurate for the change.
2. **Update the local skill** in `skills/<skill>/SKILL.md` for the matching library (see map above) so the generated guidance matches the new behavior, attributes, APIs, and triggers.
3. **Update the Shiny docs** at `~/Desktop/dev/documentation` — edit the matching docs area's `.mdx` content to reflect the fix/behavior.
4. **New feature → add a menu node** in `~/Desktop/dev/documentation/src/sidebar-topics.mjs` under the correct library entry (a new page link). Existing-behavior fixes don't need a new node.
5. **Add a release note** in the matching `…/release-notes.mdx` — rules below.

## Release notes

Release notes live at `~/Desktop/dev/documentation/src/content/docs/<area>/release-notes.mdx`.

**Format** (newest first):

```mdx
## v5                                  ← grouped by major version
### 5.1 - June 14, 2026                ← Major.Minor (append .Patch only when non-zero, e.g. 1.3.1)
<RN type="feature">Description</RN>
<RN type="enhancement">Description</RN>
<RN type="fix" breaking={true}>Description</RN>
<RN type="fix" githubNumber={1}>Description (links the GitHub issue/PR)</RN>
```

`type` is one of `feature | enhancement | fix | chore`. Add `breaking={true}` for breaking changes. The `<RN>` component is imported at the top of each file: `import RN from '/src/components/ReleaseNote.astro';`.

**Which version to use:** take the raw `version` from `version.json` (currently `5.1.0`), **minus any beta/preview suffix**. Render the heading as `Major.Minor`, adding the patch segment only when it is non-zero.

**Where to put the note:**

- **Beta/preview version** (still in development, not yet released):
  - If that version's heading does **not** exist in the release notes → create a new version entry with the release date set to **`TBD`**.
  - If it **already** exists → append the new `<RN>` line to that existing entry.
- **Non-beta version** (a real release) that does **not** yet exist in the release notes → create a new version entry with the release date set to **today** (`2026-06-14` form → `June 14, 2026`).

Create the `## v{major}` group heading too if this is the first entry for a new major version.
