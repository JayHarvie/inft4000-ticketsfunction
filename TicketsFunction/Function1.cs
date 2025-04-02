using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace TicketsFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("tickethub", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation("C# Queue trigger function processed: {MessageText}", message.MessageText);

            string messageJson = message.MessageText;

            // Deserialize the message
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Ticket? ticket = JsonSerializer.Deserialize<Ticket>(messageJson, options);

            if (ticket == null) 
            {
                _logger.LogError("Failed to deserialize the message");
                return;
            }

            _logger.LogInformation("{Name}, Your ticket has been deserialized", ticket.Name);

            // Add ticket to database

            // get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync(); // Note the ASYNC

                var query = "INSERT INTO Tickets (ConcertId, Email, Name, Phone, City, Country, Address, Province, Quantity, CreditCard, Expiration, PostalCode, SecurityCode) VALUES (@ConcertId, @Email, @Name, @Phone, @City, @Country, @Address, @Province, @Quantity, @CreditCard, @Expiration, @PostalCode, @SecurityCode);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConcertId", ticket.ConcertId);
                    cmd.Parameters.AddWithValue("@Email", ticket.Email);
                    cmd.Parameters.AddWithValue("@Name", ticket.Name);
                    cmd.Parameters.AddWithValue("@Phone", ticket.Phone);
                    cmd.Parameters.AddWithValue("@City", ticket.City);
                    cmd.Parameters.AddWithValue("@Country", ticket.Country);
                    cmd.Parameters.AddWithValue("@Address", ticket.Address);
                    cmd.Parameters.AddWithValue("@Province", ticket.Province);
                    cmd.Parameters.AddWithValue("@Quantity", ticket.Quantity);
                    cmd.Parameters.AddWithValue("@CreditCard", ticket.CreditCard);
                    cmd.Parameters.AddWithValue("@Expiration", ticket.Expiration);
                    cmd.Parameters.AddWithValue("@PostalCode", ticket.PostalCode);
                    cmd.Parameters.AddWithValue("@SecurityCode", ticket.SecurityCode);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
