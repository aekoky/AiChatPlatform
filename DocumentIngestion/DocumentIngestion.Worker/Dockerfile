FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER root

# Install Kreuzberg native dependencies (Rust core via P/Invoke)
RUN apt-get update && apt-get install -y \
    # Core system libraries
    libssl3 \
    ca-certificates \
    libgcc-s1 \
    libstdc++6 \
    # pdfium dependencies (PDF rendering)
    libfontconfig1 \
    libfreetype6 \
    # Tesseract OCR (optional — required only if EnableOcr=true)
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-fra \
    tesseract-ocr-ara \
    libtesseract-dev \
    # Office document dependencies
    libgdiplus \
    libc6-dev \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DocumentIngestion/DocumentIngestion.Worker/DocumentIngestion.Worker.csproj", "DocumentIngestion/DocumentIngestion.Worker/"]
RUN dotnet restore "./DocumentIngestion/DocumentIngestion.Worker/DocumentIngestion.Worker.csproj"
COPY . .
WORKDIR "/src/DocumentIngestion/DocumentIngestion.Worker"
RUN dotnet build "./DocumentIngestion.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DocumentIngestion.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentIngestion.Worker.dll"]
