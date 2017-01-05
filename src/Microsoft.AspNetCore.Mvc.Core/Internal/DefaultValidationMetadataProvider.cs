// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A default implementation of <see cref="IValidationMetadataProvider"/>.
    /// </summary>
    public class DefaultValidationMetadataProvider : IValidationMetadataProvider
    {
        /// <inheritdoc />
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var attribute in context.Attributes)
            {
                if (attribute is IModelValidator || attribute is IClientModelValidator)
                {
                    // If another provider has already added this attribute, do not repeat it.
                    // This will prevent attributes like RemoteAttribute (which implement ValidationAttribute and
                    // IClientModelValidator) to be added to the ValidationMetadata twice.
                    // This is to ensure we do not end up with duplication validation rules on the client side.
                    if (!context.ValidationMetadata.ValidatorMetadata.Contains(attribute))
                    {
                        context.ValidationMetadata.ValidatorMetadata.Add(attribute);
                    }
                }
            }

            // [ValidateNever] on a type affects properties in that type, not properties that have that type. Thus,
            // we ignore context.TypeAttributes for properties and don't check at all for types.
            if (context.Key.MetadataKind == ModelMetadataKind.Property)
            {
                var validateNever = context.PropertyAttributes.OfType<ValidateNeverAttribute>().FirstOrDefault();
                if (validateNever == null)
                {
                    // No [ValidateNever] on the property. Check if container has this attribute.
                    validateNever = context.Key.ContainerType.GetTypeInfo()
                        .GetCustomAttributes(typeof(ValidateNeverAttribute), inherit: true)
                        .Cast<ValidateNeverAttribute>()
                        .FirstOrDefault();
                }

                if (validateNever != null)
                {
                    context.ValidationMetadata.Validate = false;
                }
            }
        }
    }
}