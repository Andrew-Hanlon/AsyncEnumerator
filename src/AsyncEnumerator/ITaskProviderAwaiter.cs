using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface ITaskProviderAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void OnCompleted(Action continuation, ITaskLike asyncEnumerator);
    }
}
