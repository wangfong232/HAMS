using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HAMSPatient.Hubs
{
    public class BookingHub : Hub
    {
        public async Task NotifySlotChange(int slotId, string status, string? newRowVersion = null)
        {
            await Clients.Others.SendAsync("SlotStatusChanged", new
            {
                slotId,
                status,
                newRowVersion
            });
        }
    }
}
