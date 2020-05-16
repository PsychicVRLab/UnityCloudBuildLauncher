# UnityCloudBuildLauncher

## About

It is really time-consuming to launch multiple Cound Build builds with official Cloud Build control panel. Especially if you need to specify git branches.
So we created this tool. An (far) easier way to launch Cloud Build builds. 

<img src="https://user-images.githubusercontent.com/618417/74622956-12236b00-5186-11ea-9f65-cd44054204b3.png" width=200>

## Features

* launch several Cloud Build builds with a few clicks
* launch several builds with a specified git branch
* project selection with a dropdown list
* config selection with a dropdown list

## Install

Download this unitypackage here
https://github.com/styly-dev/UnityCloudBuildLauncher/raw/master/CloudBuildLauncher.unitypackage

Import it to a Unity Project.

## Settings

1. Open Settings Window by Unity Editor's menu **Window -> Cloud Build Launcher -> Settings**
1. Insert API Token
1. Push "Select a project" button to select the target project.
1. Add your favorite target configs to the "Target Config IDs" list.

![image](https://user-images.githubusercontent.com/618417/82112740-d26f1e00-978a-11ea-9d28-17b928797542.png)

## How to use

1. Open the launcher window by Unity Editor's menu **Window -> Cloud Build Launcher -> Launcher**
1. Select some of the configs that you want to build.
1. (optional) Input a git branch name to build
1. Push **Launch!** button
1. Wait until the build finishes!
