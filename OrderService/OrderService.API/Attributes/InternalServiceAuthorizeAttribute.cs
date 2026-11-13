using System;

namespace OrderService.API.Attributes
{
    /// <summary>
    /// Metadata attribute designating an endpoint or controller as requiring internal service authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class InternalServiceAuthorizeAttribute : Attribute
    {
    }
}

