using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyStringGeneration
{
    public interface IGenerativeRule
    {
        IEnumerable<string> Generate(string input);
    }

    public class GenerativeStep : IStringPipelineStep
    {
        private readonly IReadOnlyList<IGenerativeRule> _rules;

        public GenerativeStep(IEnumerable<IGenerativeRule> rules)
        {
            _rules = new List<IGenerativeRule>(rules);
        }

        public HashSet<string> Execute(HashSet<string> input, HashSet<string> globalGenerated)
        {
            foreach (var str in input)
            {
                foreach (var rule in _rules)
                {
                    var newStrings = rule.Generate(str);
                    globalGenerated.UnionWith(newStrings);
                }
            }
            // We return the same input set for the next step
            return input;
        }
    }
}
