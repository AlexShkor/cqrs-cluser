﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApi.Controllers
{
    using Contracts.Commands;
    using DataService;
    using ParalectEventSourcing.Commands;

    [Route("api/[controller]")]
    public class ShipmentsController : Controller
    {
        private readonly ICommandBus _commandBus;
        private readonly IShipmentDataService _shipmentDataService;

        public ShipmentsController(ICommandBus commandBus, IShipmentDataService shipmentDataService)
        {
            _commandBus = commandBus;
            _shipmentDataService = shipmentDataService;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<Shipment> Get()
        {
            return _shipmentDataService.GetAllShipments();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public object Post([FromBody]string address)
        {
            var connectionId = Request.Headers["Connection-Id"];
            var commandId  = Guid.NewGuid().ToString();
            var command = new CreateShipment
            {
                Id = Guid.NewGuid().ToString(),
                Address = address,
                Metadata = new CommandMetadata
                {
                    CommandId = commandId,
                    CreatedDate = DateTime.Now,
                    TypeName = typeof(CreateShipment).AssemblyQualifiedName
                }
            };

            _commandBus.Send(command);

            return new { commandId };
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public object Put(string id, [FromBody]string newAddress)
        {
            var commandId = Guid.NewGuid().ToString();
            var command = new ChangeShipmentAddress
            {
                Id = id,
                NewAddress = newAddress,
                Metadata = new CommandMetadata
                {
                    CommandId = commandId,
                    CreatedDate = DateTime.Now,
                    TypeName = typeof(ChangeShipmentAddress).AssemblyQualifiedName
                }
            };

            _commandBus.Send(command);

            return new { commandId };
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
