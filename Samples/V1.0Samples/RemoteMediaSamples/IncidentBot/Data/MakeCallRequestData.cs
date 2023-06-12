// <copyright file="MakeCallRequestData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.IncidentBot.Data
{
    /// <summary>
    /// The outgoing call request body.
    /// </summary>
    public class MakeCallRequestData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MakeCallRequestData"/> class.
        /// </summary>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="phoneNumber">a.</param>
        public MakeCallRequestData(string tenantId, string phoneNumber)
        {
            this.TenantId = tenantId;
            this.PhoneNumber = phoneNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MakeCallRequestData"/> class.
        /// </summary>
        private MakeCallRequestData()
        {
        }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the object id.
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}
