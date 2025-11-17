-- Auth Service Database Initialization Script
-- Run this script to create all necessary tables for the Auth Service

-- Create Applications table
CREATE TABLE IF NOT EXISTS "Applications" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" varchar(100) NOT NULL,
    "Code" varchar(50) NOT NULL UNIQUE,
    "ClientId" varchar(100) NOT NULL,
    "ClientSecretHash" varchar(256) NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT NOW(),
    "IsActive" boolean DEFAULT true
);

-- Create Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" uuid NOT NULL REFERENCES "Applications"("Id") ON DELETE CASCADE,
    "Email" varchar(256) NOT NULL,
    "NormalizedEmail" varchar(256) NOT NULL,
    "PasswordHash" varchar(256) NOT NULL,
    "IsEmailVerified" boolean DEFAULT false,
    "PhoneNumber" varchar(20),
    "TwoFactorEnabled" boolean DEFAULT false,
    "CreatedAt" timestamp with time zone DEFAULT NOW(),
    "LastLoginAt" timestamp with time zone,
    "IsActive" boolean DEFAULT true
);

-- Create Roles table
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" uuid NOT NULL REFERENCES "Applications"("Id") ON DELETE CASCADE,
    "Name" varchar(50) NOT NULL,
    "Description" varchar(256),
    UNIQUE("ApplicationId", "Name")
);

-- Create UserRoles junction table
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "RoleId" uuid NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

-- Create RefreshTokens table
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ApplicationId" uuid NOT NULL REFERENCES "Applications"("Id") ON DELETE CASCADE,
    "Token" varchar(256) NOT NULL UNIQUE,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT NOW(),
    "RevokedAt" timestamp with time zone,
    "ReplacedByToken" varchar(256),
    "DeviceInfo" varchar(500),
    "IpAddress" varchar(45)
);

-- Create SessionLogs table
CREATE TABLE IF NOT EXISTS "SessionLogs" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ApplicationId" uuid NOT NULL REFERENCES "Applications"("Id") ON DELETE CASCADE,
    "LoginAt" timestamp with time zone,
    "LogoutAt" timestamp with time zone,
    "IpAddress" varchar(45),
    "UserAgent" varchar(500),
    "IsSuccessful" boolean DEFAULT true
);

-- Create ApiKeys table
CREATE TABLE IF NOT EXISTS "ApiKeys" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "ApplicationId" uuid NOT NULL REFERENCES "Applications"("Id") ON DELETE CASCADE,
    "OwnerUserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Name" varchar(100) NOT NULL,
    "HashedKey" varchar(256) NOT NULL UNIQUE,
    "Scope" varchar(500) NOT NULL DEFAULT '',
    "CreatedAt" timestamp with time zone DEFAULT NOW(),
    "ExpiresAt" timestamp with time zone,
    "RevokedAt" timestamp with time zone,
    "LastUsedAt" timestamp with time zone,
    "IsActive" boolean DEFAULT true
);

-- Create EmailTokens table
CREATE TABLE IF NOT EXISTS "EmailTokens" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Token" varchar(256) NOT NULL UNIQUE,
    "TokenType" varchar(50) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT NOW(),
    "IsUsed" boolean DEFAULT false
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_applications_code ON "Applications"("Code");
CREATE INDEX IF NOT EXISTS idx_users_application_email ON "Users"("ApplicationId", "NormalizedEmail");
CREATE INDEX IF NOT EXISTS idx_users_normalized_email ON "Users"("NormalizedEmail");
CREATE INDEX IF NOT EXISTS idx_roles_application_name ON "Roles"("ApplicationId", "Name");
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON "RefreshTokens"("Token");
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_app ON "RefreshTokens"("UserId", "ApplicationId");
CREATE INDEX IF NOT EXISTS idx_api_keys_hashed_key ON "ApiKeys"("HashedKey");
CREATE INDEX IF NOT EXISTS idx_email_tokens_token ON "EmailTokens"("Token");
CREATE INDEX IF NOT EXISTS idx_email_tokens_user_type_used ON "EmailTokens"("UserId", "TokenType", "IsUsed");

-- Insert default roles for new applications (this will be handled by application service)
-- Example data for testing (remove in production)
-- INSERT INTO "Applications" ("Id", "Name", "Code", "ClientId", "ClientSecretHash", "CreatedAt", "IsActive")
-- VALUES ('550e8400-e29b-41d4-a716-446655440000', 'Test Application', 'testapp', 'app_test123', '$2a$11$example.hash.here', NOW(), true);

-- Insert default roles for the test application
-- INSERT INTO "Roles" ("Id", "ApplicationId", "Name", "Description")
-- VALUES
-- ('550e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440000', 'admin', 'Administrator with full access'),
-- ('550e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440000', 'user', 'Standard user role'),
-- ('550e8400-e29b-41d4-a716-446655440003', '550e8400-e29b-41d4-a716-446655440000', 'moderator', 'Content moderator role');

-- Create a function to hash API keys (optional, for future use)
CREATE OR REPLACE FUNCTION hash_api_key(api_key text)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    hashed_key text;
BEGIN
    SELECT encode(digest(api_key, 'sha256'), 'base64') INTO hashed_key;
    RETURN hashed_key;
END;
$$;
