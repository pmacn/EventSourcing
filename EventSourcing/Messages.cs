using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ELI.EventSourcing
{
    public interface IEvent { }

    public interface IEvent<TIdentity> : IEvent
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface ICommand { }

    public interface ICommand<TIdentity> : ICommand
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public class ApplicationServiceHost
    {
        
    }
}