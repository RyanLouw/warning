Run the isolated cache tests with a .NET 10 SDK:

    dotnet test warningchat/IssueWizardCache.Tests/IssueWizardCache.Tests.csproj

The isolated project links the production cache and filter without depending on the application's private packages or database.

Manual performance check:

1. Sign in, open a new issue, select an employee and categories, and save.
2. In browser Network tools, confirm one POST returns JSON and one GET opens the saved wizard at step 2.
3. Reopen the same issue within ten minutes and compare server response times against the first load.
4. Save an answer, change categories, add a note, upload/delete evidence, and change status. Reopen the issue and verify persisted changes appear immediately.
5. Repeat in another user's session to verify the cache does not grant access to another user's wizard and writes invalidate stale copies.
6. Wait ten minutes and reopen: data should reload from the backing services.

Cache keys contain the current user ID, warning ID (for issue snapshots), and an invalidation revision. Entries have absolute ten-minute expiry. Reference data has separate per-user entries. MVC writes invalidate all issue snapshots after the action finishes; non-Transgression writes also invalidate reference data. DeleteAttachment is covered despite being an existing GET mutation. Failed actions invalidate too, because some multi-step actions can persist partial work. The next read rebuilds the cache from persisted data. Snapshots are serialized to keep mutable models and EF entities out of shared memory.

This is an in-process cache. Other app instances, direct database changes, and external storage changes are observed at expiry; use distributed invalidation before enabling it across multiple instances. It does not cache task lists, legal wizard pages or database writes. Compare actual request timings before deciding whether to keep the experiment.
