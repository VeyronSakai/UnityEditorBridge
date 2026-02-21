using System;
using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Editor.Domains.Interfaces
{
    internal interface IRequestRouter
    {
        void Register(HttpMethodType method, string path,
            Func<IRequestContext, CancellationToken, Task> handler);

        Task HandleRequestAsync(IRequestContext context, CancellationToken cancellationToken);
    }
}
