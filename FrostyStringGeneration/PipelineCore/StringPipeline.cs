using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyStringGeneration
{
    public class StringPipeline
    {
        private readonly List<IStringPipelineStep> _steps = new List<IStringPipelineStep>();
        private readonly HashSet<string> _globalGenerated = new HashSet<string>();

        public StringPipeline AddStep(IStringPipelineStep step)
        {
            _steps.Add(step);
            return this;
        }

        public (HashSet<string> final, HashSet<string> generated) Run(HashSet<string> input)
        {
            var current = input;
            foreach (var step in _steps)
            {
                current = step.Execute(current, _globalGenerated);
            }
            return (current, _globalGenerated);
        }

        public void ClearGenerated() => _globalGenerated.Clear();
    }
}
