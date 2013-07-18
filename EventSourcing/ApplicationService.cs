using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventSourcing
{
    public interface IApplicationService<TIdentity>
        where TIdentity : IIdentity
    {
        void Execute(ICommand<TIdentity> command);
    }

    public abstract class ApplicationService<TIdentity> : IApplicationService<TIdentity>
        where TIdentity : IIdentity
    {
        public void Execute(ICommand<TIdentity> command)
        {
            ((dynamic)this).When((dynamic)command);
        }
    }
}