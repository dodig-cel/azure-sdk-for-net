﻿using System.Collections.Generic;
using System.Net;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.Azure.Test;
using Networks.Tests.Helpers;
using ResourceGroups.Tests;
using Xunit;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Networks.Tests
{
    using Microsoft.Rest.ClientRuntime.Azure.TestFramework;

    using SubResource = Microsoft.Azure.Management.Network.Models.SubResource;

    public class ApplicationGatewayTests
    {
        private static string GetChildAppGwResourceId(string subscriptionId,
                                                        string resourceGroupName,
                                                        string appGwname,
                                                        string childResourceType,
                                                        string childResourceName)
        {
            return string.Format(
                    "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Network/applicationGateways/{2}/{3}/{4}",
                    subscriptionId,
                    resourceGroupName,
                    appGwname,
                    childResourceType,
                    childResourceName);
        }

        private ApplicationGatewaySslCertificate CreateSslCertificate(string sslCertName, string password)
        {
            X509Certificate2 cert = new X509Certificate2("ApplicationGatewayCertificate\\GW5000.pfx", password, X509KeyStorageFlags.Exportable);
            ApplicationGatewaySslCertificate sslCert = new ApplicationGatewaySslCertificate()
            {
                Name = sslCertName,
                Data = Convert.ToBase64String(cert.Export(X509ContentType.Pfx, password)),
                Password = password
            };

            return sslCert;
        }

        private ApplicationGateway CreateApplicationGateway(string location, Subnet subnet, string resourceGroupName, string appGwName, string subscriptionId)
        {
            var gatewayIPConfigName = TestUtilities.GenerateName();
            var frontendIPConfigName = TestUtilities.GenerateName();
            var frontendPort1Name = TestUtilities.GenerateName();
            var frontendPort2Name = TestUtilities.GenerateName();            
            var backendAddressPoolName = TestUtilities.GenerateName();
            var backendHttpSettings1Name = TestUtilities.GenerateName();
            var backendHttpSettings2Name = TestUtilities.GenerateName();
            var requestRoutingRule1Name = TestUtilities.GenerateName();
            var requestRoutingRule2Name = TestUtilities.GenerateName();
            var httpListener1Name = TestUtilities.GenerateName();
            var httpListener2Name = TestUtilities.GenerateName();            
            var probeName = TestUtilities.GenerateName();
            var sslCertName = TestUtilities.GenerateName();
            var password = "1234";
            ApplicationGatewaySslCertificate sslCert = CreateSslCertificate(sslCertName, password);

            //var httpListenerMultiHostingName = TestUtilities.GenerateName();
            //var frontendPortMultiHostingName = TestUtilities.GenerateName();
            //var urlPathMapName = TestUtilities.GenerateName();
            //var imagePathRuleName = TestUtilities.GenerateName();
            //var videoPathRuleName = TestUtilities.GenerateName();
            //var requestRoutingRuleMultiHostingName = TestUtilities.GenerateName();

            var appGw = new ApplicationGateway()
            {
                Location = location,                
                Sku = new ApplicationGatewaySku()
                    {
                        Name = ApplicationGatewaySkuName.StandardSmall,
                        Tier = ApplicationGatewayTier.Standard,
                        Capacity = 2
                    },
                GatewayIPConfigurations = new List<ApplicationGatewayIPConfiguration>()
                    {
                        new ApplicationGatewayIPConfiguration()
                        {
                            Name = gatewayIPConfigName,
                            Subnet = new SubResource()
                            {
                                Id = subnet.Id
                            }
                        }
                    },
                SslCertificates = new List<ApplicationGatewaySslCertificate>()
                    {
                        sslCert
                    },
                FrontendIPConfigurations = new List<ApplicationGatewayFrontendIPConfiguration>() 
                    { 
                        new ApplicationGatewayFrontendIPConfiguration()
                        {
                            Name = frontendIPConfigName,
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            Subnet = new SubResource()
                            {
                                Id = subnet.Id
                            }                          
                        }                    
                    },
                FrontendPorts = new List<ApplicationGatewayFrontendPort>
                    {
                        new ApplicationGatewayFrontendPort()
                        {
                            Name = frontendPort1Name,
                            Port = 80
                        },
                        new ApplicationGatewayFrontendPort()
                        {
                            Name = frontendPort2Name,
                            Port = 443
                        },
                        //new ApplicationGatewayFrontendPort()
                        //{
                        //    Name = frontendPortMultiHostingName,
                        //    Port = 8080
                        //}
                    },
                Probes = new List<ApplicationGatewayProbe>
                    {
                        new ApplicationGatewayProbe()
                        {
                            Name = probeName,
                            Protocol = ApplicationGatewayProtocol.Http,
                            Host = "probe.com",
                            Path = "/path/path.htm",
                            Interval = 17,
                            Timeout = 17,
                            UnhealthyThreshold = 5
                        }
                    },
                BackendAddressPools = new List<ApplicationGatewayBackendAddressPool>
                    {
                        new ApplicationGatewayBackendAddressPool()
                        {
                            Name = backendAddressPoolName,
                            BackendAddresses = new List<ApplicationGatewayBackendAddress>()
                            {
                                new ApplicationGatewayBackendAddress()
                                {
                                    IpAddress = "104.42.6.202"
                                },
                                new ApplicationGatewayBackendAddress()
                                {
                                    IpAddress = "23.99.1.115"
                                }
                            }
                        }
                    },
                BackendHttpSettingsCollection = new List<ApplicationGatewayBackendHttpSettings> 
                    {
                        new ApplicationGatewayBackendHttpSettings()
                        {
                            Name = backendHttpSettings1Name,
                            Port = 80,
                            Protocol = ApplicationGatewayProtocol.Http,
                            CookieBasedAffinity = ApplicationGatewayCookieBasedAffinity.Disabled,
                            RequestTimeout = 69,
                            Probe = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "probes", probeName)
                            }
                        },
                        new ApplicationGatewayBackendHttpSettings()
                        {
                            Name = backendHttpSettings2Name,
                            Port = 80,
                            Protocol = ApplicationGatewayProtocol.Http,
                            CookieBasedAffinity = ApplicationGatewayCookieBasedAffinity.Enabled,                            
                        }
                    },
                HttpListeners = new List<ApplicationGatewayHttpListener>
                    {
                        new ApplicationGatewayHttpListener()
                        {
                            Name = httpListener1Name,
                            FrontendPort = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendPorts", frontendPort1Name)
                            },
                            FrontendIPConfiguration = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendIPConfigurations", frontendIPConfigName)
                            },
                            SslCertificate = null,
                            Protocol = ApplicationGatewayProtocol.Http
                        },
                        new ApplicationGatewayHttpListener()
                        {
                            Name = httpListener2Name,
                            FrontendPort = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendPorts", frontendPort2Name)
                            },
                            FrontendIPConfiguration = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendIPConfigurations", frontendIPConfigName)
                            },
                            SslCertificate = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "sslCertificates", sslCertName)
                            },
                            Protocol = ApplicationGatewayProtocol.Https                            
                        },                        
                        //new ApplicationGatewayHttpListener()
                        //{
                        //    Name = httpListenerMultiHostingName,
                        //    FrontendPort = new SubResource()
                        //    {
                        //        Id = GetChildAppGwResourceId(subscriptionId,
                        //            resourceGroupName, appGwName, "frontendPorts", frontendPortMultiHostingName)
                        //    },
                        //    FrontendIPConfiguration = new SubResource()
                        //    {
                        //        Id = GetChildAppGwResourceId(subscriptionId,
                        //            resourceGroupName, appGwName, "frontendIPConfigurations", frontendIPConfigName)
                        //    },
                        //    SslCertificate = null,
                        //    Protocol = ApplicationGatewayProtocol.Http
                        //}
                    },
                //UrlPathMaps = new List<ApplicationGatewayUrlPathMap>()
                //    {
                //        new ApplicationGatewayUrlPathMap()
                //        {
                //            Name = urlPathMapName,
                //            DefaultBackendAddressPool = new SubResource()
                //            {
                //                Id = GetChildAppGwResourceId(subscriptionId,
                //                    resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                //            },
                //            DefaultBackendHttpSettings = new SubResource()
                //            {
                //                Id = GetChildAppGwResourceId(subscriptionId,
                //                    resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettingsName)
                //            },
                //            PathRules = new List<ApplicationGatewayPathRule>()
                //            {
                //                new ApplicationGatewayPathRule()
                //                {
                //                    Name = imagePathRuleName,
                //                    Paths = new List<string>() { "/images*" },
                //                    BackendAddressPool = new SubResource()
                //                    {
                //                        Id = GetChildAppGwResourceId(subscriptionId,
                //                        resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                //                    },
                //                    BackendHttpSettings = new SubResource()
                //                    {
                //                        Id = GetChildAppGwResourceId(subscriptionId,
                //                        resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettingsName)
                //                    }
                //                },
                //                new ApplicationGatewayPathRule()
                //                {
                //                    Name = videoPathRuleName,
                //                    Paths = new List<string>() { "/videos*" },
                //                    BackendAddressPool = new SubResource()
                //                    {
                //                        Id = GetChildAppGwResourceId(subscriptionId,
                //                        resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                //                    },
                //                    BackendHttpSettings = new SubResource()
                //                    {
                //                        Id = GetChildAppGwResourceId(subscriptionId,
                //                        resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettingsName)
                //                    }
                //                }
                //            }
                //        }
                //    },                
                RequestRoutingRules = new List<ApplicationGatewayRequestRoutingRule>()
                    {
                        new ApplicationGatewayRequestRoutingRule()
                        {
                            Name = requestRoutingRule1Name,
                            RuleType = ApplicationGatewayRequestRoutingRuleType.Basic,
                            HttpListener = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "httpListeners", httpListener1Name)
                            },
                            BackendAddressPool = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                            },
                            BackendHttpSettings = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettings1Name)
                            }
                        },
                        new ApplicationGatewayRequestRoutingRule()
                        {
                            Name = requestRoutingRule2Name,
                            RuleType = ApplicationGatewayRequestRoutingRuleType.Basic,
                            HttpListener = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "httpListeners", httpListener2Name)
                            },
                            BackendAddressPool = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                            },
                            BackendHttpSettings = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettings2Name)
                            }
                        },
                        //new ApplicationGatewayRequestRoutingRule()
                        //{
                        //    Name = requestRoutingRuleMultiHostingName,
                        //    RuleType = ApplicationGatewayRequestRoutingRuleType.PathBasedRouting,
                        //    HttpListener = new SubResource()
                        //    {
                        //        Id = GetChildAppGwResourceId(subscriptionId,
                        //            resourceGroupName, appGwName, "httpListeners", httpListenerMultiHostingName)
                        //    },
                        //    UrlPathMap = new SubResource()
                        //    {
                        //        Id = GetChildAppGwResourceId(subscriptionId,
                        //            resourceGroupName, appGwName, "urlPathMaps", urlPathMapName)
                        //    }
                        //}
                    }
            };
            return appGw;
        }

        private ApplicationGateway CreateApplicationGatewayWithSsl(string location, Subnet subnet, string resourceGroupName, string subscriptionId)
        {
            var appGwName = TestUtilities.GenerateName();
            var gatewayIPConfigName = TestUtilities.GenerateName();
            var frontendIPConfigName = TestUtilities.GenerateName();
            var frontendPortName = TestUtilities.GenerateName();
            var backendAddressPoolName = TestUtilities.GenerateName();
            var backendHttpSettingsName = TestUtilities.GenerateName();
            var requestRoutingRuleName = TestUtilities.GenerateName();
            var sslCertName = TestUtilities.GenerateName();            
            var httpListenerName = TestUtilities.GenerateName();
            var password = "1234";                        
            ApplicationGatewaySslCertificate sslCert = CreateSslCertificate(sslCertName, password);

            var appGw = new ApplicationGateway()
            {
                Location = location,
                Sku = new ApplicationGatewaySku()
                {
                    Name = ApplicationGatewaySkuName.StandardLarge,
                    Tier = ApplicationGatewayTier.Standard,
                    Capacity = 2
                },
                GatewayIPConfigurations = new List<ApplicationGatewayIPConfiguration>()
                    {
                        new ApplicationGatewayIPConfiguration()
                        {
                            Name = gatewayIPConfigName,
                            Subnet = new SubResource()
                            {
                                Id = subnet.Id
                            }
                        }
                    },
                SslCertificates = new List<ApplicationGatewaySslCertificate>()
                    {
                        sslCert
                    },
                FrontendIPConfigurations = new List<ApplicationGatewayFrontendIPConfiguration>() 
                    { 
                        new ApplicationGatewayFrontendIPConfiguration()
                        {
                            Name = frontendIPConfigName,
                            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                            Subnet = new SubResource()
                            {
                                Id = subnet.Id
                            } 
                        }                    
                    },
                FrontendPorts = new List<ApplicationGatewayFrontendPort>
                    {
                        new ApplicationGatewayFrontendPort()
                        {
                            Name = frontendPortName,
                            Port = 443
                        }
                    },
                BackendAddressPools = new List<ApplicationGatewayBackendAddressPool>
                    {
                        new ApplicationGatewayBackendAddressPool()
                        {
                            Name = backendAddressPoolName,
                            BackendAddresses = new List<ApplicationGatewayBackendAddress>()
                            {
                                new ApplicationGatewayBackendAddress()
                                {
                                    IpAddress = "10.2.0.1"
                                },
                                new ApplicationGatewayBackendAddress()
                                {
                                    IpAddress = "10.2.0.2"
                                }
                            }
                        }
                    },
                BackendHttpSettingsCollection = new List<ApplicationGatewayBackendHttpSettings> 
                    {
                        new ApplicationGatewayBackendHttpSettings()
                        {
                            Name = backendHttpSettingsName,
                            Port = 80,
                            Protocol = ApplicationGatewayProtocol.Http,
                            CookieBasedAffinity = ApplicationGatewayCookieBasedAffinity.Enabled
                        }
                    },
                HttpListeners = new List<ApplicationGatewayHttpListener>
                    {
                        new ApplicationGatewayHttpListener()
                        {
                            Name = httpListenerName,
                            FrontendPort = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendPorts", frontendPortName)
                            },
                            FrontendIPConfiguration = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "frontendIPConfigurations", frontendIPConfigName)
                            },
                            SslCertificate = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "sslCertificates", sslCertName)
                            },
                            Protocol = ApplicationGatewayProtocol.Https
                        }
                    },
                RequestRoutingRules = new List<ApplicationGatewayRequestRoutingRule>()
                    {
                        new ApplicationGatewayRequestRoutingRule()
                        {
                            Name = requestRoutingRuleName,
                            RuleType = ApplicationGatewayRequestRoutingRuleType.Basic,
                            HttpListener = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "httpListeners", httpListenerName)
                            },
                            BackendAddressPool = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendAddressPools", backendAddressPoolName)
                            },
                            BackendHttpSettings = new SubResource()
                            {
                                Id = GetChildAppGwResourceId(subscriptionId,
                                    resourceGroupName, appGwName, "backendHttpSettingsCollection", backendHttpSettingsName)
                            }
                        }
                    }
            };
            return appGw;
        }

        private void CompareApplicationGateway(ApplicationGateway gw1, ApplicationGateway gw2)
        {
            Assert.Equal(gw1.Sku.Name, gw2.Sku.Name);
            Assert.Equal(gw1.Sku.Tier, gw2.Sku.Tier);
            Assert.Equal(gw1.Sku.Capacity, gw2.Sku.Capacity);
            Assert.Equal(gw1.GatewayIPConfigurations.Count, gw2.GatewayIPConfigurations.Count);
            Assert.Equal(gw1.FrontendIPConfigurations.Count, gw2.FrontendIPConfigurations.Count);
            Assert.Equal(gw1.FrontendPorts.Count, gw2.FrontendPorts.Count);
            Assert.Equal(gw1.Probes.Count, gw2.Probes.Count);
            Assert.Equal(gw1.SslCertificates.Count, gw2.SslCertificates.Count);
            Assert.Equal(gw1.BackendAddressPools.Count, gw2.BackendAddressPools.Count);
            Assert.Equal(gw1.BackendHttpSettingsCollection.Count, gw2.BackendHttpSettingsCollection.Count);
            Assert.Equal(gw1.HttpListeners.Count, gw2.HttpListeners.Count);
            Assert.Equal(gw1.RequestRoutingRules.Count, gw2.RequestRoutingRules.Count);            
        }

        [Fact]
        public void ApplicationGatewayApiTest()
        {
            var handler1 = new RecordedDelegatingHandler { StatusCodeToReturn = HttpStatusCode.OK };
            var handler2 = new RecordedDelegatingHandler { StatusCodeToReturn = HttpStatusCode.OK };

            using (MockContext context = MockContext.Start(this.GetType().FullName))
            {
                
                var resourcesClient = ResourcesManagementTestUtilities.GetResourceManagementClientWithHandler(context, handler1);
                var networkManagementClient = NetworkManagementTestUtilities.GetNetworkManagementClientWithHandler(context, handler2);

                var location = NetworkManagementTestUtilities.GetResourceLocation(resourcesClient, "Microsoft.Network/applicationgateways");

                string resourceGroupName = TestUtilities.GenerateName("csmrg");
                resourcesClient.ResourceGroups.CreateOrUpdate(resourceGroupName,
                    new ResourceGroup
                    {
                        Location = location
                    });

                var vnetName = TestUtilities.GenerateName();
                var subnetName = TestUtilities.GenerateName();
                var appGwName = TestUtilities.GenerateName();

                var virtualNetwork = TestHelper.CreateVirtualNetwork(vnetName, subnetName, resourceGroupName, location, networkManagementClient);
                var getSubnetResponse = networkManagementClient.Subnets.Get(resourceGroupName, vnetName, subnetName);
                Console.WriteLine("Virtual Network GatewaySubnet Id: {0}", getSubnetResponse.Id);
                var subnet = getSubnetResponse;

                var appGw = CreateApplicationGateway(location, subnet, resourceGroupName, appGwName, networkManagementClient.SubscriptionId);     

                // Put AppGw                
                var putAppGwResponse = networkManagementClient.ApplicationGateways.CreateOrUpdate(resourceGroupName, appGwName, appGw);                
                Assert.Equal("Succeeded", putAppGwResponse.ProvisioningState);
                
                // Get AppGw
                var getResp = networkManagementClient.ApplicationGateways.Get(resourceGroupName, appGwName);
                Assert.Equal(appGwName, getResp.Name);
                CompareApplicationGateway(appGw, getResp);

                //Start AppGw
                networkManagementClient.ApplicationGateways.Start(resourceGroupName, appGwName);
                
                //Stop AppGw
                networkManagementClient.ApplicationGateways.Stop(resourceGroupName, appGwName);
                
                // Delete AppGw
                networkManagementClient.ApplicationGateways.Delete(resourceGroupName, appGwName);
            }
        }        
    }
}