# Database regression checks

These tests use disposable data and fake credentials. They never connect to Supabase.

- `database-migration.test.mjs`: PostgreSQL integration checks using a temporary
  `@electric-sql/pglite` installation. Run `node Tools/Tests/database-migration.test.mjs <absolute-path-to-pglite/dist/index.js>`.
- `SaveMergeRegression.cs`: compile against the current gameplay assembly and
  Unity's netstandard reference, then run with Unity's Mono runtime. Exercises the
  real `SaveSlots.MergeCloudSnapshots` without opening or changing player saves.
- `SessionRefreshRegression.cs`: compile together with the production
  `Assets/Script/belo/SupabaseSession.cs`, referencing `System.Web.Extensions.dll`.
  Its test doubles exercise token rotation, expiry, failures and cancellation.
- `CloudSyncRegression.cs`: compile together with `SessionRefreshRegression.cs`,
  `SupabaseSession.cs` and `CloudSaveSync.cs`, choosing `CloudSyncRegression` as the
  entry point. Exercises initial retries, queued uploads, in-flight changes and
  conflict backups using a temporary fixture directory.

Keep test executables, temporary dependencies and fixture output under `Temp/DatabaseTests`.
They are not game assets and should not be committed. The production Unity assembly
can be compiled separately using its generated `Library/Bee/artifacts/*/Assembly-CSharp.rsp`,
redirecting the output and reference assembly to the temporary test directory.

- `content-catalog.test.mjs`: 15 disposable PostgreSQL checks for admin edits, guest reads, stable identities, retirement, publication and unchanged ownership. Run with the same PGlite path as the migration test.
