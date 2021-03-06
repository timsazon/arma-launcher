# ArmA Launcher

ArmA 3 Launcher with addon validation (and downloading!) feature based on [Yoma's AddonSync2009](http://forums.bistudio.com/showthread.php?t=89792) autoconfig

![Launcher Window](https://i.imgur.com/CbdQBhy.png  "Launcher Window")

## Features

- Addon validation (MD5 comparison)
- Addon downloading (FTP)
- Settings menu (A3 path, A3 mods path and some startup parameters)
- TeamSpeak 3 and TFAR automatic installation

## Building

1. If you want to localize it, just copy Resources.resx to Resources.de.resx (in case of German) and translate the values.

2. Open App.config in the root folder and set missing params.

3. Build Release

4. Publish it (e.g. NET Core's single-file or MSIX installer)

## License and Usage

See [LICENSE](https://github.com/timsazon/arma-launcher/blob/master/LICENSE.md) for details on copyright and usage of the software.
