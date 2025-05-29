# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore dependencies
COPY ["jool-backend.csproj", "./"]
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "jool-backend.csproj" -c Release -o /app/build
RUN dotnet publish "jool-backend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy the entire project for migrations
COPY --from=build /src .

# Copy the published app
COPY --from=build /app/publish .

# Crear directorio para certificados y configurar HTTPS
RUN apt-get update && apt-get install -y openssl && \
    mkdir -p /root/.aspnet/https && \
    mkdir -p /app/certs && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /app/certs/server.key -out /app/certs/server.crt \
    -subj "/CN=159.65.178.199" && \
    openssl pkcs12 -export -out /app/certs/server.pfx \
    -inkey /app/certs/server.key -in /app/certs/server.crt \
    -passout pass:YourSecurePassword && \
    cp /app/certs/server.pfx /root/.aspnet/https/ && \
    chmod -R 777 /app/certs && \
    chmod -R 777 /root/.aspnet/https

# Create and set up entrypoint script
RUN echo '#!/bin/bash\n\
\n\
# Print SSL certificate info\n\
echo "Verificando certificados SSL..."\n\
ls -la /app/certs\n\
ls -la /root/.aspnet/https\n\
\n\
# Wait for database to be ready\n\
echo "Waiting for database to be ready..."\n\
sleep 10\n\
\n\
# Run migrations\n\
echo "Running database migrations..."\n\
cd /app\n\
dotnet ef database update --project jool-backend.csproj --verbose\n\
\n\
# Start the application\n\
echo "Starting application..."\n\
dotnet jool-backend.dll' > /app/entrypoint.sh && \
chmod +x /app/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_URLS="http://+:80;https://+:443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/server.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=YourSecurePassword
ENV ASPNETCORE_HTTPS_PORT=443
ENV ASPNETCORE_ENVIRONMENT=Production
# Usar ARG para recibir las variables en tiempo de construcción o dejarlas vacías para pasarlas en tiempo de ejecución
ARG MS_CLIENT_ID_ARG
ARG MS_CLIENT_SECRET_ARG
ENV MS_CLIENT_ID=${MS_CLIENT_ID_ARG}
ENV MS_CLIENT_SECRET=${MS_CLIENT_SECRET_ARG}

# Expose ports
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["./entrypoint.sh"] 