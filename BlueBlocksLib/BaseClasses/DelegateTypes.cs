using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlocksLib
{
    public delegate void Action();
    public delegate TResult Func<T, TResult>(T arg1);
}
