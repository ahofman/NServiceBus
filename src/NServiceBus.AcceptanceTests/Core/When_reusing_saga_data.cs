namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_reusing_saga_data : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_still_work_even_though_the_idea_sucks()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaReuseEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid().ToString()
                    })))
                .Done(c => c.TimeoutInvokedForSaga1 || c.TimeoutInvokedForSaga2)
                .Run();

            Assert.True(context.TimeoutInvokedForSaga1);
            Assert.False(context.TimeoutInvokedForSaga2);
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutInvokedForSaga1 { get; set; }
            public bool TimeoutInvokedForSaga2 { get; set; }
        }

        public class SagaReuseEndpoint : EndpointConfigurationBuilder
        {
            public SagaReuseEndpoint()
            {
                EndpointSetup<DefaultServer>(c=>c.EnableFeature<TimeoutManager>());
            }

            public class ReuseSaga1 : Saga<ReuseSagaData>, IAmStartedByMessages<StartSagaMessage>,IHandleTimeouts<ReusedTimeout>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return RequestTimeout<ReusedTimeout>(context,TimeSpan.FromMilliseconds(10));
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReuseSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m=>m.SomeId).ToSaga(s=>s.SomeId);
                }

                public Task Timeout(ReusedTimeout state, IMessageHandlerContext context)
                {
                    Context.TimeoutInvokedForSaga1 = true;
                    return Task.FromResult(0);
                }
            }

            public class ReuseSaga2 : Saga<ReuseSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<ReusedTimeout>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                public Task Timeout(ReusedTimeout state, IMessageHandlerContext context)
                {
                    Context.TimeoutInvokedForSaga2 = true;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReuseSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId).ToSaga(s => s.SomeId);
                }

                public class ReuseSagaData1 : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }
            public class ReuseSagaData : ContainSagaData
            {
                public virtual string SomeId { get; set; }
            }
            public class ReusedTimeout
            {

            }
        }

        public class StartSagaMessage : IMessage
        {
            public string SomeId { get; set; }
        }
    }
}