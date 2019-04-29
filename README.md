# Telemetry Signer - Codename Apple Bloom

Receives telemetry from the local telegraf process signs it with the host key and sends it to the Ingres endpoint after verifying the endpoint.

## Dependecies

- Current dotnet SDK (`v2.2`)
- `Newtonsoft.Json` -> JSON Serializer
- `SSH.NET` -> SFTP / Second channel communication
- `System.Net.WebSockets.Client` -> Pub/Sub to parity for realtime block updates

## Build

- Have dotnet SDK installed
- `dotnet build`

## Tests

- Have the build pass
- switch into the tests directory `cd tests`
- run the tests `dotnet test`

Remark: During the test the console will print out several exceptions/errors. This is due negative testing and expected.
