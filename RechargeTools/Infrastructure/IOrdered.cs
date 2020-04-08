using System;

namespace RechargeTools.Infrastructure
{
    public interface IOrdered
    {
        // TODO: (MC) Make Nullable!
        int Ordinal { get; }
    }
}