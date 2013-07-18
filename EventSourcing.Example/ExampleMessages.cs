using EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EventSourcing.Example
{
    [DataContract(Namespace = "ExampleContractNamespace")]
    public class ExampleOpened : IEvent<ExampleId>
    {
        public ExampleOpened() { }
        public ExampleOpened(ExampleId id, DateTime openingDate)
        {
            Id = id;
            OpeningDate = openingDate;
        }

        [DataMember(Order = 1)]
        public ExampleId Id { get; private set; }

        [DataMember(Order = 2)]
        public DateTime OpeningDate { get; private set; }
    }

    [DataContract(Namespace = "ExampleContractNamespace")]
    public class OpenExample : ICommand<ExampleId>
    {
        public OpenExample() { }
        public OpenExample(ExampleId id)
        {
            Id = id;
        }

        [DataMember(Order = 1)]
        public ExampleId Id { get; set; }
    }
}
