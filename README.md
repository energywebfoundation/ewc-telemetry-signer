# Telemetry Signer - Codename Apple Bloom

Receives telemetry from the local telegraf process signs it with the host key and sends it to the Ingres endpoint after verifying the endpoint.

## Requirements

- Using .Net Core 2.2 as Microsofts Cryptolibs are very mature and C# is more robust that JS/TS
- Listen as reader on a named linux FIFO pipe
- Verify input as InfluxDB line protocol messages (see https://docs.influxdata.com/influxdb/v1.7/write_protocols/line_protocol_tutorial/)
- Use .Net RSA crypto to sign the data. (see https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsacryptoserviceprovider?view=netcore-2.2)
- Connect to Telemetry ingress and verify TLS fingerprint during handshake