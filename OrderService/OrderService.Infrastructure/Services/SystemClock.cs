using System;
using OrderService.Application.Interfaces.Services;

namespace OrderService.Infrastructure.Services
{
    public class SystemClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
