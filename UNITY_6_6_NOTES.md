# Unity 6.6 viability notes

This branch is not presented as a finished Unity 6.6 support release. It is a record of the changes we used in a consumer project to get UnityGLTF working again on Unity 6.6 alpha.

## What this fixes

- Replaces exporter-side `GetInstanceID()` usage that turns into hard failures on Unity 6.6 with `GetEntityId()`-based keys and labels.
- Updates editor asset creation code to the newer `AssetCreationEndAction` and `EntityId` APIs.
- Updates editor state and preview tracking code to use entity ids where Unity 6.6 no longer accepts instance ids.
- Forces the rough refraction renderer feature onto the RenderGraph-compatible path on newer Unity 6 releases where the older compatibility path is no longer viable.
- Replaces one Visual Scripting exporter `GUID.Generate()` call with `Guid.NewGuid()` to avoid an editor-side API mismatch we hit during the migration.

## Where this was exercised

These changes were taken from a Unity 6.6 alpha migration branch in a real game project that embeds UnityGLTF as a package dependency.

In that consumer project, this patch set was enough to:

- restore clean compilation under Unity 6.6 alpha,
- bring back editor import of existing `.glb` assets that had stopped working after the engine upgrade,
- keep the project's current PlayMode suite green, and
- produce a successful player build again once the surrounding project-side issues were handled.

## Scope and limits

- This branch is intentionally narrow. It captures the concrete changes we needed for Unity 6.6 viability, not a broad compatibility audit across all UnityGLTF features.
- The validation evidence comes from the consumer project integration described above. We did not run a separate full UnityGLTF package test pass in this forked workspace.
- We left out unrelated package metadata and Unity-authored serialization noise from the consumer branch so the patch stays focused on the actual compatibility edits.

## Suggested PR framing

If this goes upstream, the safest framing is: here are the changes we used to get UnityGLTF working for Unity 6.6 in a real project, with the expectation that Khronos can review, tighten, or extend them as needed.
