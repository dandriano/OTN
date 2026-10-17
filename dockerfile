FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY OTN/OTN.csproj OTN/
COPY OTN.Editor/OTN.Editor.csproj OTN.Editor/
COPY OTN.Wasm/OTN.Wasm.csproj OTN.Wasm/
RUN dotnet restore OTN.Wasm

COPY OTN/ OTN/
COPY OTN.Editor/ OTN.Editor/
COPY OTN.Wasm/ OTN.Wasm/
RUN dotnet publish OTN.Wasm -c Release -p:PublishTrimmed=true -o /app

FROM busybox:stable as runtime
WORKDIR /www
COPY --from=build /app/wwwroot .

EXPOSE 80
CMD ["httpd", "-f", "-p", "80", "-h", "/www"]