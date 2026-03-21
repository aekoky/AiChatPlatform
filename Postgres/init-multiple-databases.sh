set -e

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

if [ -n "$POSTGRES_PGSTORE_DATABASE" ]; then
    echo "Setting up pgvector on database: $POSTGRES_PGSTORE_DATABASE"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_PGSTORE_DATABASE" <<-EOSQL
        CREATE EXTENSION IF NOT EXISTS vector;
        CREATE SCHEMA IF NOT EXISTS rag;
EOSQL
    if [ -f /scripts/rag-schema.sql ]; then
        echo "Executing rag-schema.sql on $POSTGRES_PGSTORE_DATABASE..."
        psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_PGSTORE_DATABASE" -f /scripts/rag-schema.sql
        echo "rag-schema.sql executed successfully."
    else
        echo "rag-schema.sql not found at /scripts/rag-schema.sql — skipping."
    fi
    echo "pgvector ready on $POSTGRES_PGSTORE_DATABASE."
else
    echo "POSTGRES_PGSTORE_DATABASE is not set — skipping pgvector setup."
fi