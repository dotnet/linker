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

