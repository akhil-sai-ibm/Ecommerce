using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Services
{
    public interface IOutboxService
    {
        Task ProcessOutboxMessagesAsync();
    }
}
