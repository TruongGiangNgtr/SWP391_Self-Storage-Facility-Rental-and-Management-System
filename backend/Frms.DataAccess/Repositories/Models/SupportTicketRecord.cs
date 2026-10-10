using Frms.DataAccess.Persistence.Entities;
namespace Frms.DataAccess.Repositories.Models;
public sealed record SupportTicketRecord(SupportTicket Ticket, Guid FacilityId);
