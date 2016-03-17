﻿namespace NServiceBus.AcceptanceTests.Mutators
{
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_using_outgoing_tm_mutator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_update_message()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new MessageToBeMutated())))
                    .Done(c => c.MessageProcessed)
                    .Repeat(r => r.For(NServiceBus.AcceptanceTests.ScenarioDescriptors.Serializers.Xml))
                    .Should(c =>
                    {
                        Assert.True(c.CanAddHeaders);
                        Assert.AreEqual("SomeValue", c.MutatedPropertyValue, "Mutator should be able to mutate body.");
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool CanAddHeaders { get; set; }
            public string MutatedPropertyValue { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyTransportMessageMutator : IMutateOutgoingTransportMessages, INeedInitialization
            {
                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingHeaders["HeaderSetByMutator"] = "some value";
                    context.OutgoingHeaders[Headers.EnclosedMessageTypes] = typeof(MessageThatMutatorChangesTo).FullName;
                    context.OutgoingBody = Encoding.UTF8.GetBytes("<MessageThatMutatorChangesTo><SomeProperty>SomeValue</SomeProperty></MessageThatMutatorChangesTo>");
                    return Task.FromResult(0);
                }

                public void Customize(EndpointConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageThatMutatorChangesTo>
            {
                Context testContext;

                public MessageToBeMutatedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageThatMutatorChangesTo message, IMessageHandlerContext context)
                {
                    testContext.CanAddHeaders = context.MessageHeaders.ContainsKey("HeaderSetByMutator");
                    testContext.MutatedPropertyValue = message.SomeProperty;
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }
            }

        }

        public class MessageToBeMutated : ICommand
        {
        }

        public class MessageThatMutatorChangesTo : ICommand
        {
            public string SomeProperty { get; set; }
        }
    }
}