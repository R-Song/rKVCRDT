# How to run the program

1. Copy `x` copies of `cluster_config_example.json` (x = how many replication server you want)
2. Delete unused server, only keep 1 `.json` with `"isSelf": true` as main server
3. To start the server, run `dotnet run path/to/cluster_config.json`
