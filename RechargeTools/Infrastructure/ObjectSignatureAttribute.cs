using System;

namespace RechargeTools.Infrastructure
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ObjectSignatureAttribute : Attribute
    {
    }
}