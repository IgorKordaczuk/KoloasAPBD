using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IDbService
{
    Task<VisitDTO> GetVisitAsync(int visitId);
    Task AddNewVisitAsync(CreateVisitDTO createVisitDto);
}