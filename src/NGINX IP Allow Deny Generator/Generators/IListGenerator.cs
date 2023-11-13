using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPADG.Generators;

public interface IListGenerator
{
    #region Methods

    Task<IReadOnlyCollection<string>> GeneratorAsync();

    #endregion
}