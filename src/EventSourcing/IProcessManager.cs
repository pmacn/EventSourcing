using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing
{
    public interface IProcessManager
    {
        Guid Id { get; }
        bool Completed { get; }
        IEnumerable<ICommand> Commands { get; }
    }
}
