using System;

namespace OrderService.Application.Interfaces.Services
{
    public interface ISystemClock
    {
        DateTime UtcNow { get; }
    }
}
