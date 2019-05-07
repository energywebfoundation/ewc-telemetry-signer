FROM microsoft/dotnet:2.2-sdk AS builder
RUN mkdir -p /src
WORKDIR /src
ADD . .
RUN dotnet restore
RUN dotnet build
RUN dotnet test --no-build -v=normal tests --logger "trx;LogFileName=TestResults.trx" /p:CollectCoverage=true /p:Exclude="[xunit.*]*"
RUN dotnet publish -c Release -o build


FROM microsoft/dotnet:2.2-aspnetcore-runtime

WORKDIR /app
COPY --from=builder /src/app/build .
ENTRYPOINT ["dotnet", "TelemetrySigner.dll"]