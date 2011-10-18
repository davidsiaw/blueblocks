using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlocksLib
{
    public delegate void Action();
    public delegate TResult Func<TResult, T>(T arg1);
    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);

}
