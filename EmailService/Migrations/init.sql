-- Create EmailService Database Tables

-- Email Logs Table
CREATE TABLE IF NOT EXISTS email_logs (
    "Id" SERIAL PRIMARY KEY,
    "To" VARCHAR(255) NOT NULL,
    "Cc" VARCHAR(255),
    "Bcc" VARCHAR(255),
    "Subject" VARCHAR(500) NOT NULL,
    "Body" TEXT NOT NULL,
    "IsHtml" BOOLEAN NOT NULL DEFAULT FALSE,
    "Success" BOOLEAN NOT NULL DEFAULT FALSE,
    "MessageId" VARCHAR(255),
    "ErrorMessage" TEXT,
    "SentAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ApiKeyUsed" VARCHAR(255)
);

CREATE INDEX IF NOT EXISTS idx_email_logs_sent_at ON email_logs("SentAt");
CREATE INDEX IF NOT EXISTS idx_email_logs_to ON email_logs("To");

-- Email Templates Table
CREATE TABLE IF NOT EXISTS email_templates (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Subject" VARCHAR(500) NOT NULL,
    "Body" TEXT NOT NULL,
    "IsHtml" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_email_templates_name ON email_templates("Name");

-- API Keys Table
CREATE TABLE IF NOT EXISTS api_keys (
    "Id" SERIAL PRIMARY KEY,
    "Key" VARCHAR(255) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP,
    "LastUsedAt" TIMESTAMP,
    "UsageCount" INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_api_keys_key ON api_keys("Key");

-- Insert default API key
INSERT INTO api_keys ("Key", "Name", "Description", "IsActive") 
VALUES ('dev-email-service-key-2024', 'Development Key', 'Default development API key', TRUE)
ON CONFLICT DO NOTHING;
