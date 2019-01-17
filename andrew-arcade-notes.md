# Andrew's note for porting illinker to use Arcade for build

## Following this link:
https://github.com/dotnet/arcade/blob/master/Documentation/Onboarding.md

## Step 1: global.json
It appears the global.json might be related to https://docs.microsoft.com/en-us/dotnet/core/tools/global-json

I just copied the file from the arcade-minimal-ci example

TODO: What does this file does?
TODO: I have no idea what those version number means

## Step 2: Add (or copy) Directory.Build.props and Directory.build.targets.

I just copied the file from the arcade-minimal-ci example

TODO: What does these file do?

## Step 3: Copy eng\common from Arcade into repo.

This is trivial, done

## Step 4: Add (or copy) the Versions.props and Version.Details.xml files to your eng\ folder. Adjust the version prefix and prerelease label as necessary.

I copied and inspected the files. 

If I understand it correctly, there is a version number in the versions.prop, it starts with 1.0.0, which is probably fine. As of now, I don't know what does this version number goes, maybe the assembly version or the nuget version or both?

The SHA in Version.Details.xml is a commit hash on the arcade github repo. I *guess* this is used for synchronizing the arcade build scripts. Therefore I changed it to the SHA corresponding to the eng/common folder I copied.

TODO: Make sure we know where does the verison number goes
TODO: Confirm my understanding for the SHA usage is fine

