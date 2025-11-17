using Npgsql;
using SmsService.Models;

namespace SmsService.Repositories;

public class SmsMessageRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SmsMessageRepository> _logger;

    public SmsMessageRepository(IConfiguration configuration, ILogger<SmsMessageRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Database connection string not found");
        _logger = logger;
    }

    public async Task<int> SaveMessageAsync(SmsMessage message)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO sms_messages (recipient, message, sender_id, message_id, status, response_data, ip_address, user_agent, api_key_used, app_name, app_version)
                VALUES (@recipient, @message, @senderId, @messageId, @status, @responseData, @ipAddress, @userAgent, @apiKeyUsed, @appName, @appVersion)
                RETURNING id";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("recipient", message.Recipient);
            cmd.Parameters.AddWithValue("message", message.Message);
            cmd.Parameters.AddWithValue("senderId", message.SenderId);
            cmd.Parameters.AddWithValue("messageId", message.MessageId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("status", message.Status);
            cmd.Parameters.AddWithValue("responseData", message.ResponseData ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ipAddress", message.IpAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("userAgent", message.UserAgent ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("apiKeyUsed", message.ApiKeyUsed ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("appName", message.AppName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("appVersion", message.AppVersion ?? (object)DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save SMS message to database");
            throw;
        }
    }

    public async Task<List<SmsMessage>> GetRecentMessagesAsync(int limit = 100)
    {
        var messages = new List<SmsMessage>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT id, recipient, message, sender_id, message_id, status, created_at, response_data, ip_address, user_agent, api_key_used, app_name, app_version
                FROM sms_messages
                ORDER BY created_at DESC
                LIMIT @limit";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new SmsMessage
                {
                    Id = reader.GetInt32(0),
                    Recipient = reader.GetString(1),
                    Message = reader.GetString(2),
                    SenderId = reader.GetString(3),
                    MessageId = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Status = reader.GetString(5),
                    CreatedAt = reader.GetDateTime(6),
                    ResponseData = reader.IsDBNull(7) ? null : reader.GetString(7),
                    IpAddress = reader.IsDBNull(8) ? null : reader.GetString(8),
                    UserAgent = reader.IsDBNull(9) ? null : reader.GetString(9),
                    ApiKeyUsed = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AppName = reader.IsDBNull(11) ? null : reader.GetString(11),
                    AppVersion = reader.IsDBNull(12) ? null : reader.GetString(12)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve SMS messages from database");
            throw;
        }

        return messages;
    }

    public async Task<List<SmsMessage>> GetMessagesByRecipientAsync(string recipient)
    {
        var messages = new List<SmsMessage>();

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT id, recipient, message, sender_id, message_id, status, created_at, response_data, ip_address, user_agent, api_key_used, app_name, app_version
                FROM sms_messages
                WHERE recipient = @recipient
                ORDER BY created_at DESC";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("recipient", recipient);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new SmsMessage
                {
                    Id = reader.GetInt32(0),
                    Recipient = reader.GetString(1),
                    Message = reader.GetString(2),
                    SenderId = reader.GetString(3),
                    MessageId = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Status = reader.GetString(5),
                    CreatedAt = reader.GetDateTime(6),
                    ResponseData = reader.IsDBNull(7) ? null : reader.GetString(7),
                    IpAddress = reader.IsDBNull(8) ? null : reader.GetString(8),
                    UserAgent = reader.IsDBNull(9) ? null : reader.GetString(9),
                    ApiKeyUsed = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AppName = reader.IsDBNull(11) ? null : reader.GetString(11),
                    AppVersion = reader.IsDBNull(12) ? null : reader.GetString(12)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve SMS messages for recipient from database");
            throw;
        }

        return messages;
    }
}
