FROM node:24-alpine AS client
WORKDIR /src/client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build -- --outDir /out --emptyOutDir

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY src/Evently.Api/Evently.Api.csproj ./
RUN dotnet restore
COPY src/Evently.Api/ ./
RUN dotnet publish -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=client /out ./wwwroot
# Cookie encryption keys live here so sessions survive restarts (mount a volume on it).
RUN mkdir /keys && chown $APP_UID /keys
USER $APP_UID
# Hosts such as Railway inject PORT; the proxy in front terminates TLS, so trust its forwarded headers.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet Evently.Api.dll --urls http://+:${PORT:-8080}"]
