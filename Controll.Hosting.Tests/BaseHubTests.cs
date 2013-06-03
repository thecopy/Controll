using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class BaseHubTests
    {
        [Test]
        public void ShouldRemoveClientFromUserWhenDisconnecting()
        {
            var hub = GetTestableBaseHub();

            hub.MockedSession.Setup(x => x.Delete(It.Is<ControllClient>(cc => cc.ConnectionId == hub.Context.ConnectionId))).Verifiable();
            hub.MockedControllRepository.Setup(x => x.GetClientByConnectionId(It.Is<String>(s => s == hub.Context.ConnectionId))).Returns(new ControllClient { ConnectionId = hub.Context.ConnectionId });

            hub.OnDisconnected();

            hub.MockedSession.Verify(x => x.Delete(It.Is<ControllClient>(cc => cc.ConnectionId == hub.Context.ConnectionId)), Times.Once());
        }

        [Test]
        public void ShouldRemoveClientFromUserWhenSigningOut()
        {
            var hub = GetTestableBaseHub();

            hub.MockedControllRepository.Setup(x => x.GetClientByConnectionId(It.Is<String>(s => s == hub.Context.ConnectionId))).Returns(new ControllClient { ConnectionId = hub.Context.ConnectionId });

            hub.SignOut();

            hub.MockedSession.Verify(x => x.Delete(It.Is<ControllClient>(cc => cc.ConnectionId == hub.Context.ConnectionId)), Times.Once());
        }
        
        private TestableBaseHub GetTestableBaseHub()
        {
            var mockedControllRepository = new Mock<IControllRepository>();
            var mockedControllService = new Mock<IControllService>();
            var mockPipeline = new Mock<IHubPipelineInvoker>();
            var mockedConnectionObject = new Mock<IConnection>();
            var mockedSession = new Mock<ISession>();
            mockedSession.Setup(s => s.BeginTransaction()).Returns(new Mock<ITransaction>().Object);

            var hub = new TestableBaseHub(mockedControllRepository, mockedControllService, mockedSession)
            {
                Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject.Object, "ZombieHub", "conn-id", new StateChangeTracker())
            };

            return hub;
        }

        private class TestableBaseHub : BaseHub
        {
            public Mock<IControllRepository> MockedControllRepository { get; private set; }
            public Mock<IControllService> MockedControllService { get; set; }
            public Mock<ISession> MockedSession { get; private set; }
            public Mock<IRequest> MockedRequest { get; private set; }

            public TestableBaseHub(
                Mock<IControllRepository> mockedControllRepository,
                Mock<IControllService> mockedControllService,
                Mock<ISession> mockedSession)
                : base(mockedSession.Object, mockedControllRepository.Object, mockedControllService.Object)
            {
                MockedControllRepository = mockedControllRepository;
                MockedControllService = mockedControllService;
                MockedSession = mockedSession;

                MockedRequest = new Mock<IRequest>();
                Context = new HubCallerContext(MockedRequest.Object, "conn-id");
            }
        }
    }
}
