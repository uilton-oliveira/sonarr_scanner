[![GitHub issues](https://img.shields.io/github/issues/DarkSupremo/Sonarr-Scanner.svg?maxAge=60&style=flat-square)](https://github.com/DarkSupremo/Sonarr-Scanner/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/DarkSupremo/Sonarr-Scanner.svg?maxAge=60&style=flat-square)](https://github.com/DarkSupremo/Sonarr-Scanner/pulls)
[![Github Releases](https://img.shields.io/github/downloads/DarkSupremo/Sonarr-Scanner/total.svg?maxAge=60&style=flat-square)](https://github.com/DarkSupremo/Sonarr-Scanner/releases/latest)

# Sonarr Scanner
Keep monitoring the Sonarr and Radarr wanted list and send an search request to then at every X minutes or on Wake Event, forcing an Automatic Search instead of relying on RSS based search, this way it will be more suitable to home based systems.

The software was completly rewritten to .NET Core (cross platform)

To start using it, you just need to configure "settings_sonarr.json" and/or "settings_radarr.json", make sure to put the correect API Key. Examples are provided as "settings_sonarr.json.sample" / "settings_radarr.json.sample".

## Running on Docker
```sh
docker run -it --rm \
-v /path/to/settings_sonarr.json:/app/settings_sonarr.json:ro \
-v /path/to/settings_radarr.json:/app/settings_radarr.json:ro \
uilton/sonarr_scanner:latest
```