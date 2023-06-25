FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /update-ip

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:7.0
WORKDIR /update-ip
ENV TZ=Asia/Ho_Chi_Minh
ENV domain=value
ENV subDomains=value
ENV loginToken=value 
ENV intervalTime=value
COPY --from=build-env /update-ip/out .
ENTRYPOINT ["dotnet", "update-ip.dll"]