﻿// <copyright file="IPEndPointUtility.cs" company="Advanced Metering Services LLC">
//     Copyright (c) Advanced Metering Services LLC. All rights reserved.
// </copyright>

namespace ParalectEventSourcing.Utils
{
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Enpoint utility
    /// </summary>
    public static class IpEndPointUtility
    {
        private static readonly Regex EndpointSplit = new Regex(@"(?<host>.*):(?<port>[0-9]*)");

        /// <summary>
        /// Creates endpoint
        /// </summary>
        /// <param name="endPoint">endpoint</param>
        /// <returns>endpoint formed to specific object</returns>
        public static async Task<IPEndPoint> CreateIpEndPoint(string endPoint)
        {
            var match = EndpointSplit.Match(endPoint);
            string host = match.Groups["host"].Value;
            string portString = match.Groups["port"].Value;
            int port = int.Parse(portString, NumberStyles.None, NumberFormatInfo.CurrentInfo);
            return await CreateIpEndPoint(host, port);
        }

        /// <summary>
        /// Creates endpoint
        /// </summary>
        /// <param name="host">the host</param>
        /// <param name="port">the port</param>
        /// <returns>endpoint formed to specific object</returns>
        public static async Task<IPEndPoint> CreateIpEndPoint(string host, int port)
        {
            var ip = await GetHostIp(host);
            return new IPEndPoint(ip, port);
        }

        public static async Task<IPAddress> GetHostIp(string host)
        {
            return (await Dns.GetHostAddressesAsync(host)).First();
        }
    }
}