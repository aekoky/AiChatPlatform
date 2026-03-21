CREATE TABLE IF NOT EXISTS rag.documents (
    id UUID PRIMARY KEY,
    user_id UUID,
    session_id UUID,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('session', 'user', 'global')),
    file_name TEXT NOT NULL,
    content_type TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'indexed', 'failed')),
    chunk_count INT,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS rag.document_chunks (
    id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES rag.documents(id) ON DELETE CASCADE,
    user_id UUID,
    session_id UUID,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('session', 'user', 'global')),
    content TEXT NOT NULL,
    embedding vector(768),
    chunk_index INT NOT NULL,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_document_chunks_embedding
    ON rag.document_chunks
    USING hnsw (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_document_chunks_scope
    ON rag.document_chunks (scope, user_id, session_id);

CREATE INDEX IF NOT EXISTS idx_document_chunks_document_id
    ON rag.document_chunks (document_id);

CREATE INDEX IF NOT EXISTS idx_documents_user_id
    ON rag.documents (user_id);

CREATE INDEX IF NOT EXISTS idx_documents_session_id
    ON rag.documents (session_id);

CREATE INDEX IF NOT EXISTS idx_documents_status
    ON rag.documents (status);