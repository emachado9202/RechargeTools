using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Catalog
{
    public abstract partial class GenericEntity : IEquatable<GenericEntity>
    {
        [Key]
        public Guid Id { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GenericEntity);
        }

        bool IEquatable<GenericEntity>.Equals(GenericEntity other)
        {
            return this.Equals(other);
        }

        /// <summary>
		/// Transient objects are not associated with an item already in storage. For instance,
		/// a Product entity is transient if its Id is 0.
		/// </summary>
		public virtual bool IsTransientRecord()
        {
            return Id == Guid.Empty;
        }

        public Type GetUnproxiedType()
        {
            #region Old

            //var t = GetType();
            //if (t.AssemblyQualifiedName.StartsWith("System.Data.Entity."))
            //{
            //	// it's a proxied type
            //	t = t.BaseType;
            //}

            //return t;

            #endregion Old

            return ObjectContext.GetObjectType(GetType());
        }

        protected virtual bool Equals(GenericEntity other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (HasSameNonDefaultIds(other))
            {
                var otherType = other.GetUnproxiedType();
                var thisType = GetUnproxiedType();
                return thisType.Equals(otherType);
            }

            return false;
        }

        private bool HasSameNonDefaultIds(GenericEntity other)
        {
            return !this.IsTransientRecord() && !other.IsTransientRecord() && this.Id == other.Id;
        }
    }
}