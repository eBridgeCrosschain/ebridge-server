FROM mcr.microsoft.com/dotnet/sdk:6.0.420
ARG servicename
WORKDIR /app
COPY out/$servicename .