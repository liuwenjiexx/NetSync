using NUnit.Framework;
using System;
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

        [Test]
        public void ParseIP()
        {
            string serverType;
            string ipString;
            string address;
            int port;

            ipString = "127.0.0.1";
            Assert.IsTrue(NetworkUtility.TryParseIPAddress(ipString, out serverType, out address, out port));
            Debug.Log($"{ipString} => IP: {address}, Port: {port}");
            Assert.AreEqual("127.0.0.1", address);
            Assert.AreEqual(0, port);

            ipString = "127.0.0.1:7777";
            Assert.IsTrue(NetworkUtility.TryParseIPAddress(ipString, out serverType, out address, out port));
            Debug.Log($"{ipString} => IP: {address}, Port: {port}");
            Assert.AreEqual("127.0.0.1", address);
            Assert.AreEqual(7777, port);


            ipString = "ip://127.0.0.1:7777";
            Assert.IsTrue(NetworkUtility.TryParseIPAddress(ipString, out serverType, out address, out port));
            Debug.Log($"{ipString} => IP: {address}, Port: {port}");
            Assert.AreEqual("127.0.0.1", address);
            Assert.AreEqual(7777, port);

            Uri uri;
            ipString = "steam://123456789";
            Assert.IsTrue(Uri.TryCreate(ipString, UriKind.RelativeOrAbsolute, out uri));
            Debug.Log($"{ipString} => Scheme: {uri.Scheme}, Authority: {uri.Authority}, Port: {uri.Port}, HostType: {uri.HostNameType}");

            ipString = "http://a.b.c.d:7777";
            Uri.TryCreate(ipString, UriKind.RelativeOrAbsolute, out uri);
            Assert.AreEqual("a.b.c.d", uri.Host);
            Assert.AreEqual(7777, uri.Port);

        }

        [Test]
        public void Timestamp()
        {
            Debug.Log("Timestamp int.MaxValue, " + NetworkUtility.FromTimestamp(int.MaxValue));
            Debug.Log("TimestampSeconds int.MaxValue, " + NetworkUtility.FromTimestampSeconds(int.MaxValue));

            Debug.Log("Timestamp uint.MaxValue, " + NetworkUtility.FromTimestamp(uint.MaxValue));
            Debug.Log("TimestampSeconds uint.MaxValue, " + NetworkUtility.FromTimestampSeconds(uint.MaxValue));

            Debug.Log("Timestamp long.MaxValue, " + NetworkUtility.FromTimestamp(int.MaxValue*10_000L));
            Debug.Log("TimestampSeconds long.MaxValue, " + NetworkUtility.FromTimestampSeconds(int.MaxValue*10L));
        }
    }


}