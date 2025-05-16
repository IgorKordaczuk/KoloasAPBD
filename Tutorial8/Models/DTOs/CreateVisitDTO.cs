namespace Tutorial8.Models.DTOs;

public class CreateVisitDTO
{
    public int visitId { get; set; }
    public int clientID { get; set; }
    public string MechanicLicenseNumber { get; set; }
    public List<VisitServicesDTO> Services { get; set; }
}