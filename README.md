# Sonarr Scanner
Keep monitoring the Sonarr and Radarr wanted list and send an search request to then at every X minutes or on Wake Event, forcing an Automatic Search instead of relying on RSS based search, this way it will be more suitable to home based systems.

The software was completly rewritten to be Cross Platform, it should work fine on Mono and on .NET and has no UI anymore.
When running on .NET, it will have no console window and will have an Tray Icon when running, you should manually put it on system startup.

To start using it, you just need to configure "settings_sonarr.json" and/or "settings_radarr.json", make sure to put the correect API Key.
