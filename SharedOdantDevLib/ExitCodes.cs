using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedOdantDevLib;



//The posix standard says:
//exit codes 1 - 2, 126 - 165, and 255 [1] have special meanings,
//and should therefore be avoided for user-specified exit parameters.

public enum ExitCodes
{
    Success = 0,
    Restart = 9,
    Killed = 10,
    Exception = 11
}
