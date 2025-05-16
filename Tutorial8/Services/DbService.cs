using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class DbService : IDbService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";
    
    public async Task<VisitDTO> GetVisitAsync(int visitId)
    {

        string query = @"Select Date,c.first_name, c.last_name, c.date_of_birth, m.mechanic_id, m.licence_number, s.name, vs.service_fee From Visit
                INNER JOIN Client c on Visit.client_id = c.client_id
                INNER JOIN Mechanic m ON Visit.mechanic_id = m.mechanic_id
                INNER JOIN Visit_Service vs on Visit.visit_id = vs.visit_id
                INNER JOIN Service s ON vs.service_id = s.service_id
                WHERE vs.visit_id = @VisitId";
        
        VisitDTO? visit = null;
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = query;
        await connection.OpenAsync();
        
        command.Parameters.AddWithValue("@VisitId", visitId);
        var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (visit is null)
            {
                visit = new VisitDTO()
                {
                    Date = reader.GetDateTime(0),
                    Client = new ClientDTO()
                    {
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        DateOfBirth = reader.GetDateTime(3),
                    },
                    Mechanic = new MechanicDTO()
                    {
                        MechanicId = reader.GetInt32(4),
                        LicenceNumber = reader.GetString(5),
                    },
                    VisitServices = new List<VisitServicesDTO>(),
                };
                
                string visitServiceName = reader.GetString(6);
                
                var visitService = visit.VisitServices.FirstOrDefault(e => e.Name.Equals(visitServiceName));
                if (visitService is null)
                {
                    visitService = new VisitServicesDTO()
                    {
                        Name = visitServiceName,
                        ServiceFee = reader.GetDecimal(7),
                    };
                    visit.VisitServices.Add(visitService);
                }
                    
                    
            }
        }
            
        if (visit is null)
        {
            throw new NotFoundException("No rentals found for the specified customer.");
        }
        
        return visit;
    }

    public async Task AddNewVisitAsync(CreateVisitDTO createVisitDto)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Visit WHERE visit_id = @VisitId;";
            command.Parameters.AddWithValue("@VisitId", createVisitDto.visitId);
                
            var visitIdRes = await command.ExecuteScalarAsync();
            if(visitIdRes is not null)
                throw new ConflictException($"Visit with ID - {createVisitDto.visitId} - already exists.");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Client WHERE client_id = @ClientId";
            command.Parameters.AddWithValue("@ClientId", createVisitDto.clientID);
            
            var clientIdRes = await command.ExecuteScalarAsync();
            if(clientIdRes is null)
                throw new NotFoundException($"Client with ID - {createVisitDto.visitId} - not found.");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT mechanic_id From Mechanic WHERE licence_number = @MechanicLicenceNumber";
            command.Parameters.AddWithValue("@MechanicLicenceNumber", createVisitDto.MechanicLicenseNumber);
            
            // ID MEchanika
            var mechanicId = await command.ExecuteScalarAsync();
            if(mechanicId is null)
                throw new NotFoundException($"Mechanic with License - {createVisitDto.MechanicLicenseNumber} - not found.");

            foreach (var service in createVisitDto.Services)
            {
                command.Parameters.Clear();
                command.CommandText = "SELECT service_id from Service WHERE name = @ServiceName";
                command.Parameters.AddWithValue("@ServiceName", service.Name);
                
                var serviceId = await command.ExecuteScalarAsync();
                if(serviceId is null)
                    throw new NotFoundException($"Servie with Name - {service.Name} - not found.");
                
                //Insert do Visit
                command.Parameters.Clear();
                command.CommandText = 
                    @"INSERT INTO Visit VALUES(@VisitId, @ClientId, @MechanicId, @Time)";
        
                command.Parameters.AddWithValue("@VisitId", createVisitDto.visitId);
                command.Parameters.AddWithValue("@ClientId", createVisitDto.clientID);
                command.Parameters.AddWithValue("@MechanicId", mechanicId);
                command.Parameters.AddWithValue("@Time", DateTime.Now);
                
                command.ExecuteNonQuery();
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        

    }
    
}