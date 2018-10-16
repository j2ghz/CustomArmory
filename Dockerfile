FROM microsoft/dotnet:sdk

WORKDIR /app

ARG BUILD_DATE
ARG VCS_REF
LABEL org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.vcs-url="https://github.com/j2ghz/CustomArmory.git" \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.schema-version="1.0.0-rc1"
      
COPY src/CustomArmory/*.fsproj .
RUN dotnet restore
COPY src/CustomArmory/. .
RUN dotnet publish -c Release -o out
WORKDIR /app/out
EXPOSE 80
ENTRYPOINT ["dotnet", "CustomArmory.dll"]
