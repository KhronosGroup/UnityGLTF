# Server Builds

The server build for UnityGLTF takes place on 2 independent systems:
- [AppVeyor](https://www.appveyor.com/)
  - Configured with `appveyor.yml`.
  - Builds the `GLTFSerialization` solution and runs its unit tests.
  - Runs on all Pull Requests (and commits).
- [Travis CI](https://travis-ci.org/)
  - Configured with `.travis.yml`.
  - Builds the portion of the `GLTFSerialization` solution needed in Unity.
  - Builds the `UnityGLTF` Unity project and runs its tests.
  - Runs only on in-repository Pull Requests (and commits) for [security reasons](https://docs.travis-ci.com/user/pull-requests#pull-requests-and-security-restrictions).

## Updating the Unity license or version
If the Unity license evers expires or the version of Unity is being changed, a new Unity License File will need to be generated.
- On a local machine (probably a Mac), enlist in this repository and ensure [docker](https://www.docker.com/) is installed
- Locally update the [LaunchUnityDocker.sh](./LaunchUnityDocker.sh) script to use the actual username and password of the Unity account whose credentials will be used on the build machines, as well as updating the desired UNITY_VERSION.
  - Do **NOT** check in the username or password changes.
  - Feeding your username and password through a script instead of directly in bash prevents these from being captured in your bash history.
- In the docker container's bash (once it is launched), have Unity request a license by running:
  - xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' /opt/Unity/Editor/Unity -logFile /dev/stdout -batchmode -username "$UNITY_USERNAME" -password "$UNITY_PASSWORD"
- Wait for a line to print that looks like `LICENSE SYSTEM [2017723 8:6:38] Posting <?xml...` and copy the XML portion to a new file called `unity3d.alf`.
  - Do **NOT** check in the `unity3d.alf` file.
- Navigate to [https://license.unity3d.com/manual](https://license.unity3d.com/manual), answering the questions and uploading the `unity3d.alf` file when requested to download a `Unity_v2018.x.ulf` file (name may change in future versions).
  - Do **NOT** check in the `Unity_v2018.x.ulf` file.
- In a regular bash terminal, ensure travis is installed by running `sudo gem install travis`
- Change directory to the root of the repository (so the corresponding repository can be determined) and encrypt the file by running `travis encrypt-file Unity_v2018.x.ulf` (using the actual path to wherever you locally saved the `Unity_v2018.x.ulf` file.
  - If prompted, run `travis login --org` and re-run the original command
  - Take note of the `-openssl...` script line you are prompted to add to the `.travis.yml`
- Replace the existing `Unity_v2018.x.ulf.enc` with the generated file.
- If the encryption keys changed, update the `-openssl...` line in `.travis.yml` to reflect the new values.

If unforseen problems are encountered, some useful resources include:
- [Getting the .alf in the docker image](https://github.com/GabLeRoux/unity3d-ci-example#b-locally)
- [Encrypting the .ulf file](https://docs.travis-ci.com/user/encrypting-files/)

## Additional Resources

- [GabLeRoux Unity3D docker images](https://hub.docker.com/r/gableroux/unity3d/)
- [Tavis CI docs](https://docs.travis-ci.com/)

