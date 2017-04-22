using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface ITaskLike
    {
        bool IsCompleted { get; }

        TaskLikeAwaiterBase GetAwaiter();
    }
}
