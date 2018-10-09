# Unity glTF vNext Proposal

## The Problem with v1

UnityGLTF has been great at providing implementation of import and export of glTF objects, and is close to being at the point of supporting the standard. There are some factors holding it back from being a great library though: 

- Hard to read codebase
- Lack of modularity in components, which affects ease of ability to multithread
- Lack of proper extension support
- Need for better error reporting/handling
- Better test coverage
- Full standards compliance

Due to these issues I am proposing a breaking API change, but the goal is for it to be a stable repository after that.

My proposal for the new system would also remove support for any Unity version that does not properly support async/await (which means supporting the new language features + Unity having a sync context to ensure return to main thread).

## Code Changes
See [UnityGLTFv2.cs](UnityGLTFv2.cs) for proposed interface

## How

A branch will be created, `_v_next_`, which will be where we work on the refactor. 