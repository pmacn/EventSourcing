﻿using System;
using System.Runtime.Serialization;

namespace EventSourcing.Example
{
    [Serializable]
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

    [Serializable]
    [DataContract(Namespace = "ExampleContractNamespace")]
    public class OpenExample : ICommand<ExampleId>
    {
        public OpenExample() { }

        public OpenExample(ExampleId id)
        {
            Id = id;
            ExpectedVersion = 0;
        }

        [DataMember(Order = 1)]
        public ExampleId Id { get; set; }

        [DataMember(Order = 2)]
        public int ExpectedVersion { get; set; }
    }
}
