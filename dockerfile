FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /source

# RUN apt update && apt install python3 -y
# RUN dotnet workload install wasm-tools

COPY OTN/*.csproj ./OTN/
COPY OTN.Wasm/*.csproj ./OTN.Wasm/
RUN dotnet restore OTN.Wasm

COPY OTN/. ./OTN/
COPY OTN.Wasm/. ./OTN.Wasm/
RUN dotnet publish OTN.Wasm -c Release -p:PublishTrimmed=true -o /app

FROM nginx:alpine AS runtime
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
