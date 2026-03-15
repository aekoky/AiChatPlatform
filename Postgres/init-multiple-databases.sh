#!/bin/bash
set -e

# This script is executed by the Postgres container on first startup.
# It reads the POSTGRES_MULTIPLE_DATABASES environment variable (comma-separated)
# and creates each database if it does not already exist.
#
# Usage in docker-compose.yml:
#   environment:
#     POSTGRES_MULTIPLE_DATABASES: aichat,keycloak
#   volumes:
#     - ./Postgres/init-multiple-databases.sh:/docker-entrypoint-initdb.d/init-multiple-databases.sh:ro

create_database() {
    local database=$1
    echo "Creating database: $database"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
        SELECT 'CREATE DATABASE "$database"'
        WHERE NOT EXISTS (
            SELECT FROM pg_database WHERE datname = '$database'
        )\gexec
EOSQL
    echo "Database '$database' ready."
}

if [ -n "$POSTGRES_MULTIPLE_DATABASES" ]; then
    echo "Creating multiple databases: $POSTGRES_MULTIPLE_DATABASES"
    for db in $(echo "$POSTGRES_MULTIPLE_DATABASES" | tr ',' ' '); do
        create_database "$db"
    done
    echo "All databases created."
else
    echo "POSTGRES_MULTIPLE_DATABASES is not set — skipping multiple database creation."
fi
