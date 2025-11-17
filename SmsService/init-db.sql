-- Initialize database schema
CREATE TABLE IF NOT EXISTS sms_messages (
    id SERIAL PRIMARY KEY,
    recipient VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    sender_id VARCHAR(50) NOT NULL,
    message_id VARCHAR(100),
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    response_data TEXT,
    ip_address VARCHAR(45),
    user_agent TEXT,
    api_key_used VARCHAR(100),
    app_name VARCHAR(100),
    app_version VARCHAR(50)
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_sms_recipient ON sms_messages(recipient);
CREATE INDEX IF NOT EXISTS idx_sms_created_at ON sms_messages(created_at);
CREATE INDEX IF NOT EXISTS idx_sms_status ON sms_messages(status);
CREATE INDEX IF NOT EXISTS idx_sms_app_name ON sms_messages(app_name);
