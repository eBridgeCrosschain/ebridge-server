FROM mcr.microsoft.com/dotnet/sdk:6.0.420-alpine3.19-amd64
ARG servicename
WORKDIR /app
COPY out/$servicename .