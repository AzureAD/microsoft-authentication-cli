// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Specifies that a value must be a legal file name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class LegalFileNameAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LegalFileNameAttribute"/> class.
        /// </summary>
        public LegalFileNameAttribute()
            : base("'{0}' is an invalid file name.")
        {
        }

        /// <inheritdoc/>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string filename)
            {
                try
                {
                    if (LegalFileNameChecker.IsValidFilename(filename))
                    {
                        return ValidationResult.Success;
                    }
                }
                catch
                {
                }
            }

            return new ValidationResult(this.FormatErrorMessage(value as string));
        }
    }
}
