# ContractBot API
## _And LLM Powered Contract Assistant_

This back end service works in concert with an AngularJS front-end found here: https://github.com/tclowers/contractbot-frontend

## Features
- Parses text from a PDF contract file
- Detects non-contract files
- Parses essential data points from a contract
- Allows for querying of contract content via LLM
- Performs contract edits using natural language via LLM

## Environment Variables
The following variables must be properly set in your environment to run properly
```sh
export CONTRACTBOT_DB_CONNECTION_STRING="Server=contractbot-db.post...;"
export AZURE_BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=htt..."
export OpenAIApiKey="sk-None-BswK...8Rl1"
```

## Run Locally

ContractBot requires [Microsoft .Net](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) v8.0 to run.

Run from the command line
```sh
cd ContractBotApi
dotnet build
dotnet run
```

Copyright 2024 Tom Clowers