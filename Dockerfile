FROM microsoft/dotnet:2.2-aspnetcore-runtime

WORKDIR /app
ADD app/build .
ENTRYPOINT ["dotnet", "TelemetrySigner.dll"]