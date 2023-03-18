using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;


namespace Yanmonet.NetSync.Editor.Tests
{
    public class IPAddressTests
    {

        [Test]
        public void GetIPFromDns()
        {
            foreach (var ip in (Dns.GetHostEntry(Dns.GetHostName())).AddressList)
            {
                Debug.Log($"IP: {ip}, {ip.AddressFamily}");
            }
        }

        [Test]
        public void GetIP()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = adapter.GetIPProperties();
                
                if (props.UnicastAddresses.Count == 0)
                    continue;
                IPAddress gateway = null;

                foreach (var prop in props.GatewayAddresses)
                {
                    if (prop.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        gateway = prop.Address;
                        break;
                    }
                }

                if (gateway == null)
                    return;
                foreach (var prop in props.UnicastAddresses)
                {
                    if (prop.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Debug.Log($"IP: {prop.Address}");
                    }
                }

            }
        }

        [Test]
        public void GetIPFromNetworkInterface()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = adapter.GetIPProperties();
                Debug.Log($"==== {adapter.GetType().Name} Name: {adapter.Name}, NetworkInterfaceType: {adapter.NetworkInterfaceType}, OperationalStatus: {adapter.OperationalStatus}");
                foreach (var prop in props.UnicastAddresses)
                {
                    var address = prop.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork ||
                        address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Debug.Log($"UnicastAddresses : {address}, {address.AddressFamily}");
                    }
                }

                foreach (var prop in props.MulticastAddresses)
                {
                    var address = prop.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork ||
                        address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Debug.Log($"MulticastAddresses : {address}, {address.AddressFamily}");
                    }
                }


                foreach (var prop in props.GatewayAddresses)
                {
                    var address = prop.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork ||
                        address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Debug.Log($"GatewayAddresses : {address}, {address.AddressFamily}");
                    }
                }

                foreach (var address in props.DnsAddresses)
                {
                    Debug.Log($"DnsAddresses : {address}, {address.AddressFamily}");
                }
            }
        }
    }
}