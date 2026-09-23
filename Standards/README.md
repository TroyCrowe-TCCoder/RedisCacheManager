# Standards Folder

This folder holds repository-local addendum files to the global standards defined in the GlobalStandards repository (`Docs/Standards/`). This folder is tracked in git — addendum files are committed to this repository's history so they are versioned alongside the code they govern.

For the addendum definition and naming convention, see [Addendum Definition](../../GlobalStandards/Docs/Standards/GlobalFileSpecificationStandards.md#215-repository-local-addendum-definition-required).

## What Does Not Belong Here

- Full copies or restatements of global standards files.
- Rules that do not vary from the global standard.
- Notes, scratch files, or session state — those belong in `Working/`.

## Approved Deviations

- **Public GitHub + NuGet.org publication** (deviates from `GlobalNuGetLibraryStandards.md` §11, "Internal packages publish to Azure Artifacts rather than nuget.org"). Approved by Troy Crowe (standard owner) for this repository: `RedisCacheManager` is intentionally published as a public open-source package on `nuget.org`, with source hosted on a public GitHub repository, rather than to an internal Azure Artifacts feed.
