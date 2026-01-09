using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyStringGeneration
{
    public interface IStringPipelineStep
    {
        /// <summary>
        /// Executes this step on the input set,
        /// and possibly records newly generated strings in globalGenerated.
        /// Returns the updated set for the next step.
        /// </summary>
        HashSet<string> Execute(HashSet<string> input, HashSet<string> globalGenerated);
    }
}
