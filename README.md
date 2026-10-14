# Pandora's Box

Pandora's Box is a Dalamud plugin for Final Fantasy XIV that bundles a large collection of quality-of-life features, shortcuts, UI tweaks, and automation helpers into a single plugin.

It is designed to let you enable only the features you want, while keeping the rest disabled.

## Features

Pandora's Box includes features across several categories:

- **Commands** — custom chat commands such as duty leaving, plate linking, and testing utilities
- **Actions** — combat- and item-related helpers such as auto-mounting after combat and deciphering maps from the hotbar
- **UI** — interface improvements such as quick panel behavior, vendor defaults, and convenience menu actions
- **Targets** — object interaction helpers such as automatically opening nearby treasure chests
- **Chat** — chat-based tools such as teleport helpers for map links
- **Other** — miscellaneous gameplay and macro helpers

## Examples of included tools

A few examples of features currently in the repo:

- Automatically mount after combat
- Leave a duty with a command
- Skip `/mount` lines in macros when already mounted
- Automatically open nearby treasure chests
- Link glamour plates more easily
- Improve several vendor and menu workflows
- Add convenience helpers for gathering, relic quests, and other repetitive tasks

## Usage

After installing and enabling the plugin in Dalamud:

- Open the configuration window from the Dalamud plugin interface, or use the `/pandora` command
- Enable the features you want from the main window
- Configure any feature-specific options where available

Some features also register their own chat commands, such as:

- `/pdfleave`
- `/pgl`
- `/pan-test`

## Requirements

- Final Fantasy XIV
- Dalamud
- The supported libraries and dependencies used by the plugin at build/runtime

Some features may require other plugins to be installed. For example, the map teleport helper requires the **Teleporter** plugin.

## Notes

- This repository is licensed under the BSD 3-Clause "New" or "Revised" License.
- Some features are intentionally disabled or marked unstable in code.
- Feature availability and behavior may change over time.

## Support

For more information, visit the project homepage:

https://puni.sh
