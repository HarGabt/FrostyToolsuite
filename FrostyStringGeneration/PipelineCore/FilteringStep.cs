using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostyStringGeneration
{
    public interface IFilteringRule
    {
        bool ShouldKeep(string input);
    }

    public class FilteringStep : IStringPipelineStep
    {
        private readonly IReadOnlyList<IFilteringRule> _rules;

        public FilteringStep(IEnumerable<IFilteringRule> rules)
        {
            _rules = rules.ToList();
        }

        public HashSet<string> Execute(HashSet<string> input, HashSet<string> globalGenerated)
        {
            // Return a new set containing only those that pass all rules
            var filtered = new HashSet<string>();

            foreach (var str in input)
            {
                if (_rules.All(r => r.ShouldKeep(str)))
                {
                    filtered.Add(str);
                }
            }

            return filtered;
        }
    }
}
