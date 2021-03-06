﻿// disable obsolete warnings. Tests will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;

    public class When_only_abstract_config_override_is_found : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_not_invoke_it()
        {
            return Scenario.Define<ScenarioContext>()
                .WithEndpoint<ConfigOverrideEndpoint>().Done(c => c.EndpointsStarted)
                .Run();
        }

        public class ConfigOverrideEndpoint : EndpointConfigurationBuilder
        {
            public ConfigOverrideEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            abstract class ConfigErrorQueue : IProvideConfiguration<SomeConfiguration>
            {
                public SomeConfiguration GetConfiguration()
                {
                    throw new NotImplementedException();
                }
            }

            class SomeConfiguration { }
        }
    }
}
#pragma warning restore CS0618