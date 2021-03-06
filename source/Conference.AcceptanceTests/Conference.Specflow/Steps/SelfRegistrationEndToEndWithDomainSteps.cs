﻿// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================


using System;
using System.Threading;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Registration;
using Registration.Commands;
using System.Linq;
using Registration.Events;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "SelfRegistrationEndToEndWithDomain")]
    public class SelfRegistrationEndToEndWithDomainSteps
    {
        private readonly ICommandBus commandBus;
        private Guid orderId;

        public SelfRegistrationEndToEndWithDomainSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
        }

        [When(@"the Registrant proceed to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            var command = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            command.ConferenceId = conferenceAlias.Id;
            orderId = command.OrderId;
            this.commandBus.Send(command);

            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout);
        }

        [Then(@"the command to register the selected Order Items is received")]
        public void ThenTheCommandToRegisterTheSelectedOrderItemsIsReceived()
        {
            var orderRepo = EventSourceHelper.GetRepository<Registration.Order>();
            Registration.Order order = orderRepo.Find(orderId);

            Assert.NotNull(order);
            Assert.Equal(orderId, order.Id);
        }

        [Then(@"the event for Order placed is emitted")]
        public void ThenTheEventForOrderPlacedIsEmitted()
        {
            //OrderPlaced
            var draftOrder = RegistrationHelper.FindInContext<DraftOrder>(orderId);

            Assert.NotNull(draftOrder);
        }

        [Then(@"the command for reserving the selected Seats is received")]
        public void ThenTheCommandForReservingTheSelectedSeatsIsReceived()
        {
            var repository = EventSourceHelper.GetRepository<SeatsAvailability>();
            var command = ScenarioContext.Current.Get<RegisterToConference>();

            var availability = repository.Find(command.ConferenceId);
            
            Assert.NotNull(availability);
        }

        [Then(@"the event for reserving the selected Seats is emitted")]
        public void ThenTheEventForReservingTheSelectedSeatsIsEmitted()
        {
            //SeatsReserved
            // This event will send the MarkSeatsAsReserved cmd
        }

        [Then(@"the command for marking the selected Seats as reserved is received")]
        public void ThenTheCommandForMarkingTheSelectedSeatsAsReservedIsReceived()
        {
            //MarkSeatsAsReserved
            var repository = EventSourceHelper.GetRepository<Registration.Order>();
            var order = repository.Find(orderId);

            Assert.NotNull(order);
        }

        [Then(@"the event for completing the Order reservation is emitted")]
        public void ThenTheEventForCompletingTheOrderReservationIsEmitted()
        {
            //OrderReservationCompleted
            var draftOrder = RegistrationHelper.FindInContext<DraftOrder>(orderId);
            
            Assert.Equal(DraftOrder.States.ReservationCompleted, draftOrder.State);
        }

        [Then(@"the event for calculating the total of \$(.*) is emitted")]
        public void ThenTheEventForCalculatingTheTotalIsEmitted(decimal total)
        {
            //OrderTotalsCalculated
            var conference = ScenarioContext.Current.Get<ConferenceInfo>();
            Order order = ConferenceHelper.FindOrder(conference.Id, orderId);

            Assert.NotNull(order);
            Assert.Equal(total, order.TotalAmount);
        }
    }
}
